namespace OBSArrastre2026.App.Services;

public interface IJsonImportService
{
    Task ImportBuquesAsync(string jsonPath);
    Task ImportEspeciesAsync(string jsonPath);
    Task<(int Imported, int Ignored)> ImportMareasAsync(string[] filePaths);
    Task UpdateMareaMetadataAsync(string mareaId, string jsonPath);

    Task<bool> IsBuquesEmptyAsync();
    Task<bool> IsEspeciesEmptyAsync();
}
