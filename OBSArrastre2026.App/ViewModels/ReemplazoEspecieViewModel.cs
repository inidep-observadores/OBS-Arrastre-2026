using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services;

namespace OBSArrastre2026.App.ViewModels;

public class EspecieReemplazoItem : ObservableObject
{
    public Especie EspecieOriginal { get; }
    
    private Especie? _especieNueva;
    public Especie? EspecieNueva
    {
        get => _especieNueva;
        set => SetProperty(ref _especieNueva, value);
    }

    public int Ocurrencias { get; set; }

    public EspecieReemplazoItem(Especie original, int ocurrencias)
    {
        EspecieOriginal = original;
        Ocurrencias = ocurrencias;
    }
}

public partial class ReemplazoEspecieViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IActiveMareaManager _activeMareaManager;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ObservableCollection<EspecieReemplazoItem> EspeciesEnMarea { get; } = new();
    public ObservableCollection<Especie> TodasLasEspecies { get; } = new();

    private readonly Action? _onClose;

    public Action<string, string, string?, MessageDialogType>? ShowMessage { get; set; }
    public Func<string, string, Task<bool>>? ShowConfirmation { get; set; }

    public ReemplazoEspecieViewModel(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IActiveMareaManager activeMareaManager,
        Action? onClose = null)
    {
        _dbContextFactory = dbContextFactory;
        _activeMareaManager = activeMareaManager;
        _onClose = onClose;

        SaveCommand = new AsyncRelayCommand(AplicarCambiosAsync);
        CancelCommand = new RelayCommand(Cancel);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private void Cancel() => _onClose?.Invoke();

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            EspeciesEnMarea.Clear();
            TodasLasEspecies.Clear();

            using var context = await _dbContextFactory.CreateDbContextAsync();

            // Cargar todas las especies para el selector
            var todas = await context.Especies
                .OrderBy(e => e.NombreVulgar)
                .ToListAsync();
            foreach (var e in todas) TodasLasEspecies.Add(e);

            var activeMareaId = _activeMareaManager.ActiveMareaId;
            if (string.IsNullOrEmpty(activeMareaId)) return;

            // Buscar especies en Capturas
            var especiesCaptura = await context.ItemsCaptura
                .Where(i => i.Lance.MareaEtapa.MareaID == activeMareaId)
                .GroupBy(i => i.EspecieID)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            // Buscar especies en Muestras
            var especiesMuestra = await context.Muestras
                .Where(m => m.Lance.MareaEtapa.MareaID == activeMareaId)
                .GroupBy(m => m.EspecieID)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            // Buscar especies en Producción
            var especiesProd = await context.RegistrosProduccion
                .Where(p => p.MareaEtapa.MareaID == activeMareaId)
                .GroupBy(p => p.EspecieId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            // Combinar y contar
            var ids = especiesCaptura.Select(x => x.Id)
                .Concat(especiesMuestra.Select(x => x.Id))
                .Concat(especiesProd.Select(x => x.Id))
                .Where(id => id != null)
                .Distinct()
                .ToList();

            var result = new List<EspecieReemplazoItem>();
            foreach (var id in ids)
            {
                var especie = todas.FirstOrDefault(e => e.ID == id);
                if (especie == null) continue;

                int count = especiesCaptura.FirstOrDefault(x => x.Id == id)?.Count ?? 0;
                count += especiesMuestra.FirstOrDefault(x => x.Id == id)?.Count ?? 0;
                count += especiesProd.FirstOrDefault(x => x.Id == id)?.Count ?? 0;

                result.Add(new EspecieReemplazoItem(especie, count));
            }

            foreach (var item in result.OrderBy(x => x.EspecieOriginal.NombreVulgar))
            {
                EspeciesEnMarea.Add(item);
            }
        }
        catch (Exception ex)
        {
            ShowMessage?.Invoke("Error", $"No se pudieron cargar las especies: {ex.Message}", null, MessageDialogType.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AplicarCambiosAsync()
    {
        var aReemplazar = EspeciesEnMarea.Where(x => x.EspecieNueva != null && x.EspecieNueva.ID != x.EspecieOriginal.ID).ToList();

        if (!aReemplazar.Any())
        {
            ShowMessage?.Invoke("Sin cambios", "No se han seleccionado especies para reemplazar.", null, MessageDialogType.Info);
            return;
        }

        bool confirm = await (ShowConfirmation?.Invoke("Confirmar reemplazo", 
            $"Se procederá a reemplazar {aReemplazar.Count} especies en todos los registros de la marea actual. Esta acción es irreversible. ¿Desea continuar?") ?? Task.FromResult(false));

        if (!confirm) return;

        IsLoading = true;
        try
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var activeMareaId = _activeMareaManager.ActiveMareaId;

            foreach (var item in aReemplazar)
            {
                var idViejo = item.EspecieOriginal.ID;
                var idNuevo = item.EspecieNueva!.ID;

                // Update ItemsCaptura
                var itemsCaptura = await context.ItemsCaptura
                    .Where(i => i.Lance.MareaEtapa.MareaID == activeMareaId && i.EspecieID == idViejo)
                    .ToListAsync();
                foreach (var i in itemsCaptura) i.EspecieID = idNuevo;

                // Update Muestras
                var muestras = await context.Muestras
                    .Where(m => m.Lance.MareaEtapa.MareaID == activeMareaId && m.EspecieID == idViejo)
                    .ToListAsync();
                foreach (var m in muestras) m.EspecieID = idNuevo;

                // Update RegistrosProduccion
                var prod = await context.RegistrosProduccion
                    .Where(p => p.MareaEtapa.MareaID == activeMareaId && p.EspecieId == idViejo)
                    .ToListAsync();
                foreach (var p in prod) p.EspecieId = idNuevo;
                
                // Update MareaEtapas (Especie Objetivo)
                var etapas = await context.MareaEtapas
                    .Where(e => e.MareaID == activeMareaId && e.EspecieObjetivoID == idViejo)
                    .ToListAsync();
                foreach (var e in etapas) e.EspecieObjetivoID = idNuevo;
            }

            await context.SaveChangesAsync();
            ShowMessage?.Invoke("Éxito", "Los cambios han sido aplicados correctamente.", null, MessageDialogType.Success);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ShowMessage?.Invoke("Error", $"Ocurrió un error al aplicar los cambios: {ex.Message}", null, MessageDialogType.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
