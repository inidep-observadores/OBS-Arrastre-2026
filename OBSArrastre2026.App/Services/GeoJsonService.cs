using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using GMap.NET;
using GMap.NET.WindowsPresentation;

namespace OBSArrastre2026.App.Services
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

            switch (geometryType)
            {
                case "LineString":
                    markers.Add(CreateRoute(coordinates, layerName, color));
                    break;
                case "Polygon":
                    markers.Add(CreatePolygon(coordinates[0], layerName, color));
                    break;
                case "MultiPolygon":
                    foreach (var polyCoords in coordinates.EnumerateArray())
                    {
                        markers.Add(CreatePolygon(polyCoords[0], layerName, color));
                    }
                    break;
                case "MultiLineString":
                    foreach (var lineCoords in coordinates.EnumerateArray())
                    {
                        markers.Add(CreateRoute(lineCoords, layerName, color));
                    }
                    break;
            }
        }

        private GMapRoute CreateRoute(JsonElement coordinates, string name, Color color)
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
                StrokeThickness = 2,
                ToolTip = name
            };
            return route;
        }

        private GMapPolygon CreatePolygon(JsonElement coordinates, string name, Color color)
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
                StrokeThickness = 1,
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
            return Colors.Gray;
        }
    }
}
