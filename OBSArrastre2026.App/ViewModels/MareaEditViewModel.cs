using System.Collections.ObjectModel;
using System.Windows.Input;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MareaEditViewModel : ObservableObject
{
    private readonly Action _onClose;
    private int _anioInidep;
    private int _numeroInidep;
    private string? _codigo;
    private string? _buqueID;
    private DateTime _fechaInicio;
    private DateTime? _fechaFin;
    private string? _comentarios;

    public MareaEditViewModel(Action onClose)
    {
        _onClose = onClose;
        _fechaInicio = DateTime.Today;

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        AddEtapaCommand = new RelayCommand(AddEtapa);

        // Mock de datos iniciales
        _anioInidep = 2026;
        _numeroInidep = 14;
        
        // Añadimos una etapa inicial mock
        AddEtapa();
        if (Etapas.Count > 0) Etapas[0].IsExpanded = true;
    }

    public ObservableCollection<MareaEtapaItemViewModel> Etapas { get; } = [];

    public int AnioInidep
    {
        get => _anioInidep;
        set => SetProperty(ref _anioInidep, value);
    }

    public int NumeroInidep
    {
        get => _numeroInidep;
        set => SetProperty(ref _numeroInidep, value);
    }

    public string? Codigo
    {
        get => _codigo;
        set => SetProperty(ref _codigo, value);
    }

    public string? BuqueID
    {
        get => _buqueID;
        set => SetProperty(ref _buqueID, value);
    }

    public DateTime FechaInicio
    {
        get => _fechaInicio;
        set => SetProperty(ref _fechaInicio, value);
    }

    public DateTime? FechaFin
    {
        get => _fechaFin;
        set => SetProperty(ref _fechaFin, value);
    }

    public string? Comentarios
    {
        get => _comentarios;
        set => SetProperty(ref _comentarios, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddEtapaCommand { get; }

    private void AddEtapa()
    {
        // Colapsamos todas antes de añadir
        foreach (var e in Etapas) e.IsExpanded = false;

        var nuevaEtapa = new MareaEtapa 
        { 
            FechaZarpada = DateTime.Today,
            NombreCapitan = "Capitán Mock"
        };
        var vm = new MareaEtapaItemViewModel(nuevaEtapa) { IsExpanded = true };
        Etapas.Add(vm);
    }

    private void Save()
    {
        // Por ahora solo cerramos (mockup)
        _onClose();
    }

    private void Cancel()
    {
        _onClose();
    }
}
