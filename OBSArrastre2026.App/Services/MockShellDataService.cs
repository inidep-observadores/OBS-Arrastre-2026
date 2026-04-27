using OBSArrastre2026.App.Models;
using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.App.Services;

public interface IMockShellDataService
{
    IReadOnlyList<NavigationItemViewModel> GetNavigationItems();

    DashboardContent GetDashboard();

    ListSectionContent GetListSection(NavigationSection section);
}

public sealed class MockShellDataService : IMockShellDataService
{
    public IReadOnlyList<NavigationItemViewModel> GetNavigationItems() =>
    [
        new(NavigationSection.Inicio, "Inicio", "Panel de control general", "◌"),
        new(NavigationSection.Mareas, "Mareas", "Cabeceras y estado operativo", "◇"),
        new(NavigationSection.Lances, "Lances", "Registro y control de capturas", "△", true),
        new(NavigationSection.Muestras, "Muestras", "Muestreo biologico y control", "▣", true),
        new(NavigationSection.Submuestras, "Submuestras", "Detalle por individuo", "▤", true),
        new(NavigationSection.Produccion, "Produccion", "Registros diarios de proceso", "◫", true)
    ];

    public DashboardContent GetDashboard() =>
        new(
            "Vista general",
            "Inicio",
            "Maqueta premium para visualizar la operacion de arrastre con foco en navegacion, jerarquia visual y lectura rapida.",
            "Crear acceso rapido",
            [
                new("Mareas activas", "18", "+3 esta semana", "Cabeceras abiertas en seguimiento"),
                new("Lances cargados", "246", "+12 hoy", "Actividad consolidada por etapa"),
                new("Muestras listas", "89", "74% revisadas", "Pendientes de integracion biologica"),
                new("Produccion diaria", "42.8 t", "+6.4%", "Simulacion de cierre operativo")
            ],
            [
                new("Operacion", "Turno de carga sugerido", "La maqueta prioriza acciones frecuentes y lectura lateral continua para operadores.", "Sidebar persistente + area principal adaptable"),
                new("Muestras", "Revision biologica", "La seccion de muestras y submuestras comparte un lenguaje visual de detalle para evitar saltos cognitivos.", "Tablas mock con badges y filtros"),
                new("Tema", "Claro, oscuro y sistema", "El layout usa recursos dinamicos para preparar el soporte de tema real sin rehacer las vistas.", "Theme service desacoplado")
            ],
            [
                new("Lun", 42, "Carga inicial"),
                new("Mar", 58, "Mayor actividad"),
                new("Mie", 76, "Pico de registros"),
                new("Jue", 63, "Revision y control"),
                new("Vie", 88, "Cierre operativo")
            ]);

    public ListSectionContent GetListSection(NavigationSection section) => section switch
    {
        NavigationSection.Mareas => new(
            "Cabecera maestra",
            "Mareas",
            "Lista mock de mareas basada en el esquema SQL. La prioridad es probar densidad, filtros y lectura rapida.",
            "Nueva marea",
            "Codigo",
            "Buque",
            "Inicio",
            "Fin",
            "Comentario",
            ["Activas", "Con buque asignado", "Ultimos 30 dias"],
            [
                new("MAR-2026-014", "Mar Azul", "05 Abr 2026", "18 Abr 2026", "Control de langostino", "En curso"),
                new("MAR-2026-013", "Nuevo Horizonte", "28 Mar 2026", "09 Abr 2026", "Cierre en revision", "Revision"),
                new("MAR-2026-012", "Estrella del Sur", "11 Mar 2026", "23 Mar 2026", "Sin observaciones", "Cerrada"),
                new("MAR-2026-011", "Patagon V", "02 Mar 2026", "14 Mar 2026", "Cambio de capitan", "Archivada")
            ]),
        NavigationSection.Lances => new(
            "Actividad por etapa",
            "Lances",
            "Maqueta de la tabla maestra lances, pensada para lectura operacional y futura integracion de filtros geograficos.",
            "Nuevo lance",
            "Nro",
            "Fecha",
            "Latitud",
            "Longitud",
            "Captura Total",
            ["Marea activa", "Con coordenadas", "Ultimas 72 hs"],
            [
                new("084", "09 Abr 2026 06:20", "44.12 / -62.91", "95-110 m", "Mar 3 / Viento 40°", "Validado"),
                new("083", "09 Abr 2026 03:10", "44.07 / -62.84", "88-102 m", "Mar 2 / Viento 28°", "Borrador"),
                new("082", "08 Abr 2026 22:45", "43.98 / -62.76", "105-118 m", "Mar 4 / Viento 67°", "Validado"),
                new("081", "08 Abr 2026 18:05", "43.90 / -62.70", "91-97 m", "Mar 2 / Viento 10°", "Observado")
            ]),
        NavigationSection.Muestras => new(
            "Control biologico",
            "Muestras",
            "Vista mock de muestras, con foco en especie, origen y estado de revision de cada toma.",
            "Nueva muestra",
            "ID",
            "Lance",
            "Especie",
            "Clase",
            "Observador",
            ["Con especie", "Revision pendiente", "Ultima campana"],
            [
                new("M-2401", "Lance 084", "Merluza hubbsi", "Biologica", "A. Peralta", "Lista"),
                new("M-2398", "Lance 083", "Langostino", "Comercial", "R. Diaz", "Revision"),
                new("M-2392", "Lance 081", "Abadejo", "Biologica", "A. Peralta", "Borrador"),
                new("M-2386", "Lance 078", "Polaca", "Control", "L. Sosa", "Lista")
            ]),
        NavigationSection.Submuestras => new(
            "Detalle por individuo",
            "Submuestras",
            "Representacion mock de items_submuestras, preparada para alto detalle sin perder legibilidad.",
            "Nueva submuestra",
            "ID",
            "Muestra",
            "Sexo",
            "Estadio",
            "Medida clave",
            [],
            [
                new("SM-981", "M-2401", "Hembra", "III", "32.4 cm / 410 g", "Completa"),
                new("SM-978", "M-2401", "Macho", "II", "30.8 cm / 380 g", "Completa"),
                new("SM-962", "M-2398", "Indeterminado", "I", "21.3 cm / 145 g", "Pendiente"),
                new("SM-955", "M-2386", "Hembra", "IV", "34.1 cm / 432 g", "Auditada")
            ]),
        NavigationSection.Produccion => new(
            "Parte operativo",
            "Produccion",
            "Mock de registros_produccion, optimizado para carga diaria y contraste rapido entre producto y kilos.",
            "Nuevo registro",
            "Fecha",
            "Marea",
            "Producto",
            "Categoria",
            "Kg",
            ["Hoy", "Con comentarios", "Orden por producto"],
            [
                new("09 Abr 2026", "MAR-2026-014", "Cola de langostino", "A", "8,250", "Cerrado"),
                new("09 Abr 2026", "MAR-2026-014", "Entero congelado", "B", "12,900", "Cerrado"),
                new("08 Abr 2026", "MAR-2026-013", "Filet merluza", "Premium", "6,420", "Revision"),
                new("08 Abr 2026", "MAR-2026-013", "Harina", "Subproducto", "15,180", "Borrador")
            ]),
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, null)
    };
}
