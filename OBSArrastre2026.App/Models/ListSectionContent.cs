using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.App.Models;

public sealed record ListSectionContent(
    string Eyebrow,
    string Title,
    string Description,
    string PrimaryActionLabel,
    string Column1Header,
    string Column2Header,
    string Column3Header,
    string Column4Header,
    string Column5Header,
    string Column6Header,
    string Column7Header,
    IReadOnlyList<string> Filters,
    IReadOnlyList<MockRecordRowViewModel> Rows);
