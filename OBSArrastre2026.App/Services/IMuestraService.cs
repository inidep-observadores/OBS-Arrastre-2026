using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public interface IMuestraService
{
    Task<IReadOnlyList<Muestra>> GetMuestrasAsync(string lanceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Muestra>> GetMuestrasPorMareaAsync(string mareaId, CancellationToken cancellationToken = default);
    Task<Muestra?> GetMuestraAsync(string id, CancellationToken cancellationToken = default);
    Task SaveMuestraAsync(Muestra muestra, CancellationToken cancellationToken = default);
    Task DeleteMuestraAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EspecieLargoPeso>> GetParametrosAlometricosAsync(string especieId, CancellationToken cancellationToken = default);
}
