using System.Collections.Generic;
using System.Threading.Tasks;

namespace OBSArrastre2026.App.Services
{
    public interface IMapRenderingService
    {
        /// <summary>
        /// Genera un mapa estático en formato PNG a partir de una lista de coordenadas.
        /// </summary>
        /// <param name="latitudes">Lista de latitudes.</param>
        /// <param name="longitudes">Lista de longitudes.</param>
        /// <param name="outputPath">Ruta donde se guardará la imagen generada.</param>
        /// <returns>Tarea que representa la operación asíncrona.</returns>
        Task RenderMapAsync(IEnumerable<double> latitudes, IEnumerable<double> longitudes, string outputPath);

        /// <summary>
        /// Genera un mapa estático en formato PNG y devuelve los bytes.
        /// </summary>
        Task<byte[]> RenderMapToBytesAsync(IEnumerable<double> latitudes, IEnumerable<double> longitudes);
    }
}
