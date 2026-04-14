using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public interface IMareaService
{
    Task<IReadOnlyList<int>> GetAniosExistentesAsync(CancellationToken cancellationToken = default);
    Task<Marea?> GetMareaAsync(string id, CancellationToken cancellationToken = default);
    Task SaveMareaAsync(Marea marea, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Marea>> GetMareasAsync(
        int? anio = null,
        string? buqueId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? busquedaTextual = null,
        CancellationToken cancellationToken = default);
}
