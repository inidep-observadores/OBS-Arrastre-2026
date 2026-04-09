using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.App.Models;

public sealed record DashboardContent(
    string Eyebrow,
    string Title,
    string Description,
    string PrimaryActionLabel,
    IReadOnlyList<KpiCardViewModel> Cards,
    IReadOnlyList<InsightCardViewModel> Insights,
    IReadOnlyList<TrendPointViewModel> Trends);
