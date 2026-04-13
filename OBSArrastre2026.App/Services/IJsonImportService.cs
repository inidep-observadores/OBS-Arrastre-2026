namespace OBSArrastre2026.App.Services;

public interface IJsonImportService
{
    Task ImportBuquesAsync(string jsonPath);
    Task ImportEspeciesAsync(string jsonPath);
}
