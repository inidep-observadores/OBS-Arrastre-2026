using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using GMap.NET;
using GMap.NET.WindowsPresentation;

namespace ControlMareas.App.Services
{
    /// <summary>
    /// Servicio para cargar y parsear capas GeoJSON y convertirlas en marcadores de GMap.NET WPF.
    /// </summary>
    public class GeoJsonService
    {
        public List<GMapMarker> LoadGeoJsonMarkers()
        {
            var allMarkers = new List<GMapMarker>();
            string geoJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "geojson");

            if (!Directory.Exists(geoJsonPath)) return allMarkers;

            var files = Directory.GetFiles(geoJsonPath, "*.geojson");

            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                
                try
                {
                    string json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("features", out var features))
                    {
                        foreach (var feature in features.EnumerateArray())
                        {
                            ProcessFeature(feature, allMarkers, fileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error cargando GeoJSON {fileName}: {ex.Message}");
                }
            }

            return allMarkers;
        }

        private void ProcessFeature(JsonElement feature, List<GMapMarker> markers, string layerName)
        {
            if (!feature.TryGetProperty("geometry", out var geometry)) return;
            if (!geometry.TryGetProperty("type", out var type)) return;
            if (!geometry.TryGetProperty("coordinates", out var coordinates)) return;

            string geometryType = type.GetString() ?? "";
            Color color = GetColorForLayer(layerName);
            double thickness = GetThicknessForLayer(layerName);

            switch (geometryType)
            {
                case "LineString":
                    markers.Add(CreateRoute(coordinates, layerName, color, thickness));
                    break;
                case "Polygon":
                    markers.Add(CreatePolygon(coordinates[0], layerName, color, thickness));
                    break;
                case "MultiPolygon":
                    foreach (var polyCoords in coordinates.EnumerateArray())
                    {
                        markers.Add(CreatePolygon(polyCoords[0], layerName, color, thickness));
                    }
                    break;
                case "MultiLineString":
                    foreach (var lineCoords in coordinates.EnumerateArray())
                    {
                        markers.Add(CreateRoute(lineCoords, layerName, color, thickness));
                    }
                    break;
            }
        }

        private GMapRoute CreateRoute(JsonElement coordinates, string name, Color color, double thickness)
        {
            var points = new List<PointLatLng>();
            foreach (var coord in coordinates.EnumerateArray())
            {
                double lng = coord[0].GetDouble();
                double lat = coord[1].GetDouble();
                points.Add(new PointLatLng(lat, lng));
            }

            var route = new GMapRoute(points);
            route.Shape = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                ToolTip = name
            };
            return route;
        }

        private GMapPolygon CreatePolygon(JsonElement coordinates, string name, Color color, double thickness)
        {
            var points = new List<PointLatLng>();
            foreach (var coord in coordinates.EnumerateArray())
            {
                double lng = coord[0].GetDouble();
                double lat = coord[1].GetDouble();
                points.Add(new PointLatLng(lat, lng));
            }

            var polyColor = Color.FromArgb(40, color.R, color.G, color.B); // Semitransparente

            var polygon = new GMapPolygon(points);
            polygon.Shape = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush(color),
                Fill = new SolidColorBrush(polyColor),
                StrokeThickness = thickness,
                ToolTip = name
            };
            return polygon;
        }

        private Color GetColorForLayer(string name)
        {
            name = name.ToLower();
            if (name.Contains("zee") || name.Contains("economica")) return Colors.RoyalBlue;
            if (name.Contains("territorial")) return Colors.SkyBlue;
            if (name.Contains("veda")) return Colors.Red;
            if (name.Contains("zcp")) return Colors.ForestGreen;
            if (name.Contains("isobata") || name.Contains("mt")) return Color.FromArgb(0x60, 0x80, 0x80, 0x80); // Gris claro transparente
            return Colors.Gray;
        }

        private double GetThicknessForLayer(string name)
        {
            name = name.ToLower();
            if (name.Contains("isobata") || name.Contains("mt")) return 0.5;
            if (name.Contains("zee") || name.Contains("veda")) return 2.0;
            return 1.0;
        }
    }
}
