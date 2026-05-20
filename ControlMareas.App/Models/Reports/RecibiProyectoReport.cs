using System;
using System.Collections.Generic;

namespace ControlMareas.App.Models.Reports;

public class RecibiProyectoReport
{
    public string BuqueNombre { get; set; } = string.Empty;
    public string MareaNumero { get; set; } = string.Empty;
    public int MareaAnio { get; set; }
    public string ObservadorNombreCompleto { get; set; } = string.Empty;
    
    public string Otolitos { get; set; } = "N";
    public string Escamas { get; set; } = "N";
    public string Gonadas { get; set; } = "N";

    public List<RecibiProyectoEspecieItem> Especies { get; set; } = new();

    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
}

public class RecibiProyectoEspecieItem
{
    public string NombreEspecie { get; set; } = string.Empty;
    public string NombreCientifico { get; set; } = string.Empty;
    public List<RecibiProyectoLanceItem> Lances { get; set; } = new();
}

public class RecibiProyectoLanceItem
{
    public int NroLance { get; set; }
    public string Area { get; set; } = string.Empty;
}
