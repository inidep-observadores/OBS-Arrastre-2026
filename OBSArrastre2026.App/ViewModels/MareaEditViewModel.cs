using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentValidation;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services;

namespace OBSArrastre2026.App.ViewModels;

public sealed partial class MareaEditViewModel : ValidatableViewModelBase<MareaEditViewModel>
{
    private readonly Action _onClose;
    private int _anioInidep;
    private int _numeroInidep;
    private string? _codigo;
    private string? _buqueID;
    private DateTime _fechaInicio;
    private DateTime? _fechaFin;
    private string? _comentarios;
    private Buque? _selectedBuque;

    public MareaEditViewModel(Action onClose, IValidator<MareaEditViewModel> validator) : base(validator)
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

        ValidateAll();
    }

    public ObservableCollection<MareaEtapaItemViewModel> Etapas { get; } = [];

    public string? Codigo
    {
        get => _codigo;
        set => SetProperty(ref _codigo, value);
    }

    public string? Comentarios
    {
        get => _comentarios;
        set => SetProperty(ref _comentarios, value);
    }

    public int AnioInidep
    {
        get => _anioInidep;
        set 
        {
            if (SetProperty(ref _anioInidep, value))
                ValidatePropertyWithFluent(value, nameof(AnioInidep));
        }
    }

    public int NumeroInidep
    {
        get => _numeroInidep;
        set 
        {
            if (SetProperty(ref _numeroInidep, value))
                ValidatePropertyWithFluent(value, nameof(NumeroInidep));
        }
    }

    public string? BuqueID
    {
        get => _buqueID;
        set => SetProperty(ref _buqueID, value);
    }

    public DateTime FechaInicio
    {
        get => _fechaInicio;
        set 
        {
            if (SetProperty(ref _fechaInicio, value))
                ValidatePropertyWithFluent(value, nameof(FechaInicio));
        }
    }

    public DateTime? FechaFin
    {
        get => _fechaFin;
        set 
        {
            if (SetProperty(ref _fechaFin, value))
            {
                ValidatePropertyWithFluent(value, nameof(FechaFin));
                ValidatePropertyWithFluent(FechaInicio, nameof(FechaInicio));
            }
        }
    }

    public Buque? SelectedBuque
    {
        get => _selectedBuque;
        set 
        {
            if (SetProperty(ref _selectedBuque, value))
            {
                BuqueID = value?.Id;
                ValidatePropertyWithFluent(value, nameof(SelectedBuque));
            }
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddEtapaCommand { get; }

    private void AddEtapa()
    {
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
        if (ValidateAll())
        {
            _onClose();
        }
    }

    private void Cancel()
    {
        _onClose();
    }
}
