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
}

