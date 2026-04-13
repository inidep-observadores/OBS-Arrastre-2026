namespace OBSArrastre2026.App.Services;

public interface IDbfExtractorService
{
    Task ExtractBuquesAsync(string dbfPath, string jsonOutputPath);
    Task ExtractEspeciesAsync(string dbfPath, string jsonOutputPath);
}
