using System;
using System.Collections.Generic;

namespace OBSArrastre2026.App.Models;

public record DbfExportSummary
{
    public TimeSpan TotalTime { get; init; }
    public Dictionary<string, TimeSpan> StageTimings { get; init; } = new();
    public int TotalRecords { get; init; }
}
