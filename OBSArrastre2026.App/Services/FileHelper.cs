using System;
using System.IO;

namespace OBSArrastre2026.App.Services;

public static class FileHelper
{
    public static bool IsFileWritable(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        // Si el archivo no existe, significa que el destino está libre para creación
        if (!File.Exists(filePath))
            return true;

        try
        {
            // Intentar abrir el archivo de forma exclusiva para escritura
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                return true;
            }
        }
        catch (IOException)
        {
            // Ocurre cuando el archivo está siendo usado por otro proceso (bloqueado)
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // Ocurre por problemas de permisos de escritura o atributos de solo lectura
            return false;
        }
        catch (Exception)
        {
            // Cualquier otro error de acceso
            return false;
        }
    }
}
