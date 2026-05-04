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

    Task<bool> HasExistingDataAsync(string mareaId, CancellationToken cancellationToken = default);
    Task ClearMareaDataAsync(string mareaId, CancellationToken cancellationToken = default);
    Task DeleteMareaAsync(string id, CancellationToken cancellationToken = default);
    Task<Marea?> FindMareaAsync(int numero, int anio, CancellationToken cancellationToken = default);
}
