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
            bool isSelected = vm.SelectedRecord is LanceListItemViewModel selectedVm && selectedVm.ID == lance.Id;

            if (lance.DetallePalangre != null)
            {
                var dp = lance.DetallePalangre;

                PointLatLng? startCalPos = null;
                PointLatLng? endCalPos = null;
                PointLatLng? startVirPos = null;
                PointLatLng? endVirPos = null;

                // --- A. Segmento de Calado (Filado) ---
                if (vm.ShowPalangreCalados)
                {
                    var latIniCal = dp.LatitudInicioFiladoDecimal ?? lance.LatitudInicioDecimal;
                    var lonIniCal = dp.LongitudInicioFiladoDecimal ?? lance.LongitudInicioDecimal;

                    if (latIniCal.HasValue && lonIniCal.HasValue)
                    {
                        startCalPos = new PointLatLng(latIniCal.Value, lonIniCal.Value);

                        // Marcador Inicio Calado (Verde atenuado)
                        var startCalMarker = new GMapMarker(startCalPos.Value)
                        {
                            Shape = new Ellipse
                            {
                                Width = 10,
                                Height = 10,
                                Fill = new SolidColorBrush(Color.FromArgb(180, 76, 175, 80)),
                                Stroke = Brushes.White,
                                StrokeThickness = 1.0,
                                ToolTip = CreateLanceToolTip(lance, "CALADO (INICIO)", vm, latIniCal, lonIniCal, dp.HoraInicioFilado, lance.Fecha),
                                Cursor = Cursors.Hand
                            },
                            Offset = new Point(-5, -5)
                        };
                        startCalMarker.Shape.MouseLeftButtonDown += (s, e) =>
                        {
                            vm.SeekTrackToTime(vm.GetPalangreDateTime(lance, PalangreFase.InicioCalado));
                            e.Handled = true;
                        };
                        MainMap.Markers.Add(startCalMarker);

                        var latFinCal = dp.LatitudFinFiladoDecimal;
                        var lonFinCal = dp.LongitudFinFiladoDecimal;

                        if (latFinCal.HasValue && lonFinCal.HasValue)
                        {
                            endCalPos = new PointLatLng(latFinCal.Value, lonFinCal.Value);

                            // Línea de Calado (Color atenuado y línea continua)
                            var caladoBrush = isSelected
                                ? new SolidColorBrush(Colors.Violet) { Opacity = 0.5 }
                                : new SolidColorBrush(Colors.CornflowerBlue) { Opacity = 0.4 };

                            var caladoRoute = new GMapRoute(new List<PointLatLng> { startCalPos.Value, endCalPos.Value })
                            {
                                Shape = new Path
                                {
                                    Stroke = caladoBrush,
                                    StrokeThickness = isSelected ? 2.5 : 1.5,
                                    ToolTip = CreateLanceRouteToolTip(lance, "CALADO")
                                }
                            };
                            MainMap.Markers.Add(caladoRoute);

                            // Marcador Fin Calado (Naranja atenuado)
                            var endCalMarker = new GMapMarker(endCalPos.Value)
                            {
                                Shape = new Ellipse
                                {
                                    Width = 9,
                                    Height = 9,
                                    Fill = new SolidColorBrush(Color.FromArgb(180, 255, 152, 0)),
                                    Stroke = Brushes.White,
                                    StrokeThickness = 1.0,
                                    ToolTip = CreateLanceToolTip(lance, "CALADO (FIN)", vm, latFinCal, lonFinCal, dp.HoraFinFilado, lance.Fecha),
                                    Cursor = Cursors.Hand
                                },
                                Offset = new Point(-4.5, -4.5)
                            };
                            endCalMarker.Shape.MouseLeftButtonDown += (s, e) =>
                            {
                                vm.SeekTrackToTime(vm.GetPalangreDateTime(lance, PalangreFase.FinCalado));
                                e.Handled = true;
                            };
                            MainMap.Markers.Add(endCalMarker);
                        }
                    }
                }

                // --- B. Segmento de Virado ---
                if (vm.ShowPalangreVirados)
                {
                    var latIniVir = dp.LatitudInicioViradoDecimal;
                    var lonIniVir = dp.LongitudInicioViradoDecimal;

                    var latFinVir = dp.LatitudFinViradoDecimal ?? lance.LatitudFinalDecimal;
                    var lonFinVir = dp.LongitudFinViradoDecimal ?? lance.LongitudFinalDecimal;

                    if (latIniVir.HasValue && lonIniVir.HasValue)
                    {
                        startVirPos = new PointLatLng(latIniVir.Value, lonIniVir.Value);

                        // Marcador Inicio Virado (Cyan / Verde pleno)
                        var startVirMarker = new GMapMarker(startVirPos.Value)
                        {
                            Shape = new Ellipse
                            {
                                Width = 12,
                                Height = 12,
                                Fill = Brushes.DarkCyan,
                                Stroke = Brushes.White,
                                StrokeThickness = 1.5,
                                ToolTip = CreateLanceToolTip(lance, "VIRADO (INICIO)", vm, latIniVir, lonIniVir, dp.HoraInicioVirado, dp.FechaVirado ?? lance.Fecha),
                                Cursor = Cursors.Hand
                            },
                            Offset = new Point(-6, -6)
                        };
                        startVirMarker.Shape.MouseLeftButtonDown += (s, e) =>
                        {
                            vm.SeekTrackToTime(vm.GetPalangreDateTime(lance, PalangreFase.InicioVirado));
                            e.Handled = true;
                        };
                        MainMap.Markers.Add(startVirMarker);
                    }

                    if (latFinVir.HasValue && lonFinVir.HasValue)
                    {
                        endVirPos = new PointLatLng(latFinVir.Value, lonFinVir.Value);

                        if (startVirPos.HasValue)
                        {
                            // Línea de Virado (Color pleno / contrastado)
                            var viradoBrush = isSelected
                                ? new SolidColorBrush(Colors.Magenta) { Opacity = 0.8 }
                                : new SolidColorBrush(Colors.SteelBlue) { Opacity = 0.85 };

                            var viradoRoute = new GMapRoute(new List<PointLatLng> { startVirPos.Value, endVirPos.Value })
                            {
                                Shape = new Path
                                {
                                    Stroke = viradoBrush,
                                    StrokeThickness = isSelected ? 3.5 : 2.0,
                                    ToolTip = CreateLanceRouteToolTip(lance, "VIRADO")
                                }
                            };
                            MainMap.Markers.Add(viradoRoute);
                        }

                        // Marcador Fin Virado (Rojo pleno)
                        var endVirMarker = new GMapMarker(endVirPos.Value)
                        {
                            Shape = new Ellipse
                            {
                                Width = 12,
                                Height = 12,
                                Fill = Brushes.Red,
                                Stroke = Brushes.White,
                                StrokeThickness = 1.5,
                                ToolTip = CreateLanceToolTip(lance, "VIRADO (FIN)", vm, latFinVir, lonFinVir, dp.HoraFinVirado, dp.FechaVirado ?? lance.Fecha),
                                Cursor = Cursors.Hand
                            },
                            Offset = new Point(-6, -6)
                        };
                        endVirMarker.Shape.MouseLeftButtonDown += (s, e) =>
                        {
                            vm.SeekTrackToTime(vm.GetPalangreDateTime(lance, PalangreFase.FinVirado));
                            e.Handled = true;
                        };
                        MainMap.Markers.Add(endVirMarker);
                    }
                }

                // --- C. Líneas de Correspondencia entre Extremos de Calado y Virado ---
                if (vm.ShowPalangreCalados && vm.ShowPalangreVirados)
                {
                    var calPoints = new List<PointLatLng>();
                    if (startCalPos.HasValue) calPoints.Add(startCalPos.Value);
                    if (endCalPos.HasValue) calPoints.Add(endCalPos.Value);

                    var virPoints = new List<PointLatLng>();
                    if (startVirPos.HasValue) virPoints.Add(startVirPos.Value);
                    if (endVirPos.HasValue) virPoints.Add(endVirPos.Value);

                    if (calPoints.Count > 0 && virPoints.Count > 0)
                    {
                        PointLatLng? closestCal = null;
                        PointLatLng? closestVir = null;
                        double minDistanceKm = double.MaxValue;

                        foreach (var cp in calPoints)
                        {
                            foreach (var vp in virPoints)
                            {
                                double dist = CalculateDistanceKm(cp, vp);
                                if (dist < minDistanceKm)
                                {
                                    minDistanceKm = dist;
                                    closestCal = cp;
                                    closestVir = vp;
                                }
                            }
                        }

                        if (closestCal.HasValue && closestVir.HasValue)
                        {
                            // Pincel azul pizarra sutil y semitransparente: visible sobre el agua pero en segundo plano respecto a los lances
                            var connBrush = isSelected
                                ? new SolidColorBrush(Colors.Violet) { Opacity = 0.8 }
                                : new SolidColorBrush(Color.FromArgb(145, 65, 95, 125));

                            // 1. Unir los extremos más próximos
                            double distNm = minDistanceKm / 1.852;
                            var connectionRoute1 = new GMapRoute(new List<PointLatLng> { closestCal.Value, closestVir.Value })
                            {
                                Shape = new Path
                                {
                                    Stroke = connBrush,
                                    StrokeThickness = isSelected ? 1.4 : 1.0,
                                    StrokeDashArray = new DoubleCollection { 2, 3 },
                                    ToolTip = $"Lance #{lance.NroLance} - Extremos más próximos ({distNm:F2} mn / {minDistanceKm:F2} km)"
                                }
                            };
                            MainMap.Markers.Add(connectionRoute1);

                            // 2. Unir los otros dos extremos restantes (si ambos segmentos tienen inicio y fin)
                            if (calPoints.Count == 2 && virPoints.Count == 2)
                            {
                                var otherCal = calPoints.First(p => Math.Abs(p.Lat - closestCal.Value.Lat) > 1e-7 || Math.Abs(p.Lng - closestCal.Value.Lng) > 1e-7);
                                var otherVir = virPoints.First(p => Math.Abs(p.Lat - closestVir.Value.Lat) > 1e-7 || Math.Abs(p.Lng - closestVir.Value.Lng) > 1e-7);

                                double otherDistKm = CalculateDistanceKm(otherCal, otherVir);
                                double otherDistNm = otherDistKm / 1.852;

                                var connectionRoute2 = new GMapRoute(new List<PointLatLng> { otherCal, otherVir })
                                {
                                    Shape = new Path
                                    {
                                        Stroke = connBrush,
                                        StrokeThickness = isSelected ? 1.4 : 1.0,
                                        StrokeDashArray = new DoubleCollection { 2, 3 },
                                        ToolTip = $"Lance #{lance.NroLance} - Extremos opuestos ({otherDistNm:F2} mn / {otherDistKm:F2} km)"
                                    }
                                };
                                MainMap.Markers.Add(connectionRoute2);
                            }
                        }
                    }
                }
            }
            else
            {
                // Arrastre tradicional
                if (lance.LatitudInicioDecimal.HasValue && lance.LongitudInicioDecimal.HasValue)
                {
                    var startPos = new PointLatLng(lance.LatitudInicioDecimal.Value, lance.LongitudInicioDecimal.Value);
                    
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

                        var lanceBrush = isSelected
                            ? new SolidColorBrush(Colors.Magenta) { Opacity = 0.5 }
                            : new SolidColorBrush(Colors.SteelBlue) { Opacity = 0.5 };
                        var lanceThickness = isSelected ? 3.0 : 1.5;

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

    private object CreateLanceToolTip(
        Lance lance, 
        string type, 
        MainWindowViewModel vm, 
        double? customLat = null, 
        double? customLon = null, 
        string? customHora = null, 
        string? customFecha = null)
    {
        Brush accentBrush;
        if (type.Contains("CALADO"))
        {
            accentBrush = type.Contains("INICIO") ? Brushes.LimeGreen : Brushes.Orange;
        }
        else
        {
            accentBrush = type.Contains("INICIO") ? Brushes.DarkCyan : Brushes.Red;
        }

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

        string fechaStr = customFecha ?? (type.Contains("INICIO") ? lance.Fecha : (lance.FechaFin?.ToString("yyyy-MM-dd") ?? lance.Fecha));
        string fecha = fechaStr;
        if (DateTime.TryParse(fechaStr, out DateTime dt))
        {
            fecha = dt.ToString("dd/MM/yyyy");
        }

        string hora = customHora ?? (type.Contains("INICIO") ? lance.HoraInicio : lance.HoraFinal);
        
        double? latVal = customLat ?? (type.Contains("INICIO") ? lance.LatitudInicioDecimal : lance.LatitudFinalDecimal);
        double? lonVal = customLon ?? (type.Contains("INICIO") ? lance.LongitudInicioDecimal : lance.LongitudFinalDecimal);

        string lat = FormatCoord(latVal, true);
        string lon = FormatCoord(lonVal, false);

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

    private object CreateLanceRouteToolTip(Lance lance, string? subTipo = null)
    {
        var bgBrush = (Brush)Application.Current.FindResource("SurfaceBackgroundBrush");
        var textBrush = (Brush)Application.Current.FindResource("PrimaryTextBrush");

        var tip = new ToolTip
        {
            Background = bgBrush,
            Foreground = textBrush,
            BorderBrush = subTipo == "CALADO" ? Brushes.LightGreen : Brushes.Cyan,
            BorderThickness = new Thickness(2),
            Padding = new Thickness(10),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold
        };

        var stack = new StackPanel();
        
        string title = subTipo != null ? $"LANCE NRO: {lance.NroLance} ({subTipo})" : $"LANCE NRO: {lance.NroLance}";
        stack.Children.Add(new TextBlock 
        { 
            Text = title, 
            Foreground = subTipo == "CALADO" ? Brushes.ForestGreen : Brushes.DarkCyan,
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
        if (subTipo == "CALADO" && lance.DetallePalangre != null)
        {
            stack.Children.Add(new TextBlock { Text = $"FILADO INICIO: {lance.DetallePalangre.HoraInicioFilado}", Foreground = textBrush, FontSize = 12 });
            stack.Children.Add(new TextBlock { Text = $"FILADO FIN: {lance.DetallePalangre.HoraFinFilado}", Foreground = textBrush, FontSize = 12 });
        }
        else if (subTipo == "VIRADO" && lance.DetallePalangre != null)
        {
            stack.Children.Add(new TextBlock { Text = $"VIRADO INICIO: {lance.DetallePalangre.HoraInicioVirado}", Foreground = textBrush, FontSize = 12 });
            stack.Children.Add(new TextBlock { Text = $"VIRADO FIN: {lance.DetallePalangre.HoraFinVirado}", Foreground = textBrush, FontSize = 12 });
        }
        else
        {
            stack.Children.Add(new TextBlock { Text = $"INICIO: {lance.HoraInicio}", Foreground = textBrush, FontSize = 12 });
            stack.Children.Add(new TextBlock { Text = $"FIN: {lance.HoraFinal}", Foreground = textBrush, FontSize = 12 });
        }
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

    private static double CalculateDistanceKm(PointLatLng p1, PointLatLng p2)
    {
        double dLat = (p2.Lat - p1.Lat) * Math.PI / 180.0;
        double dLon = (p2.Lng - p1.Lng) * Math.PI / 180.0;
        double lat1 = p1.Lat * Math.PI / 180.0;
        double lat2 = p2.Lat * Math.PI / 180.0;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return 6371.0 * c;
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

    private void RecordsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.SelectedRecord is LanceListItemViewModel lance)
        {
            // Hacer zoom al lance seleccionado, aunque sea el mismo que ya estaba seleccionado
            ZoomToLance(lance.Lance);
        }
    }

    private void ZoomToLance(Lance? lance)
    {
        if (lance == null) return;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            var positions = new List<PointLatLng>();

            if (lance.LatitudInicioDecimal.HasValue && lance.LongitudInicioDecimal.HasValue)
            {
                positions.Add(new PointLatLng(lance.LatitudInicioDecimal.Value, lance.LongitudInicioDecimal.Value));
            }

            if (lance.LatitudFinalDecimal.HasValue && lance.LongitudFinalDecimal.HasValue)
            {
                positions.Add(new PointLatLng(lance.LatitudFinalDecimal.Value, lance.LongitudFinalDecimal.Value));
            }

            if (positions.Any())
            {
                double minLat = positions.Min(p => p.Lat);
                double maxLat = positions.Max(p => p.Lat);
                double minLng = positions.Min(p => p.Lng);
                double maxLng = positions.Max(p => p.Lng);

                // Asegurar que el rectángulo tenga un tamaño mínimo (para lances muy cortos)
                double width = Math.Max(maxLng - minLng, 0.05);
                double height = Math.Max(maxLat - minLat, 0.05);

                var rect = new RectLatLng(maxLat, minLng, width, height);
                MainMap.SetZoomToFitRect(rect);
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
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
