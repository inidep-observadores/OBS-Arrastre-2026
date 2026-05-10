using OBSArrastre2026.App.Models.Import;

namespace OBSArrastre2026.App.Services;

public interface IDbfExtractorService
{
    Task ExtractBuquesAsync(string dbfPath, string jsonOutputPath);
    Task ExtractEspeciesAsync(string dbfPath, string jsonOutputPath);
    Task<List<LegacyCaptura>> ReadCapturasAsync(string dbfPath);
    Task<List<LegacyMuestra>> ReadMuestrasAsync(string dbfPath);
    Task<List<LegacySubmuestra>> ReadSubmuestrasAsync(string dbfPath);
    Task<List<LegacyLg>> ReadLgAsync(string dbfPath);
    Task<List<LegacyTracking>> ReadTrackingAsync(string dbfPath);
    Task<List<LegacyProduccion>> ReadProduccionAsync(string dbfPath);
    Task<System.Text.Encoding> DetectEncodingSmartAsync(string dbfPath);
}
