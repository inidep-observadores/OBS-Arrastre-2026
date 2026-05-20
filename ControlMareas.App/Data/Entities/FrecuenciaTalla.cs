namespace ControlMareas.App.Data.Entities;

public sealed class FrecuenciaTalla
{
    public string ID { get; set; } = Guid.NewGuid().ToString();

    public string? MuestraID { get; set; }
    public Muestra? Muestra { get; set; }

    public double Talla { get; set; }
    public int NroMachos { get; set; }
    public int NroHembras { get; set; }
    public int NroIndeterminados { get; set; }
    public int NroTotal { get; set; }

    public int NroLangostinosMachoMaduros { get; set; }
    public int NroLangostinosHembraMaduras { get; set; }
    public int NroLangostinosHembraImpregnadas { get; set; }
    public string? Metadata { get; set; }
}
