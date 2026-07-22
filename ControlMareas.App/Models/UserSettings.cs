using System.Windows;

namespace ControlMareas.App.Models;

public sealed class UserSettings
{
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;
    
    public WindowState WindowState { get; set; } = WindowState.Maximized;
    public string? RevisorNombre { get; set; }
    public string? RevisorApellido { get; set; }
    public string? ActiveMareaId { get; set; }
    public int? LastSelectedMareaAnio { get; set; }
    public bool FiltrarDiferenciasAuditoria { get; set; } = true;
    public double ToleranciaFiltroAuditoria { get; set; } = 2.0;
    public bool OmitirValidacionMuestraSubmuestra { get; set; } = false;
}
