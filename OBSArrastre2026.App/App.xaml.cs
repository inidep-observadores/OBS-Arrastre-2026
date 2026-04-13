using System;
using System.Windows;
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
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<IMockShellDataService, MockShellDataService>();

                services.AddDbContextFactory<AppDbContext>((sp, options) =>
                {
                    var databasePathProvider = sp.GetRequiredService<IDatabasePathProvider>();
                    options.UseSqlite(databasePathProvider.GetConnectionString());
                });

                services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
                services.AddSingleton<IBuqueService, BuqueService>();

                // Servicios de sincronización de datos
                services.AddSingleton<IDbfExtractorService, DbfExtractorService>();
                services.AddSingleton<IJsonImportService, JsonImportService>();
                services.AddSingleton<IDataSyncCoordinator, DataSyncCoordinator>();
                services.AddSingleton<IMareaReportService, MareaReportService>();
                services.AddSingleton<IMareaImportService, MareaImportService>();

                // Validación y ViewModels
                services.AddValidatorsFromAssemblyContaining<App>();
                
                services.AddSingleton<Func<Action, MareaEditViewModel>>(sp => 
                    (Action onClose) => new MareaEditViewModel(onClose, sp.GetRequiredService<IValidator<MareaEditViewModel>>()));

                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        await _host.StartAsync();

        // Inicializar base de datos y realizar sembrado/sincronización de datos maestros
        await _host.Services.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
        await _host.Services.GetRequiredService<IDataSyncCoordinator>().SyncAllAsync();

        _host.Services.GetRequiredService<IThemeService>().ApplyTheme(AppThemeMode.System);

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
