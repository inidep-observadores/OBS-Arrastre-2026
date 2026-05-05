using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SkiaSharp;

namespace OBSArrastre2026.App.Services
{
    public class MapRenderingService : IMapRenderingService
    {
        private const int Dpi = 220;
        // Se eliminan Width y Height constantes para usar dimensionamiento dinámico

        // Colores base
        private static readonly SKColor ColorOcean = SKColors.White;
        private static readonly SKColor ColorLand = new SKColor(0x99, 0xCC, 0xCC); // #99CCCC
        private static readonly SKColor ColorCoastline = new SKColor(0x0F, 0x17, 0x2A); // #0F172A
        private static readonly SKColor ColorGrid = new SKColor(0xB6, 0xB6, 0xB6, 0x59); // #b6b6b6 con alpha 0.35
        private static readonly SKColor ColorAxis = new SKColor(0x0A, 0x0A, 0x0A);
        private static readonly SKColor ColorPoints = new SKColor(0x11, 0x11, 0x11);
        private static readonly SKColor ColorPortCircle = new SKColor(0xD3, 0x2F, 0x2F); // #d32f2f

        public async Task RenderMapAsync(IEnumerable<double> latitudes, IEnumerable<double> longitudes, string outputPath)
        {
            var lats = latitudes.ToList();
            var lons = longitudes.ToList();

            if (!lats.Any() || !lons.Any()) return;

            var (lonMin, lonMax, latMin, latMax) = ComputeEnvelope(lats, lons);

            // Calcular dimensiones dinámicas
            CalculateLayout(lonMin, lonMax, latMin, latMax, out int width, out int height, out var plotRect, out float scale, out double cosLat);

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(ColorOcean);

            // Proyector simplificado basado en el layout calculado
            Func<double, double, SKPoint> project = (lon, lat) =>
            {
                float x = (float)(plotRect.Left + (lon - lonMin) * cosLat * scale);
                float y = (float)(plotRect.Top + (latMax - lat) * scale);
                return new SKPoint(x, y);
            };

            canvas.Save();
            canvas.ClipRect(plotRect);
            DrawBackgroundGrid(canvas, lonMin, lonMax, latMin, latMax, project);
            await DrawGeoJsonLayers(canvas, project, lonMin, lonMax, latMin, latMax);
            DrawPoints(canvas, lats, lons, project);
            canvas.Restore();

            DrawAxesAndScales(canvas, lonMin, lonMax, latMin, latMax, project, plotRect);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            await using var stream = File.OpenWrite(outputPath);
            data.SaveTo(stream);
        }

        public async Task<byte[]> RenderMapToBytesAsync(IEnumerable<double> latitudes, IEnumerable<double> longitudes)
        {
            var lats = latitudes.ToList();
            var lons = longitudes.ToList();

            if (!lats.Any() || !lons.Any()) return Array.Empty<byte>();

            var (lonMin, lonMax, latMin, latMax) = ComputeEnvelope(lats, lons);
            CalculateLayout(lonMin, lonMax, latMin, latMax, out int width, out int height, out var plotRect, out float scale, out double cosLat);

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(ColorOcean);

            Func<double, double, SKPoint> project = (lon, lat) =>
            {
                float x = (float)(plotRect.Left + (lon - lonMin) * cosLat * scale);
                float y = (float)(plotRect.Top + (latMax - lat) * scale);
                return new SKPoint(x, y);
            };

            canvas.Save();
            canvas.ClipRect(plotRect);
            DrawBackgroundGrid(canvas, lonMin, lonMax, latMin, latMax, project);
            await DrawGeoJsonLayers(canvas, project, lonMin, lonMax, latMin, latMax);
            DrawPoints(canvas, lats, lons, project);
            canvas.Restore();

            DrawAxesAndScales(canvas, lonMin, lonMax, latMin, latMax, project, plotRect);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        private void CalculateLayout(double lonMin, double lonMax, double latMin, double latMax, out int width, out int height, out SKRect plotRect, out float scale, out double cosLat)
        {
            double midLat = (latMin + latMax) / 2.0;
            cosLat = Math.Cos(midLat * Math.PI / 180.0);

            double geoWidth = (lonMax - lonMin) * cosLat;
            double geoHeight = (latMax - latMin);

            // Altura de referencia para el área de dibujo (plot)
            // Usamos un valor que proporcione buena resolución en el informe
            float targetPlotHeight = 800f; 
            scale = (float)(targetPlotHeight / geoHeight);

            float plotW = (float)(geoWidth * scale);
            float plotH = (float)(geoHeight * scale);

            // Márgenes fijos en píxeles para etiquetas y ejes
            float padL = 130f; // Espacio para latitudes y etiquetas LS/LW
            float padR = 60f;
            float padT = 60f;
            float padB = 130f; // Espacio para longitudes

            width = (int)Math.Ceiling(plotW + padL + padR);
            height = (int)Math.Ceiling(plotH + padT + padB);

            plotRect = new SKRect(padL, padT, padL + plotW, padT + plotH);
        }

        private (double lonMin, double lonMax, double latMin, double latMax) ComputeEnvelope(List<double> lats, List<double> lons)
        {
            double minLat = lats.Min();
            double maxLat = lats.Max();
            double minLon = lons.Min();
            double maxLon = lons.Max();

            double latSpan = Math.Max(Math.Abs(maxLat - minLat), 0.2);
            double lonSpan = Math.Max(Math.Abs(maxLon - minLon), 0.2);

            // Margen generoso alrededor de los puntos
            double latMargin = Math.Max(latSpan * 0.4, 0.4);
            double lonMargin = Math.Max(lonSpan * 0.4, 0.6);

            double west = minLon - lonMargin;
            double east = maxLon + lonMargin;
            double south = minLat - latMargin;
            double north = maxLat + latMargin;

            // Forzar límites de la costa argentina si estamos muy cerca
            if (west > -68.0 && minLon < -60) west = -68.0;

            double westAligned = Math.Floor(west);
            double eastAligned = Math.Ceiling(east);
            double southAligned = Math.Floor(south);
            double northAligned = Math.Ceiling(north);

            // Asegurar un tamaño mínimo de ventana (3 grados)
            if (eastAligned - westAligned < 3) 
            {
                eastAligned = westAligned + 3;
            }
            if (northAligned - southAligned < 3) 
            {
                northAligned = southAligned + 3;
            }

            return (westAligned, eastAligned, southAligned, northAligned);
        }

        private void DrawBackgroundGrid(SKCanvas canvas, double lonMin, double lonMax, double latMin, double latMax, Func<double, double, SKPoint> project)
        {
            using var paint = new SKPaint
            {
                Color = ColorGrid,
                StrokeWidth = 1.5f,
                Style = SKPaintStyle.Stroke
            };

            for (int lon = (int)lonMin; lon <= (int)lonMax; lon++)
            {
                var p1 = project(lon, latMin);
                var p2 = project(lon, latMax);
                canvas.DrawLine(p1, p2, paint);
            }

            for (int lat = (int)latMin; lat <= (int)latMax; lat++)
            {
                var p1 = project(lonMin, lat);
                var p2 = project(lonMax, lat);
                canvas.DrawLine(p1, p2, paint);
            }
        }

        private async Task DrawGeoJsonLayers(SKCanvas canvas, Func<double, double, SKPoint> project, double lonMin, double lonMax, double latMin, double latMax)
        {
            string geoJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "geojson");
            if (!Directory.Exists(geoJsonPath)) return;

            // Orden de dibujo aproximado: Land primero, luego isobatas, luego vedas/ZEE, luego puertos.
            var files = Directory.GetFiles(geoJsonPath, "*.geojson")
                .OrderBy(f => GetLayerZOrder(Path.GetFileNameWithoutExtension(f)))
                .ToList();

            foreach (var file in files)
            {
                string layerName = Path.GetFileNameWithoutExtension(file).ToLower();
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    using var doc = JsonDocument.Parse(json);
                    var features = doc.RootElement.GetProperty("features");

                    foreach (var feature in features.EnumerateArray())
                    {
                        DrawFeature(canvas, feature, layerName, project, lonMin, lonMax, latMin, latMax);
                    }
                }
                catch { /* Ignorar errores de carga de archivos individuales */ }
            }
        }

        private void DrawFeature(SKCanvas canvas, JsonElement feature, string layerName, Func<double, double, SKPoint> project, double lonMin, double lonMax, double latMin, double latMax)
        {
            if (!feature.TryGetProperty("geometry", out var geometry)) return;
            string type = geometry.GetProperty("type").GetString();
            var coords = geometry.GetProperty("coordinates");

            var style = GetStyleForLayer(layerName);
            using var paintFill = new SKPaint { Color = style.FillColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var paintStroke = new SKPaint { Color = style.StrokeColor, StrokeWidth = style.StrokeWidth, Style = SKPaintStyle.Stroke, IsAntialias = true };
            
            if (style.IsDashed)
            {
                paintStroke.PathEffect = SKPathEffect.CreateDash(new float[] { 10, 10 }, 0);
            }

            if (type == "Polygon" || type == "MultiPolygon")
            {
                var polygons = type == "Polygon" ? new[] { coords } : coords.EnumerateArray().ToArray();
                foreach (var poly in polygons)
                {
                    using var path = new SKPath();
                    var rings = poly.EnumerateArray().ToArray();
                    if (!rings.Any()) continue;

                    // Exterior ring
                    bool first = true;
                    foreach (var point in rings[0].EnumerateArray())
                    {
                        var p = project(point[0].GetDouble(), point[1].GetDouble());
                        if (first) { path.MoveTo(p); first = false; }
                        else path.LineTo(p);
                    }
                    path.Close();
                    
                    if (style.FillColor.Alpha > 0) canvas.DrawPath(path, paintFill);
                    canvas.DrawPath(path, paintStroke);
                }
            }
            else if (type == "LineString" || type == "MultiLineString")
            {
                var lines = type == "LineString" ? new[] { coords } : coords.EnumerateArray().ToArray();
                foreach (var line in lines)
                {
                    using var path = new SKPath();
                    bool first = true;
                    foreach (var point in line.EnumerateArray())
                    {
                        var p = project(point[0].GetDouble(), point[1].GetDouble());
                        if (first) { path.MoveTo(p); first = false; }
                        else path.LineTo(p);
                    }
                    canvas.DrawPath(path, paintStroke);
                }
            }
            else if (type == "Point" && layerName.Contains("puertos"))
            {
                double lon = coords[0].GetDouble();
                double lat = coords[1].GetDouble();
                if (lon >= lonMin && lon <= lonMax && lat >= latMin && lat <= latMax)
                {
                    string name = feature.GetProperty("properties").GetProperty("NOMBRE").GetString();
                    DrawPort(canvas, lon, lat, name, project);
                }
            }
        }

        private void DrawPort(SKCanvas canvas, double lon, double lat, string name, Func<double, double, SKPoint> project)
        {
            var p = project(lon, lat);
            float radius = 15f;
            using var paint = new SKPaint { Color = ColorPortCircle, StrokeWidth = 3f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawCircle(p, radius, paint);

            if (!string.IsNullOrEmpty(name))
            {
                using var textPaint = new SKPaint
                {
                    Color = ColorAxis,
                    TextSize = 28f,
                    IsAntialias = true,
                    FakeBoldText = true,
                    Typeface = SKTypeface.FromFamilyName("Arial")
                };
                canvas.DrawText(name, p.X + 20, p.Y + 10, textPaint);
            }
        }

        private void DrawPoints(SKCanvas canvas, List<double> lats, List<double> lons, Func<double, double, SKPoint> project)
        {
            using var paint = new SKPaint { Color = ColorPoints, Style = SKPaintStyle.Fill, IsAntialias = true };
            float radius = 12f;

            for (int i = 0; i < lats.Count; i++)
            {
                var p = project(lons[i], lats[i]);
                canvas.DrawCircle(p, radius, paint);
            }
        }

        private void DrawAxesAndScales(SKCanvas canvas, double lonMin, double lonMax, double latMin, double latMax, Func<double, double, SKPoint> project, SKRect plotRect)
        {
            using var axisPaint = new SKPaint { Color = ColorAxis, StrokeWidth = 2.5f, Style = SKPaintStyle.Stroke };
            using var tickPaint = new SKPaint { Color = ColorAxis, StrokeWidth = 2.0f, Style = SKPaintStyle.Stroke };
            using var textPaint = new SKPaint { Color = ColorAxis, TextSize = 24f, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial") };

            // Marco principal
            canvas.DrawRect(plotRect, axisPaint);

            // Ticks de longitud (abajo y arriba)
            for (double lon = Math.Floor(lonMin); lon <= Math.Ceiling(lonMax); lon += 1.0)
            {
                // Mayor
                var pBottom = project(lon, latMin);
                var pTop = project(lon, latMax);
                canvas.DrawLine(pBottom.X, pBottom.Y, pBottom.X, pBottom.Y + 20, tickPaint);
                canvas.DrawLine(pTop.X, pTop.Y, pTop.X, pTop.Y - 20, tickPaint);
                
                string label = $"º{Math.Abs((int)Math.Round(lon))}";
                canvas.DrawText(label, pBottom.X - 20, pBottom.Y + 50, textPaint);

                // Menores (cada 10 min = 1/6 grado)
                for (int m = 1; m < 6; m++)
                {
                    double mVal = lon + (m / 6.0);
                    if (mVal > lonMax) break;
                    var pm = project(mVal, latMin);
                    canvas.DrawLine(pm.X, pm.Y, pm.X, pm.Y + 10, tickPaint);
                    var pmTop = project(mVal, latMax);
                    canvas.DrawLine(pmTop.X, pmTop.Y, pmTop.X, pmTop.Y - 10, tickPaint);
                }
            }

            // Ticks de latitud (izquierda y derecha)
            for (double lat = Math.Floor(latMin); lat <= Math.Ceiling(latMax); lat += 1.0)
            {
                var pLeft = project(lonMin, lat);
                var pRight = project(lonMax, lat);
                canvas.DrawLine(pLeft.X, pLeft.Y, pLeft.X - 20, pLeft.Y, tickPaint);
                canvas.DrawLine(pRight.X, pRight.Y, pRight.X + 20, pRight.Y, tickPaint);

                string label = $"º{Math.Abs((int)Math.Round(lat))}";
                canvas.DrawText(label, pLeft.X - 60, pLeft.Y + 10, textPaint);

                // Menores
                for (int m = 1; m < 6; m++)
                {
                    double mVal = lat + (m / 6.0);
                    if (mVal > latMax) break;
                    var pm = project(lonMin, mVal);
                    canvas.DrawLine(pm.X, pm.Y, pm.X - 10, pm.Y, tickPaint);
                    var pmRight = project(lonMax, mVal);
                    canvas.DrawLine(pmRight.X, pmRight.Y, pmRight.X + 10, pmRight.Y, tickPaint);
                }
            }

            // Etiquetas LW y LS
            canvas.DrawText("LW", plotRect.Left - 50, plotRect.Bottom + 80, textPaint);
            canvas.DrawText("LS", plotRect.Left - 80, plotRect.Bottom + 40, textPaint);
        }

        private int GetLayerZOrder(string name)
        {
            name = name.ToLower();
            if (name.Contains("land")) return 0;
            if (name.Contains("mt") || name.Contains("isobata")) return 5;
            if (name.Contains("zee") || name.Contains("veda")) return 10;
            if (name.Contains("puerto")) return 20;
            return 50;
        }

        private LayerStyle GetStyleForLayer(string name)
        {
            name = name.ToLower();
            if (name.Contains("land")) return new LayerStyle(ColorLand, ColorCoastline, 1.5f);
            if (name.Contains("1000mt")) return new LayerStyle(SKColors.Transparent, SKColors.Black, 1.2f, true);
            if (name.Contains("200mt")) return new LayerStyle(SKColors.Transparent, SKColors.Black, 1.0f, true);
            if (name.Contains("100mt")) return new LayerStyle(SKColors.Transparent, SKColors.Black, 0.8f, true);
            if (name.Contains("zee-50m")) return new LayerStyle(SKColors.Transparent, new SKColor(0x15, 0x65, 0xC0), 2.5f);
            if (name.Contains("zee-200millas")) return new LayerStyle(SKColors.Transparent, new SKColor(0xD3, 0x2F, 0x2F), 2.5f);
            if (name.Contains("veda")) return new LayerStyle(new SKColor(0xB7, 0x1C, 0x1C, 0x40), new SKColor(0xB7, 0x1C, 0x1C), 2.0f);
            
            return new LayerStyle(SKColors.Transparent, SKColors.Gray, 1.0f);
        }

        private struct LayerStyle
        {
            public SKColor FillColor;
            public SKColor StrokeColor;
            public float StrokeWidth;
            public bool IsDashed;

            public LayerStyle(SKColor fill, SKColor stroke, float width, bool dashed = false)
            {
                FillColor = fill;
                StrokeColor = stroke;
                StrokeWidth = width;
                IsDashed = dashed;
            }
        }
    }
}
