using System;
using System.IO;
using System.Text.Json;
using ControlMareas.App.Models;

namespace ControlMareas.App.Services;

public interface IUserSettingsService
{
    UserSettings GetSettings();
    void SaveSettings(UserSettings settings);
    void UpdateSettings(Action<UserSettings> action);
}

public sealed class UserSettingsService : IUserSettingsService
{
    private readonly string _settingsPath;
    private UserSettings? _cachedSettings;

    public UserSettingsService(IDatabasePathProvider pathProvider)
    {
        // Reutilizamos la carpeta base del proveedor de base de datos para centralizar la configuración
        var dbPath = pathProvider.GetDatabasePath();
        var baseFolder = Path.GetDirectoryName(dbPath)!;
        _settingsPath = Path.Combine(baseFolder, "settings.json");
    }

    public UserSettings GetSettings()
    {
        if (_cachedSettings != null) return _cachedSettings;

        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                _cachedSettings = JsonSerializer.Deserialize<UserSettings>(json);
            }
            catch
            {
                // Si el archivo está corrupto o hay un error, se ignorará y se usarán valores por defecto
            }
        }

        _cachedSettings ??= new UserSettings();
        return _cachedSettings;
    }

    public void SaveSettings(UserSettings settings)
    {
        _cachedSettings = settings;
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Errores de escritura (ej. permisos) se capturan aquí para evitar cierres inesperados
        }
    }

    public void UpdateSettings(Action<UserSettings> action)
    {
        var settings = GetSettings();
        action(settings);
        SaveSettings(settings);
    }
}
