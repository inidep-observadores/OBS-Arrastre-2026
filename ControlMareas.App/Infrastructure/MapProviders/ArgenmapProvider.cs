using System;
using System.Globalization;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

namespace ControlMareas.App.Infrastructure.MapProviders
{
    /// <summary>
    /// Proveedor de tiles de Argenmap (IGN Argentina) para GMap.NET.
    /// Utiliza el servicio WMTS oficial del Instituto Geográfico Nacional.
    /// </summary>
    public class ArgenmapProvider : GMapProvider
    {
        public static readonly ArgenmapProvider Instance = new ArgenmapProvider();

        public ArgenmapProvider()
        {
            MaxZoom = 20;
            MinZoom = 1;
            RefererUrl = "https://www.ign.gob.ar/";
            Copyright = string.Format("© {0} Instituto Geográfico Nacional Argentina", DateTime.Today.Year);
            GMapProvider.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        }

        public override Guid Id => new Guid("B8A1F9D0-C3B2-4A1E-8A1F-9D0C3B2A1E0F");
        public override string Name => "ArgenmapIGN";
        public override PureProjection Projection => MercatorProjection.Instance;

        // Requerido por GMap.NET: debe retornar el proveedor que genera los tiles
        public override GMapProvider[] Overlays => new GMapProvider[] { this };

        public override PureImage GetTileImage(GPoint pos, int zoom)
        {
            string url = string.Format(
                CultureInfo.InvariantCulture,
                "https://wms.ign.gob.ar/geoserver/gwc/service/wmts?service=WMTS&request=GetTile&version=1.0.0&layer=capabaseargenmap&style=default&tilematrixset=EPSG:3857&format=image/png&tilematrix=EPSG:3857:{0}&tilerow={1}&tilecol={2}",
                zoom, pos.Y, pos.X);

            return GetTileImageUsingHttp(url);
        }
    }
}
