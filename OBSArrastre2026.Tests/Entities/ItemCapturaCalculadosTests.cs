using FluentAssertions;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Models;

namespace OBSArrastre2026.Tests.Entities;

public sealed class ItemCapturaCalculadosTests
{
    // ── CapturaTotalKgCalculado ──────────────────────────────────────────────

    [Fact]
    public void CapturaTotalKg_TipoKilogramos_DevuelveElValorDirecto()
    {
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Kilogramos,
            DatoCaptura = 250.5
        };

        item.CapturaTotalKgCalculado.Should().Be(250.5);
    }

    [Fact]
    public void CapturaTotalKg_TipoPorcentaje_CalculaCorrectamente()
    {
        var lance = new Lance { CapturaTotalKg = 1000 };
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Porcentaje,
            DatoCaptura = 30,    // 30%
            Lance = lance
        };

        item.CapturaTotalKgCalculado.Should().Be(300);
    }

    [Fact]
    public void CapturaTotalKg_TipoPorcentaje_SinLance_DevuelveCero()
    {
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Porcentaje,
            DatoCaptura = 30
        };

        item.CapturaTotalKgCalculado.Should().Be(0);
    }

    [Fact]
    public void CapturaTotalKg_TipoPorcentaje_LanceCapturaCero_DevuelveCero()
    {
        var lance = new Lance { CapturaTotalKg = 0 };
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Porcentaje,
            DatoCaptura = 50,
            Lance = lance
        };

        item.CapturaTotalKgCalculado.Should().Be(0);
    }

    [Fact]
    public void CapturaTotalKg_TipoMuestra_CalculaProporcion()
    {
        // 2 items de muestra: 200g y 800g → total muestras = 1000g
        // CapturaTotalKg = 5000 kg, sin items de Kg/porcentaje → disponibleParaMuestras = 5000
        // item con 200g de muestra → 5000 * 200 / 1000 = 1000 kg
        var lance = new Lance { CapturaTotalKg = 5000 };
        var item1 = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Muestra,
            DatoCaptura = 200,
            Lance = lance
        };
        var item2 = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Muestra,
            DatoCaptura = 800,
            Lance = lance
        };
        lance.ItemsCaptura.Add(item1);
        lance.ItemsCaptura.Add(item2);

        item1.CapturaTotalKgCalculado.Should().Be(1000);
        item2.CapturaTotalKgCalculado.Should().Be(4000);
    }

    [Fact]
    public void CapturaTotalKg_TipoMuestra_SinLance_DevuelveCero()
    {
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Muestra,
            DatoCaptura = 500
        };

        item.CapturaTotalKgCalculado.Should().Be(0);
    }

    [Fact]
    public void CapturaTotalKg_TipoPorcentaje_RedondeaADosDecimales()
    {
        var lance = new Lance { CapturaTotalKg = 1000 };
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Porcentaje,
            DatoCaptura = 33.33,   // 33.33% de 1000 = 333.3
            Lance = lance
        };

        item.CapturaTotalKgCalculado.Should().Be(Math.Round(333.3, 2));
    }

    // ── PesoDescarteCalculado ────────────────────────────────────────────────

    [Fact]
    public void PesoDescarte_TipoKilogramos_DevuelveValorDirecto()
    {
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Kilogramos,
            DatoCaptura = 100,
            TipoDatoDescarte = TipoDatoDescarte.Kilogramos,
            DatoDescarte = 25
        };

        item.PesoDescarteCalculado.Should().Be(25);
    }

    [Fact]
    public void PesoDescarte_TipoPorcentaje_CalculaCorrectamente()
    {
        var lance = new Lance { CapturaTotalKg = 1000 };
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Kilogramos,
            DatoCaptura = 500,
            TipoDatoDescarte = TipoDatoDescarte.Porcentaje,
            DatoDescarte = 20    // 20% de 500 kg captura = 100 kg descarte
        };

        item.PesoDescarteCalculado.Should().Be(100);
    }

    [Fact]
    public void PesoDescarte_CapturaTotal_Cero_DevuelveCero()
    {
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Kilogramos,
            DatoCaptura = 0,
            TipoDatoDescarte = TipoDatoDescarte.Porcentaje,
            DatoDescarte = 50
        };

        item.PesoDescarteCalculado.Should().Be(0);
    }

    // ── PorcentDescarteCalculado ─────────────────────────────────────────────

    [Fact]
    public void PorcentDescarte_TipoPorcentaje_DevuelveValorDirecto()
    {
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Kilogramos,
            DatoCaptura = 500,
            TipoDatoDescarte = TipoDatoDescarte.Porcentaje,
            DatoDescarte = 15
        };

        item.PorcentDescarteCalculado.Should().Be(15);
    }

    [Fact]
    public void PorcentDescarte_TipoKilogramos_CalculaPorcentaje()
    {
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Kilogramos,
            DatoCaptura = 500,
            TipoDatoDescarte = TipoDatoDescarte.Kilogramos,
            DatoDescarte = 100   // 100 kg / 500 kg = 20%
        };

        item.PorcentDescarteCalculado.Should().Be(20);
    }

    [Fact]
    public void PorcentDescarte_DescarteCero_DevuelveCero()
    {
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Kilogramos,
            DatoCaptura = 500,
            TipoDatoDescarte = TipoDatoDescarte.Kilogramos,
            DatoDescarte = 0
        };

        item.PorcentDescarteCalculado.Should().Be(0);
    }

    // ── PesoMuestra ──────────────────────────────────────────────────────────

    [Fact]
    public void PesoMuestra_TipoMuestra_DevuelveElDatoCaptura()
    {
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Muestra,
            DatoCaptura = 350
        };

        item.PesoMuestra.Should().Be(350);
    }

    [Fact]
    public void PesoMuestra_TipoNoMuestra_DevuelveCero()
    {
        var item = new ItemCaptura
        {
            TipoDatoCaptura = TipoDatoCaptura.Kilogramos,
            DatoCaptura = 350
        };

        item.PesoMuestra.Should().Be(0);
    }
}
