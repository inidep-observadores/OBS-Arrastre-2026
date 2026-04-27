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

    public MainWindow(MainWindowViewModel viewModel, IUserSettingsService settingsService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsService = settingsService;

        StateChanged += MainWindow_StateChanged;


        // Suscribirse a actualizaciones de datos
        viewModel.MapUpdateRequested += ViewModel_MapUpdateRequested;
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
        var vm = (MainWindowViewModel)DataContext;

        if (vm == null) return;

        // 1. Dibujar Track de la marea (Naranja)
        if (vm.CurrentTrack.Any())
        {
            var points = vm.CurrentTrack.Select(p => new PointLatLng(p.Latitud, p.Longitud)).ToList();
            var route = new GMapRoute(points)
            {
                Shape = new Path
                {
                    Stroke = Brushes.OrangeRed,
                    StrokeThickness = 2,
                    ToolTip = "Track de la marea"
                }
            };
            MainMap.Markers.Add(route);
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
                    
                    // Línea del lance (Cian)
                    var lanceRoute = new GMapRoute(new List<PointLatLng> { startPos, endPos })
                    {
                        Shape = new Path
                        {
                            Stroke = Brushes.Cyan,
                            StrokeThickness = 1.5,
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

        // 3. Ajustar vista si hay datos
        if (MainMap.Markers.Any())
        {
            // Pequeño delay para asegurar que el control esté listo para centrar
            Dispatcher.BeginInvoke(new Action(() => MainMap.ZoomAndCenterMarkers(null)), System.Windows.Threading.DispatcherPriority.Background);
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

    private string FormatCoord(double? val, bool isLat)
    {
        if (!val.HasValue) return "-";
        double abs = Math.Abs(val.Value);
        int deg = (int)abs;
        double min = (abs - deg) * 60;
        string q = isLat ? (val >= 0 ? "N" : "S") : (val >= 0 ? "E" : "O");
        return $"{deg}º {min:00.1}' {q}".Replace('.', ',');
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState != WindowState.Minimized)
        {
            _settingsService.UpdateSettings(s => s.WindowState = WindowState);
        }
    }
}
