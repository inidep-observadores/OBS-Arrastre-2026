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
using ControlMareas.App.ViewModels;
using ControlMareas.App.Services;
using ControlMareas.App.Models;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Infrastructure.MapProviders;
using System.Net;
using System.Windows.Input;

namespace ControlMareas.App;

public partial class MainWindow : Window
{
    private readonly IUserSettingsService _settingsService;
    private readonly GeoJsonService _geoJsonService;
    private readonly List<GMapMarker> _staticGeoJsonMarkers = new();
    private GMapMarker? _vesselMarker;

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
 
        // Filtrar track según ventana temporal si aplica
        var trackToDraw = vm.CurrentTrack;
        if (vm.TrackVisibilityWindowDays > 0 && vm.CurrentPlaybackPoint != null)
        {
            var centerDate = vm.CurrentPlaybackPoint.FechaHora;
            var window = TimeSpan.FromDays(vm.TrackVisibilityWindowDays);
            trackToDraw = vm.CurrentTrack.Where(p => 
                p.FechaHora >= centerDate.Subtract(window) && 
                p.FechaHora <= centerDate.Add(window)).ToList();
        }

        // 1. Dibujar Track de la marea (Violeta)
        if (vm.ShowTrackLine && trackToDraw.Any())
        {
            var points = trackToDraw.Select(p => new PointLatLng(p.Latitud, p.Longitud)).ToList();
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
        }

        // 2. Dibujar Puntos de Track
        if (vm.ShowTrackPoints && trackToDraw.Any())
        {
            for (int i = 0; i < trackToDraw.Count; i++)
            {
                var track = trackToDraw[i];
                var originalIndex = vm.CurrentTrack.IndexOf(track);
                var pointPos = new PointLatLng(track.Latitud, track.Longitud);
                var pointMarker = new GMapMarker(pointPos)
                {
                    Shape = new Ellipse
                    {
                        Width = 4,
                        Height = 4,
                        Fill = Brushes.DarkViolet,
                        ToolTip = CreateTrackingToolTip(track),
                        Cursor = Cursors.Hand
                    },
                    Offset = new Point(-2, -2)
                };

                pointMarker.Shape.MouseLeftButtonDown += (s, e) =>
                {
                    vm.CurrentTrackPointIndex = originalIndex;
                    e.Handled = true;
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
                        Width = 12,
                        Height = 12,
                        Fill = Brushes.Green,
                        Stroke = Brushes.White,
                        StrokeThickness = 1.5,
                        ToolTip = CreateLanceToolTip(lance, "INICIO", vm),
                        Cursor = Cursors.Hand
                    },
                    Offset = new Point(-6, -6)
                };
                startMarker.Shape.MouseLeftButtonDown += (s, e) =>
                {
                    vm.SeekTrackToTime(vm.GetLanceDateTime(lance, true));
                    e.Handled = true;
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
                            ToolTip = CreateLanceRouteToolTip(lance)
                        }
                    };
                    MainMap.Markers.Add(lanceRoute);

                    // Marcador Fin (Rojo)
                    var endMarker = new GMapMarker(endPos)
                    {
                        Shape = new Ellipse
                        {
                            Width = 12,
                            Height = 12,
                            Fill = Brushes.Red,
                            Stroke = Brushes.White,
                            StrokeThickness = 1.5,
                            ToolTip = CreateLanceToolTip(lance, "FIN", vm),
                            Cursor = Cursors.Hand
                        },
                        Offset = new Point(-6, -6)
                    };
                    endMarker.Shape.MouseLeftButtonDown += (s, e) =>
                    {
                        vm.SeekTrackToTime(vm.GetLanceDateTime(lance, false));
                        e.Handled = true;
                    };
                    MainMap.Markers.Add(endMarker);
                }
            }
        }

        // 4. Dibujar Marcador de Buque (si hay un punto seleccionado en el track)
        if (vm.CurrentTrackPointIndex >= 0 && vm.CurrentTrackPointIndex < vm.CurrentTrack.Count)
        {
            var point = vm.CurrentTrack[vm.CurrentTrackPointIndex];
            var pos = new PointLatLng(point.Latitud, point.Longitud);
            
            if (_vesselMarker == null)
            {
                _vesselMarker = new GMapMarker(pos)
                {
                    Shape = CreateVesselShape(),
                    Offset = new Point(-6, -15) // Centrado para 12x30
                };
            }
            
            _vesselMarker.Position = pos;
            
            // Aplicar rotación según el rumbo (0º es Norte, el barco ya apunta al Norte)
            if (_vesselMarker.Shape is FrameworkElement shape)
            {
                shape.RenderTransform = new RotateTransform(point.Rumbo, 6, 15);
                shape.ToolTip = CreateTrackingToolTip(point);
            }
            
            MainMap.Markers.Add(_vesselMarker);
        }
        else
        {
            _vesselMarker = null; 
        }
 
        // 3. Ajustar vista si se solicitó un zoom automático (ej: al cargar marea)
        if (vm.ShouldZoomOnNextUpdate)
        {
            vm.ShouldZoomOnNextUpdate = false;
            var dynamicMarkers = MainMap.Markers.Where(m => !_staticGeoJsonMarkers.Contains(m)).ToList();
            if (dynamicMarkers.Any())
            {
                Dispatcher.BeginInvoke(new Action(() => 
                {
                    var points = dynamicMarkers.Select(m => m.Position).ToList();
                    double minLat = points.Min(p => p.Lat);
                    double maxLat = points.Max(p => p.Lat);
                    double minLng = points.Min(p => p.Lng);
                    double maxLng = points.Max(p => p.Lng);
 
                    // Asegurar que el rectángulo tenga un tamaño mínimo
                    double width = Math.Max(maxLng - minLng, 0.05);
                    double height = Math.Max(maxLat - minLat, 0.05);
 
                    var rect = new RectLatLng(maxLat, minLng, width, height);
                    MainMap.SetZoomToFitRect(rect);
                    
                    if (MainMap.Zoom > 2) MainMap.Zoom--;
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
    }

    private object CreateLanceToolTip(Lance lance, string type, MainWindowViewModel vm)
    {
        var accentBrush = type == "INICIO" ? Brushes.Green : Brushes.Red;
        var textBrush = (Brush)Application.Current.FindResource("PrimaryTextBrush");
        var bgBrush = (Brush)Application.Current.FindResource("SurfaceBackgroundBrush");

        var tip = new ToolTip
        {
            Background = bgBrush,
            Foreground = textBrush,
            BorderBrush = accentBrush,
            BorderThickness = new Thickness(2),
            Padding = new Thickness(10),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold
        };

        // Formatear fecha de yyyy-MM-dd a dd/MM/yyyy si es posible
        string fecha = lance.Fecha;
        if (DateTime.TryParse(lance.Fecha, out DateTime dt))
        {
            fecha = dt.ToString("dd/MM/yyyy");
        }

        string hora = type == "INICIO" ? lance.HoraInicio : lance.HoraFinal;
        
        string lat = type == "INICIO" ? FormatCoord(lance.LatitudInicioDecimal, true) : FormatCoord(lance.LatitudFinalDecimal, true);
        string lon = type == "INICIO" ? FormatCoord(lance.LongitudInicioDecimal, false) : FormatCoord(lance.LongitudFinalDecimal, false);

        tip.Content = new StackPanel();
        var header = new TextBlock 
        { 
            Text = $"LANCE NRO: {lance.NroLance} ({type})", 
            Foreground = accentBrush,
            Margin = new Thickness(0, 0, 0, 8),
            FontWeight = FontWeights.Bold
        };
        var body = new TextBlock 
        { 
            Text = $"FECHA: {fecha}\n" +
                   $"HORA: {hora}\n" +
                   $"POS: {lat}, {lon}",
            Foreground = textBrush
        };

        ((StackPanel)tip.Content).Children.Add(header);
        ((StackPanel)tip.Content).Children.Add(body);

        return tip;
    }

    private object CreateTrackingToolTip(MareaTracking point)
    {
        var textBrush = (Brush)Application.Current.FindResource("PrimaryTextBrush");
        var bgBrush = (Brush)Application.Current.FindResource("SurfaceBackgroundBrush");

        var tip = new ToolTip
        {
            Background = bgBrush,
            Foreground = textBrush,
            BorderBrush = Brushes.DarkViolet,
            BorderThickness = new Thickness(2),
            Padding = new Thickness(10),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold
        };

        string lat = FormatCoord(point.Latitud, true);
        string lon = FormatCoord(point.Longitud, false);

        tip.Content = new StackPanel();
        var header = new TextBlock 
        { 
            Text = "PUNTO DE TRACK", 
            Foreground = Brushes.DarkViolet,
            Margin = new Thickness(0, 0, 0, 8),
            FontWeight = FontWeights.Bold
        };
        var body = new TextBlock 
        { 
            Text = $"FECHA: {point.FechaHora:dd/MM/yyyy}\n" +
                   $"HORA: {point.FechaHora:HH:mm}\n" +
                   $"POS: {lat}, {lon}\n" +
                   $"RUMBO: {point.Rumbo:0}º\n" +
                   $"VELOCIDAD: {point.Velocidad:0.0} nudos",
            Foreground = textBrush
        };

        ((StackPanel)tip.Content).Children.Add(header);
        ((StackPanel)tip.Content).Children.Add(body);

        return tip;
    }

    private object CreateLanceRouteToolTip(Lance lance)
    {
        var bgBrush = (Brush)Application.Current.FindResource("SurfaceBackgroundBrush");
        var textBrush = (Brush)Application.Current.FindResource("PrimaryTextBrush");

        var tip = new ToolTip
        {
            Background = bgBrush,
            Foreground = textBrush,
            BorderBrush = Brushes.Cyan,
            BorderThickness = new Thickness(2),
            Padding = new Thickness(10),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold
        };

        var stack = new StackPanel();
        
        stack.Children.Add(new TextBlock 
        { 
            Text = $"LANCE NRO: {lance.NroLance}", 
            Foreground = Brushes.DarkCyan,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Formatear fecha si es posible
        string fecha = lance.Fecha;
        if (DateTime.TryParse(lance.Fecha, out DateTime dt))
        {
            fecha = dt.ToString("dd/MM/yyyy");
        }

        stack.Children.Add(new TextBlock { Text = $"FECHA: {fecha}", Foreground = textBrush, FontSize = 12 });
        stack.Children.Add(new TextBlock { Text = $"INICIO: {lance.HoraInicio}", Foreground = textBrush, FontSize = 12 });
        stack.Children.Add(new TextBlock { Text = $"FIN: {lance.HoraFinal}", Foreground = textBrush, FontSize = 12 });
        stack.Children.Add(new TextBlock { Text = $"CAPTURA: {lance.CapturaTotalKg:N0} kg", Foreground = textBrush, FontSize = 12 });

        tip.Content = stack;
        return tip;
    }

    private string FormatCoord(double? val, bool isLat)
    {
        if (!val.HasValue) return "-";
        double abs = Math.Abs(val.Value);
        int deg = (int)abs;
        double min = (abs - deg) * 60;
        string q = isLat ? (val >= 0 ? "N" : "S") : (val >= 0 ? "E" : "O");
        return $"{deg}º {min:00.0}' {q}".Replace('.', ',');
    }

    private UIElement CreateVesselShape()
    {
        var canvas = new Canvas { Width = 12, Height = 30 };
        
        // Cuerpo del buque (Rectángulo) - Parte inferior
        var body = new Rectangle
        {
            Width = 12,
            Height = 20,
            Fill = Brushes.DeepSkyBlue,
            Stroke = Brushes.MidnightBlue,
            StrokeThickness = 1
        };
        Canvas.SetLeft(body, 0);
        Canvas.SetTop(body, 10);
        
        // Proa (Triángulo) - Parte superior, apunta al Norte (0º)
        var proa = new Polygon
        {
            Points = new PointCollection { new Point(0, 10), new Point(6, 0), new Point(12, 10) },
            Fill = Brushes.DeepSkyBlue,
            Stroke = Brushes.MidnightBlue,
            StrokeThickness = 1
        };
        
        canvas.Children.Add(body);
        canvas.Children.Add(proa);
        
        // Efecto premium
        canvas.Effect = new System.Windows.Media.Effects.DropShadowEffect 
        { 
            BlurRadius = 5, 
            ShadowDepth = 2, 
            Opacity = 0.5 
        };
        
        return canvas;
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

            // Asegurar que el rectángulo tenga un tamaño mínimo para evitar fallos de SetZoomToFitRect
            double width = Math.Max(maxLng - minLng, 0.05);
            double height = Math.Max(maxLat - minLat, 0.05);

            var rect = new RectLatLng(maxLat, minLng, width, height);
            
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

    private void RecordsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem != null)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                listBox.ScrollIntoView(listBox.SelectedItem);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private void ControlLance_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListViewItem item && item.DataContext is ControlLanceDetailViewModel detailVm)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.EditLanceFromDetailCommand.Execute(detailVm);
            }
        }
    }

    private void MainMap_MouseMove(object sender, MouseEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var point = e.GetPosition(MainMap);
            var latLng = MainMap.FromLocalToLatLng((int)point.X, (int)point.Y);
            vm.MouseLatitude = latLng.Lat;
            vm.MouseLongitude = latLng.Lng;
        }
    }

    private void MainMap_MouseLeave(object sender, MouseEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.MouseLatitude = null;
            vm.MouseLongitude = null;
        }
    }
}
