using FluentAssertions;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Models;

namespace OBSArrastre2026.Tests.Entities;

public sealed class LanceAgregadosTests
{
    [Fact]
    public void SumaPesoMuestras_SoloItemsTipoMuestra_SumaCorrectamente()
    {
        var lance = new Lance { CapturaTotalKg = 5000 };
        lance.ItemsCaptura.Add(new ItemCaptura { TipoDatoCaptura = TipoDatoCaptura.Muestra, DatoCaptura = 300, Lance = lance });
        lance.ItemsCaptura.Add(new ItemCaptura { TipoDatoCaptura = TipoDatoCaptura.Muestra, DatoCaptura = 700, Lance = lance });

        lance.SumaPesoMuestras.Should().Be(1000);
    }

    [Fact]
    public void SumaPesoMuestras_SinItemsMuestra_EsCero()
    {
        var lance = new Lance { CapturaTotalKg = 1000 };
        lance.ItemsCaptura.Add(new ItemCaptura { TipoDatoCaptura = TipoDatoCaptura.Kilogramos, DatoCaptura = 500, Lance = lance });

        lance.SumaPesoMuestras.Should().Be(0);
    }

    [Fact]
    public void SumaPesoMuestras_ColeccionVacia_EsCero()
    {
        var lance = new Lance();

        lance.SumaPesoMuestras.Should().Be(0);
    }

    [Fact]
    public void SumaPesoMuestras_MezclaDeItems_SoloSumaLosDeMuestra()
    {
        var lance = new Lance { CapturaTotalKg = 2000 };
        lance.ItemsCaptura.Add(new ItemCaptura { TipoDatoCaptura = TipoDatoCaptura.Muestra, DatoCaptura = 400, Lance = lance });
        lance.ItemsCaptura.Add(new ItemCaptura { TipoDatoCaptura = TipoDatoCaptura.Kilogramos, DatoCaptura = 1000, Lance = lance });
        lance.ItemsCaptura.Add(new ItemCaptura { TipoDatoCaptura = TipoDatoCaptura.Porcentaje, DatoCaptura = 30, Lance = lance });

        lance.SumaPesoMuestras.Should().Be(400);
    }

    [Fact]
    public void SumaPesoCapturadoNoMuestreado_ItemsKg_SumaCorrectamente()
    {
        var lance = new Lance { CapturaTotalKg = 5000 };
        lance.ItemsCaptura.Add(new ItemCaptura { TipoDatoCaptura = TipoDatoCaptura.Kilogramos, DatoCaptura = 1200, Lance = lance });
        lance.ItemsCaptura.Add(new ItemCaptura { TipoDatoCaptura = TipoDatoCaptura.Kilogramos, DatoCaptura = 800, Lance = lance });

        lance.SumaPesoCapturadoNoMuestreado.Should().Be(2000);
    }

    [Fact]
    public void SumaPesoCapturadoNoMuestreado_ItemsPorcentaje_CalculaDesdeTotal()
    {
        var lance = new Lance { CapturaTotalKg = 1000 };
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Porcentaje,
            DatoCaptura = 40,   // 40% de 1000 = 400 kg
            Lance = lance
        };
        lance.ItemsCaptura.Add(item);

        lance.SumaPesoCapturadoNoMuestreado.Should().Be(400);
    }

    [Fact]
    public void SumaPesoCapturadoNoMuestreado_NoIncluye_ItemsDeMuestra()
    {
        var lance = new Lance { CapturaTotalKg = 5000 };
        lance.ItemsCaptura.Add(new ItemCaptura { TipoDatoCaptura = TipoDatoCaptura.Muestra, DatoCaptura = 500, Lance = lance });
        lance.ItemsCaptura.Add(new ItemCaptura { TipoDatoCaptura = TipoDatoCaptura.Kilogramos, DatoCaptura = 300, Lance = lance });

        lance.SumaPesoCapturadoNoMuestreado.Should().Be(300);
    }

    [Fact]
    public void SumaPesoCapturadoNoMuestreado_ColeccionVacia_EsCero()
    {
        var lance = new Lance();

        lance.SumaPesoCapturadoNoMuestreado.Should().Be(0);
    }
}
