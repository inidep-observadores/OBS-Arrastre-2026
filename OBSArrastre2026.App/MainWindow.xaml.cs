using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using OBSArrastre2026.App.ViewModels;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.App.Models;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Infrastructure.MapProviders;
using System.Net;
using System.Windows.Input;

namespace OBSArrastre2026.App;

public partial class MainWindow : Window
{
    private readonly IUserSettingsService _settingsService;
    private readonly GeoJsonService _geoJsonService;
    private readonly List<GMapMarker> _staticGeoJsonMarkers = new();

    public MainWindow(MainWindowViewModel viewModel, IUserSettingsService settingsService, GeoJsonService geoJsonService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsService = settingsService;
        _geoJsonService = geoJsonService;

        StateChanged += MainWindow_StateChanged;


        // Suscribirse a actualizaciones de datos
        viewModel.MapUpdateRequested += ViewModel_MapUpdateRequested;
        viewModel.MapFocusRequested += ViewModel_MapFocusRequested;
    }

    private void MainMap_Loaded(object sender, RoutedEventArgs e)
    {
        // El mapa debe configurarse en el Dispatcher para asegurar que
        // WPF haya terminado de calcular el layout del control antes de pedir tiles.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            // Asignar proveedor Argenmap (IGN Argentina)
            MainMap.MapProvider = ArgenmapProvider.Instance;

            // Modo de acceso: usa servidor y caché local
            MainMap.Manager.Mode = AccessMode.ServerAndCache;

            MainMap.MinZoom = 2;
            MainMap.MaxZoom = 18;
            MainMap.ShowCenter = false;
            MainMap.IgnoreMarkerOnMouseWheel = true;
            MainMap.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            MainMap.CanDragMap = true;
            MainMap.DragButton = MouseButton.Left;
            
            // Centro inicial sobre Argentina
            MainMap.Position = new PointLatLng(-42, -60);
            MainMap.Zoom = 5;

            // Cargar capas GeoJSON (ZEE, Vedas, etc.)
            var geoMarkers = _geoJsonService.LoadGeoJsonMarkers();
            _staticGeoJsonMarkers.Clear();
            _staticGeoJsonMarkers.AddRange(geoMarkers);
            
            foreach (var marker in _staticGeoJsonMarkers)
            {
                MainMap.Markers.Add(marker);
            }

            // Dibujar marcadores si hay datos ya cargados
            if (DataContext is MainWindowViewModel)
            {
                ViewModel_MapUpdateRequested();
            }
            
            MainMap.ReloadMap();
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void ViewModel_MapUpdateRequested()
    {
        if (MainMap == null) return;

        MainMap.Markers.Clear();
        
        // 0. Re-dibujar capas estáticas
        foreach (var marker in _staticGeoJsonMarkers)
        {
            MainMap.Markers.Add(marker);
        }

        var vm = (MainWindowViewModel)DataContext;

        if (vm == null) return;

        // 1. Dibujar Track de la marea (Violeta)
        if (vm.CurrentTrack.Any())
        {
            var points = vm.CurrentTrack.Select(p => new PointLatLng(p.Latitud, p.Longitud)).ToList();
            var route = new GMapRoute(points)
            {
                Shape = new Path
                {
                    Stroke = new SolidColorBrush(Colors.DarkViolet) { Opacity = 0.5 },
                    StrokeThickness = 2,
                    ToolTip = "Track de la marea"
                }
            };
            MainMap.Markers.Add(route);

            // Marcadores para cada punto del track (muy pequeños, apenas más gruesos que el track)
            foreach (var p in vm.CurrentTrack)
            {
                var pointPos = new PointLatLng(p.Latitud, p.Longitud);
                var pointMarker = new GMapMarker(pointPos)
                {
                    Shape = new Ellipse
                    {
                        Width = 4,
                        Height = 4,
                        Fill = Brushes.DarkViolet,
                        ToolTip = CreateTrackingToolTip(p)
                    },
                    Offset = new Point(-2, -2)
                };
                MainMap.Markers.Add(pointMarker);
            }
        }

        // 2. Dibujar Lances
        foreach (var lance in vm.CurrentLances)
        {
            if (lance.LatitudInicioDecimal.HasValue && lance.LongitudInicioDecimal.HasValue)
            {
                var startPos = new PointLatLng(lance.LatitudInicioDecimal.Value, lance.LongitudInicioDecimal.Value);
                
                // Marcador Inicio (Verde)
                var startMarker = new GMapMarker(startPos)
                {
                    Shape = new Ellipse
                    {
                        Width = 10,
                        Height = 10,
                        Fill = Brushes.Green,
                        ToolTip = CreateLanceToolTip(lance, "INICIO", vm)
                    },
                    Offset = new Point(-5, -5)
                };
                MainMap.Markers.Add(startMarker);

                if (lance.LatitudFinalDecimal.HasValue && lance.LongitudFinalDecimal.HasValue)
                {
                    var endPos = new PointLatLng(lance.LatitudFinalDecimal.Value, lance.LongitudFinalDecimal.Value);
                    
                    // Determinar color (Azul si está seleccionado, Cian si no)
                    bool isSelected = vm.SelectedRecord is LanceListItemViewModel selectedVm && selectedVm.ID == lance.Id;
                    var lanceBrush = isSelected ? Brushes.Blue : Brushes.Cyan;
                    var lanceThickness = isSelected ? 3.0 : 1.5;

                    // Línea del lance
                    var lanceRoute = new GMapRoute(new List<PointLatLng> { startPos, endPos })
                    {
                        Shape = new Path
                        {
                            Stroke = lanceBrush,
                            StrokeThickness = lanceThickness,
                            ToolTip = $"Lance Nro: {lance.NroLance}"
                        }
                    };
                    MainMap.Markers.Add(lanceRoute);

                    // Marcador Fin (Rojo)
                    var endMarker = new GMapMarker(endPos)
                    {
                        Shape = new Ellipse
                        {
                            Width = 10,
                            Height = 10,
                            Fill = Brushes.Red,
                            ToolTip = CreateLanceToolTip(lance, "FIN", vm)
                        },
                        Offset = new Point(-5, -5)
                    };
                    MainMap.Markers.Add(endMarker);
                }
            }
        }

        // 3. Ajustar vista si hay datos dinámicos y NO hay una selección activa
        var dynamicMarkers = MainMap.Markers.Where(m => !_staticGeoJsonMarkers.Contains(m)).ToList();
        if (dynamicMarkers.Any() && vm.SelectedRecord == null)
        {
            Dispatcher.BeginInvoke(new Action(() => 
            {
                var points = dynamicMarkers.Select(m => m.Position).ToList();
                double minLat = points.Min(p => p.Lat);
                double maxLat = points.Max(p => p.Lat);
                double minLng = points.Min(p => p.Lng);
                double maxLng = points.Max(p => p.Lng);

                // Asegurar que el rectángulo tenga un tamaño mínimo para evitar fallos de SetZoomToFitRect
                double width = Math.Max(maxLng - minLng, 0.01);
                double height = Math.Max(maxLat - minLat, 0.01);

                var rect = new RectLatLng(maxLat, minLng, width, height);
                MainMap.SetZoomToFitRect(rect);
                
                if (MainMap.Zoom > 2) MainMap.Zoom--;
            }), System.Windows.Threading.DispatcherPriority.Loaded); // Usamos Loaded para que ocurra tras el renderizado
        }
    }

    private object CreateLanceToolTip(Lance lance, string type, MainWindowViewModel vm)
    {
        var tip = new ToolTip
        {
            Background = Brushes.GhostWhite,
            Foreground = Brushes.DodgerBlue,
            BorderBrush = Brushes.DodgerBlue,
            BorderThickness = new Thickness(2),
            Padding = new Thickness(10),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold
        };

        string fecha = lance.Fecha;
        string hora = type == "INICIO" ? lance.HoraInicio : lance.HoraFinal;
        
        // Usamos el método de formateo del ViewModel para mantener consistencia
        // (Nota: Asumimos que FormatCoordinate es público ahora o accesible)
        string lat = type == "INICIO" ? FormatCoord(lance.LatitudInicioDecimal, true) : FormatCoord(lance.LatitudFinalDecimal, true);
        string lon = type == "INICIO" ? FormatCoord(lance.LongitudInicioDecimal, false) : FormatCoord(lance.LongitudFinalDecimal, false);

        tip.Content = $"LANCE NRO: {lance.NroLance} ({type})\n\n" +
                      $"FECHA: {fecha}\n" +
                      $"HORA: {hora}\n" +
                      $"POS: {lat}, {lon}";

        return tip;
    }

    private object CreateTrackingToolTip(MareaTracking point)
    {
        var tip = new ToolTip
        {
            Background = Brushes.GhostWhite,
            Foreground = Brushes.DarkViolet,
            BorderBrush = Brushes.DarkViolet,
            BorderThickness = new Thickness(2),
            Padding = new Thickness(10),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold
        };

        string lat = FormatCoord(point.Latitud, true);
        string lon = FormatCoord(point.Longitud, false);

        tip.Content = "PUNTO DE TRACK\n\n" +
                      $"FECHA: {point.FechaHora:dd/MM/yyyy}\n" +
                      $"HORA: {point.FechaHora:HH:mm}\n" +
                      $"POS: {lat}, {lon}\n" +
                      $"RUMBO: {point.Rumbo:0}º\n" +
                      $"VELOCIDAD: {point.Velocidad:0.0} nudos";

        return tip;
    }

    private string FormatCoord(double? val, bool isLat)
    {
        if (!val.HasValue) return "-";
        double abs = Math.Abs(val.Value);
        int deg = (int)abs;
        double min = (abs - deg) * 60;
        string q = isLat ? (val >= 0 ? "N" : "S") : (val >= 0 ? "E" : "O");
        return $"{deg}º {min:00.1}' {q}".Replace('.', ',');
    }

    private void ViewModel_MapFocusRequested(List<PointLatLng> points)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (MainMap == null || points == null || points.Count < 2) 
            {
                if (points?.Count == 1)
                {
                    MainMap.Position = points[0];
                    MainMap.Zoom = 12;
                }
                return;
            }

            // Calcular el rectángulo que abarca todos los puntos
            double minLat = points.Min(p => p.Lat);
            double maxLat = points.Max(p => p.Lat);
            double minLng = points.Min(p => p.Lng);
            double maxLng = points.Max(p => p.Lng);

            // GMap.NET RectLatLng(top, left, width, height)
            // top = maxLat, left = minLng
            var rect = new RectLatLng(maxLat, minLng, maxLng - minLng, maxLat - minLat);
            
            // Ajustar el zoom y la posición
            MainMap.SetZoomToFitRect(rect);

            // Bajar un nivel de zoom para dejar margen en los bordes
            if (MainMap.Zoom > 2) MainMap.Zoom--;
        }));
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState != WindowState.Minimized)
        {
            _settingsService.UpdateSettings(s => s.WindowState = WindowState);
        }
    }

    private void RecordsList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is MainWindowViewModel vm && vm.SelectedRecord != null)
            {
                vm.OpenSelectedRecordEditCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
