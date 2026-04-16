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
                    _searchText = $"{value.NombreVulgar} ({value.NombreCientifico})";
                    OnPropertyChanged(nameof(SearchText));
                    OnPropertyChanged(nameof(SummaryText));
                }
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
            _entity.DatoCaptura = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SummaryText));
        }
    }

    public TipoDatoCaptura TipoDatoCaptura
    {
        get => (TipoDatoCaptura)_entity.TipoDatoCaptura;
        set 
        {
            _entity.TipoDatoCaptura = (int)value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SummaryText));
        }
    }

    public double DatoDescarte
    {
        get => _entity.DatoDescarte;
        set 
        {
            _entity.DatoDescarte = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SummaryText));
        }
    }

    public TipoDatoDescarte TipoDatoDescarte
    {
        get => (TipoDatoDescarte)_entity.TipoDatoDescarte;
        set 
        {
            _entity.TipoDatoDescarte = (int)value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SummaryText));
        }
    }

    public int NumeroOrden => _entity.NumeroOrden;
    public string EspecieNombreVulgar => SelectedEspecie?.NombreVulgar ?? "---";
    public string EspecieNombreCientifico => SelectedEspecie?.NombreCientifico ?? "---";

    public string DatoCapturaDisplay
    {
        get
        {
            string unidad = TipoDatoCaptura == TipoDatoCaptura.Kilogramos ? "Kg" : "%";
            return $"{DatoCaptura:N2} {unidad}";
        }
    }

    public string DatoDescartePorcentajeDisplay
    {
        get
        {
            if (TipoDatoDescarte == TipoDatoDescarte.Porcentaje)
                return $"{DatoDescarte:N2}%";
            
            return string.Empty; // Según el usuario, si cargó en kilos no se muestra el porcentaje
        }
    }

    public string DatoDescarteKilosDisplay
    {
        get
        {
            if (TipoDatoDescarte == TipoDatoDescarte.Kilogramos)
                return $"{DatoDescarte:N2}";

            // Si es porcentaje, intentamos calcular sobre la captura de esta especie (si está en kilos)
            if (TipoDatoDescarte == TipoDatoDescarte.Porcentaje && TipoDatoCaptura == TipoDatoCaptura.Kilogramos)
            {
                return $"{(DatoCaptura * DatoDescarte / 100.0):N2}";
            }

            return "---";
        }
    }

    public string SummaryText
    {
        get
        {
            if (SelectedEspecie == null) return "Nueva especie...";
            
            return $"{SelectedEspecie.NombreVulgar} - {DatoCapturaDisplay}";
        }
    }

    public ICommand ToggleExpandedCommand { get; }
    public ICommand RemoveCommand { get; }
    public Action<CatchItemViewModel>? RequestDeletion { get; set; }

    public ItemCaptura ToEntity() => _entity;
}
