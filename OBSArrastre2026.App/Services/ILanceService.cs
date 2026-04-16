using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public interface ILanceService
{
    Task<IReadOnlyList<Lance>> GetLancesAsync(
        string? mareaEtapaId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? nroLance = null,
        string? especieBusqueda = null,
        CancellationToken cancellationToken = default);

    Task<Lance?> GetLanceAsync(string id, CancellationToken cancellationToken = default);
    Task SaveLanceAsync(Lance lance, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Especie>> GetEspeciesAsync(CancellationToken cancellationToken = default);
}
