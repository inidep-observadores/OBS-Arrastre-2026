using System.ComponentModel.DataAnnotations.Schema;
using OBSArrastre2026.App.Models;

namespace OBSArrastre2026.App.Data.Entities;

public sealed class ItemCaptura
{
    public string ID { get; set; } = Guid.NewGuid().ToString();

    public string? LanceID { get; set; }
    public Lance? Lance { get; set; }

    public string? EspecieID { get; set; }
    public string? EspecieOriginal { get; set; }
    public Especie? Especie { get; set; }

    public int NumeroOrden { get; set; }
    public TipoDatoCaptura TipoDatoCaptura { get; set; }
    public double DatoCaptura { get; set; }
    public TipoDatoDescarte TipoDatoDescarte { get; set; }
    public double DatoDescarte { get; set; }
    public string? Metadata { get; set; }

    [NotMapped]
    public double CapturaTotalKgCalculado
    {
        get
        {
            if (TipoDatoCaptura == TipoDatoCaptura.Kilogramos)
            {
                return DatoCaptura;
            }

            if (TipoDatoCaptura == TipoDatoCaptura.Porcentaje)
            {
                if (Lance?.CapturaTotalKg > 0)
                {
                    return Math.Round(DatoCaptura * Lance.CapturaTotalKg.Value / 100.0, 2);
                }
                return 0;
            }

            if (TipoDatoCaptura == TipoDatoCaptura.Muestra)
            {
                if (Lance != null && Lance.SumaPesoMuestras > 0 && Lance.CapturaTotalKg > 0)
                {
                    double disponibleParaMuestras = Lance.CapturaTotalKg.Value - Lance.SumaPesoCapturadoNoMuestreado;
                    if (disponibleParaMuestras < 0) disponibleParaMuestras = 0;
                    return Math.Round(disponibleParaMuestras * DatoCaptura / Lance.SumaPesoMuestras, 2);
                }
                return 0;
            }

            return 0;
        }
    }

    [NotMapped]
    public double PesoMuestra => TipoDatoCaptura == TipoDatoCaptura.Muestra ? DatoCaptura : 0;

    [NotMapped]
    public double PesoDescarteCalculado
    {
        get
        {
            if (TipoDatoDescarte == TipoDatoDescarte.Kilogramos)
            {
                return DatoDescarte;
            }

            double capturaTotal = CapturaTotalKgCalculado;
            if (capturaTotal > 0)
            {
                return Math.Round(DatoDescarte * capturaTotal / 100.0, 2);
            }

            return 0;
        }
    }

    [NotMapped]
    public double PorcentDescarteCalculado
    {
        get
        {
            if (TipoDatoDescarte == TipoDatoDescarte.Porcentaje)
            {
                return DatoDescarte;
            }

            double capturaTotal = CapturaTotalKgCalculado;
            if (capturaTotal > 0 && DatoDescarte > 0)
            {
                return Math.Round(DatoDescarte * 100.0 / capturaTotal, 2);
            }

            return 0;
        }
    }
}

