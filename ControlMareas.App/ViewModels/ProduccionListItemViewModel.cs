using System;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.ViewModels;

public sealed class ProduccionListItemViewModel(RegistroProduccion registro) : ObservableObject
{
    public RegistroProduccion Registro => registro;

    public string FechaDisplay
    {
        get
        {
            if (DateTime.TryParse(registro.Fecha, out var dt))
                return dt.ToString("dd/MM/yyyy");
            return registro.Fecha;
        }
    }
    
    public string EspecieDisplay => registro.Especie?.FullDisplayName ?? "-";
    
    public string ProductoCodigo => registro.Producto?.Codigo ?? "-";
    
    public string Categoria => registro.Categoria ?? "-";

    public string ProductoDescripcion => registro.Producto?.Descripcion ?? "-";

    public double Factor => registro.Factor ?? 0;
    
    public double Kg => registro.Kg ?? 0;

    public string ID => registro.Id;
}
