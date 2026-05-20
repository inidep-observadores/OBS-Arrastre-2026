using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Models;
using ControlMareas.App.Services;
using ControlMareas.App.ViewModels;
using Xunit;

namespace ControlMareas.Tests;

public class ControlProduccionRayaTests
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IActiveMareaManager _activeMareaManager;
    private readonly IProduccionService _produccionService;
    private readonly IMareaService _mareaService;
    private readonly IThemeService _themeService;
    private readonly IMareaValidationService _validationService;
    private readonly IMareaReportService _reportService;
    private readonly IJsonImportService _jsonImportService;
    private readonly IMareaImportService _mareaImportService;
    private readonly IMockShellDataService _mockShellDataService;
    private readonly IBuqueService _buqueService;
    private readonly ILanceService _lanceService;
    private readonly IMuestraService _muestraService;
    private readonly ISubmuestraService _submuestraService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IMapRenderingService _mapRenderingService;
    private readonly IExcelReportService _excelReportService;
    private readonly IMareaSummaryService _mareaSummaryService;
    private readonly IDbfExporterService _dbfExporterService;
    private readonly ConfiguracionViewModel _configuracionViewModel;

    public ControlProduccionRayaTests()
    {
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        _dbContextFactory.CreateDbContextAsync().Returns(_ => Task.FromResult(new AppDbContext(options)));
        _dbContextFactory.CreateDbContext().Returns(_ => new AppDbContext(options));

        _activeMareaManager = Substitute.For<IActiveMareaManager>();
        _produccionService = Substitute.For<IProduccionService>();
        _mareaService = Substitute.For<IMareaService>();
        _themeService = Substitute.For<IThemeService>();
        _validationService = Substitute.For<IMareaValidationService>();
        _reportService = Substitute.For<IMareaReportService>();
        _jsonImportService = Substitute.For<IJsonImportService>();
        _mareaImportService = Substitute.For<IMareaImportService>();
        _mockShellDataService = Substitute.For<IMockShellDataService>();
        _buqueService = Substitute.For<IBuqueService>();
        _lanceService = Substitute.For<ILanceService>();
        _muestraService = Substitute.For<IMuestraService>();
        _submuestraService = Substitute.For<ISubmuestraService>();
        _userSettingsService = Substitute.For<IUserSettingsService>();
        _userSettingsService.GetSettings().Returns(new UserSettings());
        _mapRenderingService = Substitute.For<IMapRenderingService>();
        _excelReportService = Substitute.For<IExcelReportService>();
        _mareaSummaryService = Substitute.For<IMareaSummaryService>();
        _dbfExporterService = Substitute.For<IDbfExporterService>();
        _configuracionViewModel = new ConfiguracionViewModel(_themeService, _userSettingsService);
    }

    [Fact]
    public async Task LoadControlProduccionAsync_ShouldGroupOrphanRayasUnderGenericRow()
    {
        // Arrange
        var mareaId = "marea-1";
        var etapaId = "etapa-1";
        var marea = new Marea { ID = mareaId };
        var etapa = new MareaEtapa { ID = etapaId, MareaID = mareaId };
        marea.Etapas.Add(etapa);

        _activeMareaManager.ActiveMarea.Returns(marea);

        // Especies
        var espRayaGeneric = new Especie { ID = "esp-raya-gen", CodigoInidep = "7109000000", NombreVulgar = "Raya Genérica" };
        var espRayaSpec1 = new Especie { ID = "esp-raya-spec-1", CodigoInidep = "7109000001", NombreVulgar = "Raya Pintada" };

        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            db.Especies.AddRange(espRayaGeneric, espRayaSpec1);
            
            var lance = new Lance 
            { 
                Id = "lance-1", 
                MareaEtapaId = etapaId, 
                MareaEtapa = etapa,
                Fecha = DateTime.Today.ToString("yyyy-MM-dd")
            };
            
            // Captura: Raya Pintada (7109000001) - Esta será huérfana porque no estará en producción
            lance.ItemsCaptura.Add(new ItemCaptura 
            { 
                ID = "item-1", 
                EspecieID = espRayaSpec1.ID, 
                Especie = espRayaSpec1,
                TipoDatoCaptura = TipoDatoCaptura.Kilogramos,
                DatoCaptura = 100,
                Lance = lance
            });

            db.Lances.Add(lance);
            await db.SaveChangesAsync();
        }

        // Producción: Raya Genérica (7109000000)
        var regProd = new RegistroProduccion 
        { 
            Id = "prod-1", 
            MareaEtapaId = etapaId, 
            EspecieId = espRayaGeneric.ID, 
            Especie = espRayaGeneric,
            Kg = 50,
            Factor = 1.0
        };
        _produccionService.GetRegistrosProduccionAsync(etapaId).Returns(new List<RegistroProduccion> { regProd });

        var vm = CreateViewModel();

        // Act
        await vm.LoadControlProduccionAsyncInternal();

        // Assert
        var rayaRow = vm.Records.OfType<ControlProduccionListItemViewModel>().FirstOrDefault(r => r.Especie.Contains("Rayas"));
        rayaRow.Should().NotBeNull("Debería existir una fila agrupada de rayas");
        rayaRow!.ProduccionTotal.Should().Be(50);
        
        // La captura de 100kg de Raya Pintada (huérfana) debería sumarse aquí
        rayaRow.CapturaRetenida.Should().Be(100, "La captura de la raya huérfana debería sumarse a la fila genérica");
    }

    [Fact]
    public async Task LoadControlLanceDetailsAsync_ShouldShowLancesForOrphanRayasWhenGenericRowIsSelected()
    {
        // Arrange
        var mareaId = "marea-2";
        var etapaId = "etapa-2";
        var marea = new Marea { ID = mareaId };
        var etapa = new MareaEtapa { ID = etapaId, MareaID = mareaId };
        marea.Etapas.Add(etapa);

        _activeMareaManager.ActiveMarea.Returns(marea);

        var espRayaGeneric = new Especie { ID = "esp-raya-gen-2", CodigoInidep = "7109000000", NombreVulgar = "Raya Genérica" };
        var espRayaSpec2 = new Especie { ID = "esp-raya-spec-2", CodigoInidep = "7109000002", NombreVulgar = "Raya Huérfana" };

        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            db.Especies.AddRange(espRayaGeneric, espRayaSpec2);
            
            var lance = new Lance 
            { 
                Id = "lance-2", 
                MareaEtapaId = etapaId, 
                MareaEtapa = etapa,
                NroLance = 42,
                Fecha = DateTime.Today.ToString("yyyy-MM-dd")
            };
            
            lance.ItemsCaptura.Add(new ItemCaptura 
            { 
                ID = "item-2", 
                EspecieID = espRayaSpec2.ID, 
                Especie = espRayaSpec2,
                TipoDatoCaptura = TipoDatoCaptura.Kilogramos,
                DatoCaptura = 200,
                Lance = lance
            });

            db.Lances.Add(lance);
            await db.SaveChangesAsync();
        }

        var vm = CreateViewModel();
        
        // Simulamos el ítem de la lista (Grupo Genérico Virtual)
        var controlItem = new ControlProduccionListItemViewModel
        {
            EspecieId = "RAYA_GENERICA_GRUPO",
            IsSummaryView = true
        };

        // Act
        await vm.LoadControlLanceDetailsAsyncInternal(controlItem);

        // Assert
        vm.ControlLanceDetails.Should().NotBeEmpty("Deberían mostrarse lances en el detalle lateral");
        vm.ControlLanceDetails.Should().Contain(d => d.NroLance == 42);
        vm.TotalCapturaKg.Should().Be(200);
    }

    private TestMainWindowViewModel CreateViewModel()
    {
        return new TestMainWindowViewModel(
            _mockShellDataService,
            _themeService,
            _mareaService,
            _buqueService,
            _lanceService,
            _muestraService,
            _activeMareaManager,
            (a, s) => Substitute.For<MareaEditViewModel>(null, null, null, null, null, null),
            (a, s1, s2) => Substitute.For<LanceEditViewModel>(null, null, null, null, null, null, null),
            (a, s1, s2) => Substitute.For<MuestraEditViewModel>(null, null, null, null, null, null),
            (a, s) => Substitute.For<SubmuestraEditViewModel>(null, null, null, null, null),
            _submuestraService,
            _produccionService,
            (a, s) => Substitute.For<ProduccionEditViewModel>(null, null, null, null, null, null),
            _validationService,
            _reportService,
            _jsonImportService,
            _mareaImportService,
            _mapRenderingService,
            _excelReportService,
            _mareaSummaryService,
            _userSettingsService,
            _dbfExporterService,
            _dbContextFactory,
            _configuracionViewModel);
    }
}

public class TestMainWindowViewModel : MainWindowViewModel
{
    public TestMainWindowViewModel(
        IMockShellDataService mockShellDataService,
        IThemeService themeService,
        IMareaService mareaService,
        IBuqueService buqueService,
        ILanceService lanceService,
        IMuestraService muestraService,
        IActiveMareaManager activeMareaManager,
        Func<Action, string?, MareaEditViewModel> mareaEditFactory,
        Func<Action, string, string?, LanceEditViewModel> lanceEditFactory,
        Func<Action, string, string?, MuestraEditViewModel> muestraEditFactory,
        Func<Action, string, SubmuestraEditViewModel> submuestraEditFactory,
        ISubmuestraService submuestraService,
        IProduccionService produccionService,
        Func<Action, string?, ProduccionEditViewModel> produccionEditFactory,
        IMareaValidationService validationService,
        IMareaReportService mareaReportService,
        IJsonImportService jsonImportService,
        IMareaImportService mareaImportService,
        IMapRenderingService mapRenderingService,
        IExcelReportService excelReportService,
        IMareaSummaryService mareaSummaryService,
        IUserSettingsService userSettingsService,
        IDbfExporterService dbfExporterService,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ConfiguracionViewModel configuracionViewModel) 
        : base(mockShellDataService, themeService, mareaService, buqueService, lanceService, muestraService, activeMareaManager, mareaEditFactory, lanceEditFactory, muestraEditFactory, submuestraEditFactory, submuestraService, produccionService, produccionEditFactory, validationService, mareaReportService, jsonImportService, mareaImportService, mapRenderingService, excelReportService, mareaSummaryService, userSettingsService, dbfExporterService, dbContextFactory, configuracionViewModel)
    {
    }

    public Task LoadControlProduccionAsyncInternal() => (Task)typeof(MainWindowViewModel)
        .GetMethod("LoadControlProduccionAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?.Invoke(this, null)!;

    public Task LoadControlLanceDetailsAsyncInternal(ControlProduccionListItemViewModel vm) => (Task)typeof(MainWindowViewModel)
        .GetMethod("LoadControlLanceDetailsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?.Invoke(this, new object[] { vm })!;
}
