using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public interface IProduccionService
{
    Task<IReadOnlyList<RegistroProduccion>> GetRegistrosProduccionAsync(string mareaEtapaId, CancellationToken cancellationToken = default);
    Task<RegistroProduccion?> GetRegistroProduccionAsync(string id, CancellationToken cancellationToken = default);
    Task SaveRegistroProduccionAsync(RegistroProduccion registro, CancellationToken cancellationToken = default);
    Task DeleteRegistroProduccionAsync(string id, CancellationToken cancellationToken = default);
}
