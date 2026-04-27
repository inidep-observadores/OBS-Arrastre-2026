using System;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

namespace OBSArrastre2026.App.Infrastructure.MapProviders
{
    /// <summary>
    /// Implementación de GMapProvider para Argenmap (IGN Argentina) usando el estándar TMS.
    /// </summary>
    public class ArgenmapProvider : GMapProvider
    {
        public static readonly ArgenmapProvider Instance;

        static ArgenmapProvider()
        {
            Instance = new ArgenmapProvider();
        }

        ArgenmapProvider()
        {
            RefererUrl = "https://www.ign.gob.ar/";
            Copyright = string.Format("© {0} Instituto Geográfico Nacional", DateTime.Today.Year);
        }

        public override Guid Id { get; } = new Guid("7E4F9B1A-5D2C-4B3E-8A1F-9D0C3B2A1E0F");

        public override string Name { get; } = "Argenmap";

        public override GMapProvider[] Overlays { get; } = null;

        public override PureProjection Projection { get; } = MercatorProjection.Instance;

        public override PureImage GetTileImage(GPoint pos, int zoom)
        {
            string url = MakeTileUrl(pos, zoom);
            return GetTileImageUsingHttp(url);
        }

        private string MakeTileUrl(GPoint pos, int zoom)
        {
            // El IGN requiere el endpoint específico de la capa para mayor estabilidad y el prefijo EPSG:3857
            return string.Format("https://wms.ign.gob.ar/geoserver/capabaseargenmap/gwc/service/wmts?service=WMTS&request=GetTile&version=1.0.0&layer=capabaseargenmap&style=default&tilematrixset=EPSG:3857&format=image/png&tilematrix=EPSG:3857:{0}&tilerow={1}&tilecol={2}", 
                zoom, pos.Y, pos.X);
        }
    }
}
