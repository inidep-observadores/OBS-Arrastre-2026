using ControlMareas.App.Models;
using ControlMareas.App.ViewModels;

namespace ControlMareas.App.Services;

public interface IMockShellDataService
{
    IReadOnlyList<NavigationItemViewModel> GetNavigationItems();

    ListSectionContent GetListSection(NavigationSection section);
}

public sealed class MockShellDataService : IMockShellDataService
{
    public IReadOnlyList<NavigationItemViewModel> GetNavigationItems() =>
    [
        new(NavigationSection.Mareas, "Mareas", "Cabeceras y estado operativo", "◇"),
        new(NavigationSection.Lances, "Lances", "Registro y control de capturas", "△", true),
        new(NavigationSection.Muestras, "Muestras", "Muestreo biologico y control", "▣", true),
        new(NavigationSection.Submuestras, "Submuestras", "Detalle por individuo", "▤", true),
        new(NavigationSection.Produccion, "Produccion", "Registros diarios de proceso", "◫", true),
        new(NavigationSection.ControlProduccion, "Control Capt./Prod.", "Balance de masa diario por especie", "⚖", true),
        new(NavigationSection.ReemplazoEspecie, "Reemplazar especie", "Reidentificación masiva en la marea", "⇄", true)
    ];

    public ListSectionContent GetListSection(NavigationSection section) => section switch
    {
        NavigationSection.Mareas => new(
            "Cabecera maestra",
            "Mareas",
            "Gestión integral de las mareas del buque, incluyendo períodos operativos, estados de cierre y auditoría general.",
            "Nueva marea",
            "Codigo",
            "Buque",
            "Inicio",
            "Fin",
            "Comentario",
            "",
            "",
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
            "Registro detallado de lances de pesca con control de posición geográfica, tiempos de arrastre y captura total por especie.",
            "Nuevo lance",
            "Nro",
            "Fecha",
            "Latitud",
            "Longitud",
            "Captura Total",
            "Hora Inicio",
            "Hora Fin",
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
            "Administración de muestras biológicas y comerciales para el análisis de tallas y composición por especie.",
            "Nueva muestra",
            "Etapa",
            "Lance",
            "Fecha",
            "Hora",
            "Especie",
            "Peso",
            "Tipo",
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
            "Muestras que ya cuentan con ejemplares individuales procesados.",
            "Nueva submuestra",
            "Nro. Lance",
            "Fecha",
            "Hora Virada",
            "Especie",
            "Peso",
            "",
            "",
            [],
            [
                new("21", "24/04/2026", "14:30", "Merluza común", "45.2 kg", "SM-001"),
                new("22", "24/04/2026", "18:45", "Abadejo", "12.8 kg", "SM-002")
            ]),
        NavigationSection.Produccion => new(
            "Parte operativo",
            "Produccion",
            "Seguimiento diario de la producción a bordo, discriminado por producto, categoría, kilos y factores de conversión.",
            "Nuevo registro",
            "Fecha",
            "Marea",
            "Producto",
            "Categoria",
            "Kg",
            "",
            "",
            ["Hoy", "Con comentarios", "Orden por producto"],
            [
                new("09 Abr 2026", "MAR-2026-014", "Cola de langostino", "A", "8,250", "Cerrado"),
                new("09 Abr 2026", "MAR-2026-014", "Entero congelado", "B", "12,900", "Cerrado"),
                new("08 Abr 2026", "MAR-2026-013", "Filet merluza", "Premium", "6,420", "Revision"),
                new("08 Abr 2026", "MAR-2026-013", "Harina", "Subproducto", "15,180", "Borrador")
            ]),
        NavigationSection.ControlProduccion => new(
            "Auditoría de Masa",
            "Control Capt./Prod.",
            "Comparativa entre la producción declarada (reconstruida a captura) y los lances del día.",
            "Actualizar",
            "Fecha",
            "Especie",
            "Prod. Total",
            "Capt. Recon.",
            "Capt. Total",
            "Dif. Kg",
            "Dif. %",
            ["Solo diferencias", "Ultima semana"],
            []),
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, null)
    };
}
