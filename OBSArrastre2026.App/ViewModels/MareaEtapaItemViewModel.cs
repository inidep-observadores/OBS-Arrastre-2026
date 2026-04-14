using System.Windows.Input;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MareaEtapaItemViewModel : ObservableObject
{
    private readonly string _id;
    private bool _isExpanded;
    private DateTime _fechaZarpada;
    private DateTime? _fechaArribo;
    private string? _nombreCapitan;
    private string? _nombreOficialCubierta;
    private string? _nombreOficialPesca;

    public MareaEtapaItemViewModel(MareaEtapa mareaEtapa)
    {
        _id = mareaEtapa.ID;
        _fechaZarpada = mareaEtapa.FechaZarpada;
        _fechaArribo = mareaEtapa.FechaArribo;
        _nombreCapitan = mareaEtapa.NombreCapitan;
        _nombreOficialCubierta = mareaEtapa.NombreOficialCubierta;
        _nombreOficialPesca = mareaEtapa.NombreOficialPesca;

        ToggleExpandedCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
        RemoveCommand = new RelayCommand(() => RequestDeletion?.Invoke(this));
    }

    public string ID => _id;
    public Action<MareaEtapaItemViewModel>? RequestDeletion { get; set; }
    public ICommand RemoveCommand { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public DateTime FechaZarpada
    {
        get => _fechaZarpada;
        set 
        {
            if (SetProperty(ref _fechaZarpada, value))
            {
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }

    public DateTime? FechaArribo
    {
        get => _fechaArribo;
        set 
        {
            if (SetProperty(ref _fechaArribo, value))
            {
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }

    public string? NombreCapitan
    {
        get => _nombreCapitan;
        set 
        {
            if (SetProperty(ref _nombreCapitan, value))
            {
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }

    public string? NombreOficialCubierta
    {
        get => _nombreOficialCubierta;
        set => SetProperty(ref _nombreOficialCubierta, value);
    }

    public string? NombreOficialPesca
    {
        get => _nombreOficialPesca;
        set => SetProperty(ref _nombreOficialPesca, value);
    }

    public string SummaryText => $"{FechaZarpada:dd/MM/yyyy} - {(FechaArribo.HasValue ? FechaArribo.Value.ToString("dd/MM/yyyy") : "En curso")} | Cap. {NombreCapitan ?? "S/D"}";

    public ICommand ToggleExpandedCommand { get; }

    public MareaEtapa ToEntity() => new()
    {
        ID = _id,
        FechaZarpada = FechaZarpada,
        FechaArribo = FechaArribo,
        NombreCapitan = NombreCapitan,
        NombreOficialCubierta = NombreOficialCubierta,
        NombreOficialPesca = NombreOficialPesca
    };
}
