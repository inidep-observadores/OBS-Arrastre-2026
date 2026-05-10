using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OBSArrastre2026.App.Configuration;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Models;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.App.ViewModels;

using OBSArrastre2026.App.Features.Mareas;
using OBSArrastre2026.App.Features.Lances;

namespace OBSArrastre2026.App;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.SetBasePath(AppContext.BaseDirectory);
                configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<DatabaseOptions>(context.Configuration.GetSection(DatabaseOptions.SectionName));
                services.AddSingleton(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value);

                services.AddSingleton<IDatabasePathProvider, DatabasePathProvider>();
                services.AddSingleton<IUserSettingsService, UserSettingsService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<IMockShellDataService, MockShellDataService>();

                services.AddDbContextFactory<AppDbContext>((sp, options) =>
                {
                    var databasePathProvider = sp.GetRequiredService<IDatabasePathProvider>();
                    options.UseSqlite(databasePathProvider.GetConnectionString())
                           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                });

                services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
                services.AddSingleton<IActiveMareaManager, ActiveMareaManager>();
                services.AddSingleton<IBuqueService, BuqueService>();
                services.AddSingleton<IMareaService, MareaService>();
                services.AddSingleton<ILanceService, LanceService>();
                services.AddSingleton<IMuestraService, MuestraService>();
                services.AddSingleton<ISubmuestraService, SubmuestraService>();
                services.AddSingleton<IProduccionService, ProduccionService>();
                services.AddSingleton<IProductoService, ProductoService>();

                // Servicios de sincronización de datos
                services.AddSingleton<IDbfExtractorService, DbfExtractorService>();
                services.AddSingleton<IJsonImportService, JsonImportService>();
                services.AddSingleton<IDataSyncCoordinator, DataSyncCoordinator>();
                services.AddSingleton<IMareaReportService, MareaReportService>();
                services.AddSingleton<IMareaImportService, MareaImportService>(sp => 
                    new MareaImportService(
                        sp.GetRequiredService<IDbfExtractorService>(),
                        sp.GetRequiredService<IMareaReportService>(),
                        sp.GetRequiredService<IDbContextFactory<AppDbContext>>()));
                services.AddSingleton<IMareaValidationService, MareaValidationService>();
                services.AddSingleton<GeoJsonService>();
                services.AddSingleton<IMapRenderingService, MapRenderingService>();
                services.AddSingleton<IExcelReportService, ExcelReportService>();
                services.AddSingleton<IMareaSummaryService, MareaSummaryService>();
                services.AddSingleton<IDbfExporterService, DbfExporterService>();

                // Validación y ViewModels
                services.AddValidatorsFromAssemblyContaining<App>();
                services.AddTransient<IValidator<MuestraEditViewModel>, MuestraEditViewModelValidator>();
                services.AddTransient<IValidator<SubmuestraEditViewModel>, SubmuestraEditViewModelValidator>();
                services.AddTransient<IValidator<ProduccionEditViewModel>, ProduccionEditViewModelValidator>();

                services.AddTransient<MareaEditViewModel>();
                services.AddTransient<LanceEditViewModel>();
                services.AddTransient<MuestraEditViewModel>();
                services.AddTransient<SubmuestraEditViewModel>();

                services.AddSingleton<Func<Action, string?, MareaEditViewModel>>(sp =>
                    (onClose, mareaId) =>
                    {
                        var validator = sp.GetRequiredService<IValidator<MareaEditViewModel>>();
                        var mareaService = sp.GetRequiredService<IMareaService>();
                        var buqueService = sp.GetRequiredService<IBuqueService>();
                        var mareaImportService = sp.GetRequiredService<IMareaImportService>();
                        var jsonImportService = sp.GetRequiredService<IJsonImportService>();
                        var activeMareaManager = sp.GetRequiredService<IActiveMareaManager>();
                        return new MareaEditViewModel(onClose, validator, mareaService, buqueService, mareaImportService, jsonImportService, activeMareaManager, mareaId);
                    });

                services.AddSingleton<Func<Action, string, string?, LanceEditViewModel>>(sp =>
                    (onClose, mareaId, lanceId) =>
                    {
                        var validator = sp.GetRequiredService<IValidator<LanceEditViewModel>>();
                        var lanceService = sp.GetRequiredService<ILanceService>();
                        return new LanceEditViewModel(onClose, validator, lanceService, mareaId, lanceId);
                    });

                services.AddSingleton<Func<Action, string, string?, MuestraEditViewModel>>(sp =>
                    (onClose, lanceId, muestraId) =>
                    {
                        var validator = sp.GetRequiredService<IValidator<MuestraEditViewModel>>();
                        var muestraService = sp.GetRequiredService<IMuestraService>();
                        var lanceService = sp.GetRequiredService<ILanceService>();
                        return new MuestraEditViewModel(onClose, validator, muestraService, lanceService, lanceId, muestraId);
                    });

                services.AddSingleton<Func<Action, string, SubmuestraEditViewModel>>(sp =>
                    (onClose, muestraId) =>
                    {
                        var validator = sp.GetRequiredService<IValidator<SubmuestraEditViewModel>>();
                        var submuestraService = sp.GetRequiredService<ISubmuestraService>();
                        var muestraService = sp.GetRequiredService<IMuestraService>();
                        return new SubmuestraEditViewModel(onClose, validator, submuestraService, muestraService, muestraId);
                    });

                services.AddTransient<ProduccionEditViewModel>();
                services.AddSingleton<Func<Action, string?, ProduccionEditViewModel>>(sp =>
                    (onClose, registroId) =>
                    {
                        var validator = sp.GetRequiredService<IValidator<ProduccionEditViewModel>>();
                        var produccionService = sp.GetRequiredService<IProduccionService>();
                        var productoService = sp.GetRequiredService<IProductoService>();
                        var activeMareaManager = sp.GetRequiredService<IActiveMareaManager>();
                        return new ProduccionEditViewModel(onClose, validator, produccionService, productoService, activeMareaManager, registroId);
                    });

                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        // Registro de navegación global "Enter as Tab"
        EventManager.RegisterClassHandler(typeof(UIElement), UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown));

        // Selección automática de texto al recibir foco en TextBox
        EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(OnTextBoxGotFocus));
    }

    private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            // Usamos Dispatcher para asegurar que la selección ocurra después de que 
            // los eventos de mouse (que podrían deseleccionar) hayan terminado.
            tb.Dispatcher.BeginInvoke(new Action(() => tb.SelectAll()));
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var element = Keyboard.FocusedElement as UIElement;
            if (element == null) return;

            // Excepción: Permitir el Enter normal en TextBox que acepten retornos
            if (element is TextBox tb && tb.AcceptsReturn) return;

            // Navegar al siguiente/anterior elemento
            var direction = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift 
                            ? FocusNavigationDirection.Previous 
                            : FocusNavigationDirection.Next;

            e.Handled = true;
            element.MoveFocus(new TraversalRequest(direction));
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Habilitar soporte para codificaciones legacy (IBM850, Windows-1252, etc.)
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        base.OnStartup(e);

        await _host.StartAsync();

        // Inicializar base de datos (migraciones)
        var databaseInitializer = _host.Services.GetRequiredService<IDatabaseInitializer>();
        await databaseInitializer.InitializeAsync();
        
        // Sincronizar datos maestros (especies, buques) necesarios para el catálogo
        await _host.Services.GetRequiredService<IDataSyncCoordinator>().SyncAllAsync();

        // Sembrar catálogos que dependen de datos maestros
        await databaseInitializer.SeedCatalogsAsync();

        // Cargar preferencias de usuario
        var settingsService = _host.Services.GetRequiredService<IUserSettingsService>();
        var settings = settingsService.GetSettings();

        // Aplicar tema guardado
        _host.Services.GetRequiredService<IThemeService>().ApplyTheme(settings.ThemeMode);

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        
        // Aplicar estado de la ventana (Maximizada por defecto si no hay registro)
        mainWindow.WindowState = settings.WindowState;
        
        // Inicializar gestión de marea activa
        await _host.Services.GetRequiredService<IActiveMareaManager>().InitializeAsync();

        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
