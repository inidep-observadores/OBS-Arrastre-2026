using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Models;

namespace OBSArrastre2026.App.ViewModels;

public sealed class CatchItemViewModel : ObservableObject
{
    private readonly ItemCaptura _entity;
    private bool _isExpanded;
    private string _searchText = string.Empty;
    private Especie? _selectedEspecie;
    private readonly IEnumerable<Especie> _allEspecies;

    public CatchItemViewModel(ItemCaptura entity, IEnumerable<Especie> allEspecies)
    {
        _entity = entity;
        _allEspecies = allEspecies;
        _selectedEspecie = entity.Especie;
        
        if (entity.Especie != null)
        {
            _searchText = $"{entity.Especie.NombreVulgar} ({entity.Especie.NombreCientifico})";
        }

        ToggleExpandedCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
        RemoveCommand = new RelayCommand(() => RequestDeletion?.Invoke(this));
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnPropertyChanged(nameof(FilteredEspecies));
                
                // Si el texto se vacía, limpiamos la especie seleccionada
                if (string.IsNullOrWhiteSpace(value))
                {
                    SelectedEspecie = null;
                    IsExpanded = false;
                }
                else
                {
                    // Si el usuario escribe algo que no coincide con la especie actual, abrimos el desplegable
                    string currentName = SelectedEspecie?.NombreVulgar ?? string.Empty;
                    string currentFull = SelectedEspecie != null ? $"{SelectedEspecie.NombreVulgar} ({SelectedEspecie.NombreCientifico})" : string.Empty;
                    
                    if (value != currentName && value != currentFull)
                    {
                        IsExpanded = true;
                    }
                }
            }
        }
    }

    public Especie? SelectedEspecie
    {
        get => _selectedEspecie;
        set
        {
            if (SetProperty(ref _selectedEspecie, value))
            {
                _entity.Especie = value;
                _entity.EspecieID = value?.ID;
                
                if (value != null)
                {
                    // Al seleccionar, actualizamos el texto de búsqueda al nombre vulgar
                    // para que coincida con TextSearch.TextPath="NombreVulgar"
                    _searchText = value.NombreVulgar;
                    OnPropertyChanged(nameof(SearchText));
                    IsExpanded = false;
                }
                
                OnPropertyChanged(nameof(EspecieNombreVulgar));
                OnPropertyChanged(nameof(EspecieNombreCientifico));
            }
        }
    }

    public IEnumerable<Especie> FilteredEspecies
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText) || (SelectedEspecie != null && SearchText == $"{SelectedEspecie.NombreVulgar} ({SelectedEspecie.NombreCientifico})"))
            {
                return _allEspecies.OrderByDescending(e => e.Frecuente).ThenBy(e => e.NombreVulgar).Take(20);
            }

            return _allEspecies
                .Where(e => (e.NombreVulgar?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                             (e.NombreCientifico?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .OrderByDescending(e => e.Frecuente)
                .ThenBy(e => e.NombreVulgar)
                .Take(50);
        }
    }

    public double DatoCaptura
    {
        get => _entity.DatoCaptura;
        set 
        {
            if (_entity.DatoCaptura != value)
            {
                _entity.DatoCaptura = value;
                OnPropertyChanged();
                NotifyParentOfWeightChange?.Invoke();
                NotifyCalculatedWeightsChanged();
            }
        }
    }




    public TipoDatoCaptura TipoDatoCaptura
    {
        get => _entity.TipoDatoCaptura;
        set 
        {
            if (_entity.TipoDatoCaptura != value)
            {
                _entity.TipoDatoCaptura = value;
                OnPropertyChanged();
                NotifyParentOfWeightChange?.Invoke();
                NotifyCalculatedWeightsChanged();
            }
        }
    }




    public double DatoDescarte
    {
        get => _entity.DatoDescarte;
        set 
        {
            if (_entity.DatoDescarte != value)
            {
                _entity.DatoDescarte = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DatoDescartePorcentajeDisplay));
                OnPropertyChanged(nameof(DatoDescarteKilosDisplay));
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }




    public TipoDatoDescarte TipoDatoDescarte
    {
        get => _entity.TipoDatoDescarte;
        set 
        {
            _entity.TipoDatoDescarte = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DatoDescartePorcentajeDisplay));
            OnPropertyChanged(nameof(DatoDescarteKilosDisplay));
            OnPropertyChanged(nameof(SummaryText));
        }
    }


    public int NumeroOrden 
    { 
        get => _entity.NumeroOrden;
        set 
        {
            if (_entity.NumeroOrden != value)
            {
                _entity.NumeroOrden = value;
                OnPropertyChanged();
            }
        }
    }
    public string EspecieNombreVulgar => SelectedEspecie?.NombreVulgar ?? "---";
    public string EspecieNombreCientifico => SelectedEspecie?.NombreCientifico ?? "---";

    public string DatoCapturaDisplay
    {
        get
        {
            string unidad = TipoDatoCaptura == TipoDatoCaptura.Porcentaje ? "%" : "Kg";
            return $"{DatoCaptura:N2} {unidad}";
        }
    }

    public double CapturaTotalKg => _entity.CapturaTotalKgCalculado;

    public string DatoDescartePorcentajeDisplay
    {
        get
        {
            double value = _entity.PorcentDescarteCalculado;
            return value > 0 ? $"{value:N2}%" : "---";
        }
    }

    public string DatoDescarteKilosDisplay
    {
        get
        {
            double value = _entity.PesoDescarteCalculado;
            return value > 0 ? $"{value:N2}" : "---";
        }
    }


    public string SummaryText
    {
        get
        {
            if (SelectedEspecie == null) return "Nueva especie...";
            
            return $"{SelectedEspecie.FullDisplayName} - {DatoCapturaDisplay}";
        }
    }

    public void NotifyCalculatedWeightsChanged()
    {
        OnPropertyChanged(nameof(DatoCapturaDisplay));
        OnPropertyChanged(nameof(CapturaTotalKg));
        OnPropertyChanged(nameof(DatoDescartePorcentajeDisplay));
        OnPropertyChanged(nameof(DatoDescarteKilosDisplay));
        OnPropertyChanged(nameof(SummaryText));
    }

    public ICommand ToggleExpandedCommand { get; }
    public ICommand RemoveCommand { get; }
    public Action<CatchItemViewModel>? RequestDeletion { get; set; }
    public Action? NotifyParentOfWeightChange { get; set; }


    public ItemCaptura ToEntity() => _entity;
}
