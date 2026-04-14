using System.Windows;

namespace OBSArrastre2026.App.Models;

public sealed class UserSettings
{
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;
    
    public WindowState WindowState { get; set; } = WindowState.Maximized;
}
