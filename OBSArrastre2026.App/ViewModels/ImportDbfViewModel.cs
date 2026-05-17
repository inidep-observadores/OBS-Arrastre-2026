using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Models;
using OBSArrastre2026.App.Models.Import;
using System.IO.Compression;

namespace OBSArrastre2026.App.ViewModels;

public sealed class DbfFileItem : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public string SizeDisplay { get; init; } = string.Empty;
    public string DateDisplay { get; init; } = string.Empty;
}

public sealed partial class ImportDbfViewModel : ObservableObject, IDisposable
{
    private readonly Action<IEnumerable<string>?, string?> _onFinished;
    private readonly IMareaImportService _importService;
    private readonly IJsonImportService _jsonImportService;
    private readonly IMareaService _mareaService;
    private int _mareaNum;
    private int _anio;
    private string _pattern = string.Empty;
    private string _barco;
    private string? _mareaId;
    private IEnumerable<MareaEtapa> _etapas;
    private bool _isBusy;
    private bool _isMareaInputVisible;
    private string? _originalZipFolder;
    private readonly List<string> _filesExtractedFromZip = new();

    public ImportDbfViewModel(
        string? mareaId,
        int mareaNum, 
        int anio, 
        IMareaImportService importService,
        IJsonImportService jsonImportService,
        IMareaService mareaService,
        string barco,
        IEnumerable<MareaEtapa> etapas,
        Action<IEnumerable<string>?, string?> onFinished)
    {
        _mareaId = mareaId;
        _mareaNum = mareaNum;
        _anio = anio;
        _importService = importService;
        _jsonImportService = jsonImportService;
        _mareaService = mareaService;
        _barco = barco;
        _etapas = etapas;
        _onFinished = onFinished;
        _isMareaInputVisible = string.IsNullOrEmpty(mareaId);
        UpdatePattern();

        AddFilesCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(AddFiles, () => !IsBusy && (!IsMareaInputVisible || (MareaNum > 0 && Anio > 2000)));
        AcceptCommand = new AsyncRelayCommand(AcceptAsync, () => !IsBusy && SelectedFiles.Count > 0);
        CancelCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => _onFinished(null, null), () => !IsBusy);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set 
        {
            if (SetProperty(ref _isBusy, value))
            {
                (AddFilesCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                (AcceptCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                (CancelCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    private string _busyMessage = "Procesando...";
    public string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    public int MareaNum
    {
        get => _mareaNum;
        set 
        {
            if (SetProperty(ref _mareaNum, value))
            {
                UpdatePattern();
                (AddFilesCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public int Anio
    {
        get => _anio;
        set 
        {
            if (SetProperty(ref _anio, value))
            {
                UpdatePattern();
                (AddFilesCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsMareaInputVisible
    {
        get => _isMareaInputVisible;
        set => SetProperty(ref _isMareaInputVisible, value);
    }

    private TipoDatoDescarte _selectedTipoDatoDescarte = TipoDatoDescarte.Kilogramos;
    public TipoDatoDescarte SelectedTipoDatoDescarte
    {
        get => _selectedTipoDatoDescarte;
        set => SetProperty(ref _selectedTipoDatoDescarte, value);
    }

    private void UpdatePattern()
    {
        _pattern = $"{_mareaNum}{_anio % 100:D2}";
        OnPropertyChanged(nameof(PatternNote));
    }

    public string PatternNote => $"Patrón esperado: *{_pattern}*.dbf / M{_pattern}.json / Marea_{_pattern}.zip";
    
    public ObservableCollection<DbfFileItem> SelectedFiles { get; } = [];

    public ICommand AddFilesCommand { get; }
    public ICommand AcceptCommand { get; }
    public ICommand CancelCommand { get; }

    public Func<string, string, string?, MessageDialogType, Task>? ShowMessage { get; set; }
    public Func<string, string, Task<bool?>>? ShowConfirmation { get; set; }

    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = $"Archivos de Marea (*{_pattern}*.dbf; M{_pattern}.json; Marea_{_pattern}.zip)|*{_pattern}*.dbf;M{_pattern}.json;Marea_{_pattern}.zip|Todos los archivos (*.*)|*.*",
            Title = "Seleccionar Archivos de Marea (DBF, JSON o ZIP)"
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var filePath in dialog.FileNames)
            {
                var fileName = Path.GetFileName(filePath);

                if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessZipFile(filePath);
                    continue;
                }
                
                if (!fileName.Contains(_pattern, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (SelectedFiles.Any(f => f.FullPath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var info = new FileInfo(filePath);
                SelectedFiles.Add(new DbfFileItem
                {
                    Name = fileName,
                    FullPath = filePath,
                    SizeDisplay = FormatSize(info.Length),
                    DateDisplay = info.LastWriteTime.ToString("dd/MM/yyyy HH:mm")
                });
            }
            (AcceptCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private void ProcessZipFile(string zipPath)
    {
        try
        {
            _originalZipFolder = Path.GetDirectoryName(zipPath);
            if (string.IsNullOrEmpty(_originalZipFolder)) return;

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                _filesExtractedFromZip.Clear();

                // Extraer el contenido del ZIP directamente en la carpeta original
                foreach (var entry in archive.Entries)
                {
                    var targetPath = Path.Combine(_originalZipFolder, entry.Name);
                    entry.ExtractToFile(targetPath, true);
                    _filesExtractedFromZip.Add(targetPath);
                }

                // Verificar si hay un archivo de tracking (T{pattern}.dbf) en los archivos extraídos
                var hasTracking = _filesExtractedFromZip.Any(f => 
                    Path.GetFileName(f).StartsWith("T", StringComparison.OrdinalIgnoreCase) && 
                    Path.GetExtension(f).Equals(".dbf", StringComparison.OrdinalIgnoreCase) && 
                    Path.GetFileNameWithoutExtension(f).Contains(_pattern, StringComparison.OrdinalIgnoreCase));

                if (!hasTracking)
                {
                    ShowMessage?.Invoke("ZIP Inválido", $"No se encontró el archivo de datos (T{_pattern}.dbf) dentro del ZIP.", null, MessageDialogType.Error);
                    return;
                }

                // Agregar todos los archivos extraídos que correspondan al patrón a la UI
                foreach (var path in _filesExtractedFromZip)
                {
                    var name = Path.GetFileName(path);
                    if (!name.Contains(_pattern, StringComparison.OrdinalIgnoreCase)) continue;

                    // Si ya existía en SelectedFiles por una selección manual previa, lo removemos
                    var existing = SelectedFiles.FirstOrDefault(f => f.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase));
                    if (existing != null) SelectedFiles.Remove(existing);

                    var info = new FileInfo(path);
                    SelectedFiles.Add(new DbfFileItem
                    {
                        Name = name,
                        FullPath = path,
                        SizeDisplay = FormatSize(info.Length),
                        DateDisplay = info.LastWriteTime.ToString("dd/MM/yyyy HH:mm")
                    });
                }
            }
            (AcceptCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            ShowMessage?.Invoke("Error ZIP", $"Error al procesar el archivo ZIP: {ex.Message}", null, MessageDialogType.Error);
        }
    }

    private async Task AcceptAsync()
    {
        if (SelectedFiles.Count == 0) return;

        IsBusy = true;
        bool isNewMareaCreated = false;
        try
        {
            // 0. Si no hay ID de marea, intentar buscar una existente
            bool mareaExiste = false;
            if (!string.IsNullOrEmpty(_mareaId))
            {
                mareaExiste = true;
            }
            else
            {
                BusyMessage = "Verificando si la marea ya existe...";
                var existing = await _mareaService.FindMareaAsync(_mareaNum, _anio);
                if (existing != null)
                {
                    _mareaId = existing.ID;
                    _etapas = existing.Etapas;
                    _barco = existing.Buque?.Nombre ?? _barco;
                    mareaExiste = true;
                }
            }

            // Si la marea es nueva (no existe en SQLite), es obligatorio contar con el archivo de metadata (.json)
            var jsonFile = SelectedFiles.FirstOrDefault(f => f.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
            if (!mareaExiste && jsonFile == null)
            {
                IsBusy = false;
                if (ShowMessage != null)
                {
                    await ShowMessage(
                        "Falta Metadata Obligatoria", 
                        "La marea que intentas importar no existe en la base de datos local y no se incluyó el archivo JSON de metadatos (.json).\n\n" +
                        "Para registrar una marea nueva es obligatorio incluir dicho archivo, ya que contiene la información de etapas (zarpada/arribo) requerida para vincular correctamente los lances.", 
                        null, 
                        MessageDialogType.Error);
                }
                _onFinished(null, null);
                return;
            }

            // Si es marea nueva, se crea en la base de datos
            if (!mareaExiste)
            {
                BusyMessage = "Creando nueva marea...";
                var marea = new Marea
                {
                    ID = Guid.NewGuid().ToString(),
                    NumeroInidep = _mareaNum,
                    AnioInidep = _anio,
                    FechaInicio = DateTime.Today
                };
                await _mareaService.SaveMareaAsync(marea);
                _mareaId = marea.ID;
                isNewMareaCreated = true;
            }

            BusyMessage = "Validando integridad de archivos...";
            // 0. Verificar si hay archivo JSON para metadatos (pero no guardar nada aún en la base de datos)
            bool importMetadata = false;
            if (jsonFile != null)
            {
                if (!mareaExiste)
                {
                    // Si es marea nueva, la metadata es obligatoria
                    importMetadata = true;
                }
                else
                {
                    BusyMessage = "Verificando metadatos de marea desde JSON...";
                    bool? confirmMetadata = await (ShowConfirmation?.Invoke(
                        "Importar Metadatos", 
                        "Se ha detectado un archivo JSON de metadatos. ¿Deseas actualizar el Buque, Fechas y Etapas de la marea con la información del JSON?") ?? Task.FromResult<bool?>(false));
                    
                    importMetadata = confirmMetadata == true;
                }
            }

            // Recargar marea actual de la base de datos (por si tiene datos ya cargados)
            var mareaUpdated = await _mareaService.GetMareaAsync(_mareaId);
            if (mareaUpdated == null)
            {
                throw new InvalidOperationException("No se pudo obtener la marea recién cargada.");
            }

            // Si el usuario aceptó actualizar metadatos, los aplicamos únicamente en memoria para la validación
            if (importMetadata && jsonFile != null)
            {
                BusyMessage = "Cargando metadatos propuestos en memoria...";
                await _jsonImportService.UpdateMareaMetadataInMemoryAsync(mareaUpdated, jsonFile.FullPath);
            }

            // Sincronizamos las variables del ViewModel con lo que hay en memoria
            _barco = mareaUpdated.Buque?.Nombre ?? _barco;
            _mareaNum = mareaUpdated.NumeroInidep;
            _anio = mareaUpdated.AnioInidep;
            OnPropertyChanged(nameof(MareaNum));
            OnPropertyChanged(nameof(Anio));

            // La carpeta base es la del primer archivo seleccionado
            string basePath = Path.GetDirectoryName(SelectedFiles[0].FullPath) ?? string.Empty;

            // 1. Validar utilizando exclusivamente los archivos seleccionados en la UI
            BusyMessage = "Validando integridad de archivos DBF...";
            var filesToProcess = SelectedFiles.Select(f => f.FullPath).Distinct().ToList();
            
            var report = await _importService.ProcessMareaImportAsync(basePath, filesToProcess, mareaUpdated);
            report.UnidadDescarte = SelectedTipoDatoDescarte;

            if (report.HasFatalErrors)
            {
                // REQ: Si la importación falla por error grave, eliminar la marea (si fue creada en este proceso)
                if (isNewMareaCreated && !string.IsNullOrEmpty(_mareaId))
                {
                    BusyMessage = "Cancelando importación por errores graves...";
                    await _mareaService.DeleteMareaAsync(_mareaId);
                    _mareaId = null;
                }

                // Limpiar archivos extraídos del ZIP
                Dispose();

                TryOpenAuditReport(basePath, report);
                if (ShowMessage != null) await ShowMessage("Errores de Validación", "Se detectaron errores graves que impiden la importación. Se ha abierto el reporte PDF con el detalle.", null, MessageDialogType.Error);
                _onFinished(null, null);
                return;
            }

            // 2. Verificar datos existentes y confirmar
            BusyMessage = "Verificando datos previos...";
            if (!report.IsTrackingOnly && await _importService.HasDataAsync(_mareaId))
            {
                IsBusy = false; // Ocultamos spinner para mostrar confirmación
                bool? confirm = await (ShowConfirmation?.Invoke(
                    "Sobreescribir Datos", 
                    "Esta marea ya contiene lances, muestras o producción cargada. Si continúas, todos los datos existentes serán eliminados para realizar una importación limpia. ¿Deseas continuar?") ?? Task.FromResult<bool?>(false));
                
                if (confirm != true)
                {
                    // Si cancela y la marea fue creada en este proceso, la eliminamos para no dejar basura
                    if (isNewMareaCreated && !string.IsNullOrEmpty(_mareaId))
                    {
                        await _mareaService.DeleteMareaAsync(_mareaId);
                        _mareaId = null;
                    }
                    
                    // Limpiar archivos extraídos del ZIP
                    Dispose();
                    
                    _onFinished(null, null);
                    return;
                }
                
                IsBusy = true;
            }

            // 3. Importación Real (Commit)
            BusyMessage = "Persistiendo datos en la base de datos...";

            // A. Primero actualizamos los metadatos reales en la base de datos si el usuario lo confirmó
            if (importMetadata && jsonFile != null)
            {
                BusyMessage = "Guardando metadatos y etapas actualizadas...";
                await _jsonImportService.UpdateMareaMetadataAsync(_mareaId, jsonFile.FullPath);
            }

            // B. Segundo, si no es de solo tracking y tenía datos anteriores, limpiamos la base de datos
            if (!report.IsTrackingOnly)
            {
                if (await _importService.HasDataAsync(_mareaId))
                {
                    BusyMessage = "Eliminando datos previos de lances y capturas...";
                    await _importService.ClearMareaDataAsync(_mareaId);
                }
            }
            else
            {
                BusyMessage = "Limpiando datos previos de seguimiento satelital...";
                await _importService.ClearTrackingDataAsync(_mareaId);
            }

            // C. Tercero, persistimos los nuevos datos de los archivos seleccionados
            BusyMessage = "Persistiendo datos en la base de datos...";
            await _importService.ImportAsync(_mareaId, report);

            IsBusy = false;
            
            if (report.Issues.Any())
            {
                TryOpenAuditReport(basePath, report);
            }

            _onFinished(SelectedFiles.Select(f => f.FullPath), _mareaId);
        }
        catch (Exception ex)
        {
            IsBusy = false;
            // Si hay error grave inesperado y la marea fue creada en este proceso, la eliminamos
            if (isNewMareaCreated && !string.IsNullOrEmpty(_mareaId))
            {
                try { await _mareaService.DeleteMareaAsync(_mareaId); } catch { }
                _mareaId = null;
            }
            
            // Limpiar archivos extraídos del ZIP
            Dispose();

            if (ShowMessage != null) await ShowMessage("Error de Importación", $"Ocurrió un error inesperado: {ex.Message}", ex.ToString(), MessageDialogType.Error);
            _onFinished(null, null);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void TryOpenAuditReport(string basePath, MareaValidationReport? report = null)
    {
        string safeBarco;
        string mareaNumStr;
        string anioStr;

        if (report != null)
        {
            safeBarco = (report.Barco ?? "S-D").Replace("/", "-").Replace("\\", "-");
            mareaNumStr = report.Marea;
            anioStr = report.Año.ToString();
        }
        else
        {
            safeBarco = (_barco ?? "S-D").Replace("/", "-").Replace("\\", "-");
            mareaNumStr = _mareaNum.ToString();
            anioStr = _anio.ToString();
        }
        
        string reportPath = Path.Combine(basePath, "Reportes", $"Audit_{safeBarco}_{mareaNumStr}_{anioStr}.pdf");
        
        if (File.Exists(reportPath))
        {
            try
            {
                Process.Start(new ProcessStartInfo(reportPath) { UseShellExecute = true });
            }
            catch { /* Ignorar errores al abrir el proceso */ }
        }
    }

    private string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{(double)bytes / (1024 * 1024):F2} MB";
        return $"{(double)bytes / 1024:F2} KB";
    }

    public void Dispose()
    {
        // 1. Limpiar archivos extraídos del ZIP
        foreach (var file in _filesExtractedFromZip)
        {
            try
            {
                if (File.Exists(file)) File.Delete(file);
            }
            catch { /* Ignorar si está bloqueado */ }
        }

    }
}
