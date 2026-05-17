# Historial de Versiones

Documentación histórica del desarrollo del proyecto OBSArrastre2026.
Período documentado: 2026-04-09 a 2026-05-16 (39 días de desarrollo).
Todas las versiones basadas en Semantic Versioning 2.0.0 y Conventional Commits 1.0.0.

---

## [v1.5.0] - 2026-05-16

**Descripción**: Optimizaciones finales, soporte dinámico de etapas y mejora de flujos de importación.

### Added (Características nuevas)
- feat(reporte-pdf): ajustar anchos de columna, prefijo buque pesquero y soporte dinámico de separación por etapas
- feat(app): implementar inicio de instancia única y optimizar flujos de importación y reportes
- feat(dbf): convertir pesos de captura y descarte a kilogramos en la exportación DBF
- feat: implementar encuadre dinámico de mapas en reportes

### Fixed (Correcciones)
- fix(import): diferir la persistencia de datos y etapas hasta la confirmación final y filtrar archivos no seleccionados
- fix(submuestras): corregir carga de datos inicial en edición de submuestras
- fix(reportes): corregir omisión de datos multietapa y filtro de representatividad
- fix(etapas): corregir cálculo y visualización de número de etapa en muestras

---

## [v1.4.0] - 2026-05-15

**Descripción**: Mejoras en UI, reportes y navegación avanzada.

### Added (Características nuevas)
- feat(muestras): agregar validación de integridad, columna de tipo y edición manual
- feat(produccion): integrar columna de saldo acumulado y restaurar diferencia kg con resaltado de negativos
- feat(reportes): agregar reporte PDF de detalle por especie y cálculo de porcentaje en totales
- feat: centralizar la numeración de etapas de marea y mejorar selector en producción
- feat: implementar confirmación de guardado en lances y refresco de mapa
- feat(ui): añadir opción de agrupación por etapas en control de producción
- feat(ui): implementar máscara de entrada para coordenadas geográficas en CoordinatePickerPremium
- feat(ui): mejorar edición de submuestras y persistencia de eliminación
- feat: agregar navegación Anterior/Siguiente en edición de lances
- feat: unificar barra de acciones en formularios de edición con FormActionBar
- feat: implementar persistencia de navegación y scroll automático en listas
- feat(import): mejorar detección de codificación DBF y resolución de especies con acentos
- feat(ui): implementar refresco automático del control de producción tras editar lance
- feat(map): estandarizar manejo de zona horaria en tracking
- feat(ui): implementar pantalla de inicio (splash screen) con soporte de temas

### Fixed (Correcciones)
- fix(import): búsqueda flexible de buque y mejora en consistencia de nombres
- fix(produccion): habilitar validación para creación dinámica de productos
- fix(ui): corregir validación de factor de producción y ajustar layout de submuestras
- fix: evitar autoselección de texto en buscador de especies
- fix(import): asegurar limpieza de archivos ZIP y corregir error de compilación
- fix(assets): regenerar icono de la aplicación desde png

### Changed (Cambios y mejoras)
- refactor(audit): mejorar mensajes de error en validación de muestras y submuestras
- style: alinear columnas de datos y encabezados en listas de lances, muestras y submuestras
- feat(produccion): permitir creación dinámica de productos y mejorar selección de submuestras
- ui(control-produccion): optimizar alineación, visibilidad y anchos de columna en detalle por especie
- ui: optimizar anchos de columnas en listas de producción y submuestras
- ui: ajustar anchos de columna en control de producción
- ui: aumentar el ancho de la columna de especie en las listas
- ui: aumentar ancho de columna especie en lista de producción

---

## [v1.3.0] - 2026-05-12

**Descripción**: Instalador NSIS, mejoras de distribución y cobertura de pruebas.

### Added (Características nuevas)
- feat(ui): implementar pantalla de inicio (splash screen) con soporte de temas
- feat: implementar selección de especies con nombres científicos y normalización de áreas estadísticas con cuadrantes decimales
- feat: soporte para importación de ZIP y mejoras en informes
- feat(informes): mejora visual de tablas y diferenciación de áreas en resumen narrativo
- feat(informes): sincronizar cálculos de frecuencias y gráficos de word con excel
- feat(reportes): agrupar áreas de trabajo por parte entera en informes
- test: agregar cobertura de pruebas unitarias con 111 tests nuevos

### Fixed (Correcciones)
- fix: corregir ruta de catálogos DBF y migrar carpetas de trabajo a LocalAppData para compatibilidad con instalador
- fix: revertir a publicación autocontenida estándar para mejorar estabilidad del instalador
- fix(reportes): omitir serie de indeterminados en gráficos de frecuencia de tallas
- fix(reportes): corregir base de cálculo de porcentajes en frecuencias de talla
- fix: corregir NullReferenceException en importación y estabilizar ProcesosViewModel
- fix(datos): corregir código inidep para merluza negra en el inicializador

### Changed (Cambios y mejoras)
- build(setup): renombrar el ejecutable del instalador
- style: mejorar estética del reporte Recibí del Proyecto con tabla de 3 columnas y nombres científicos
- build: eliminar script temporal de instalador

---

## [v1.2.0] - 2026-05-08

**Descripción**: Exportación DBF round-trip y homologación total con formato institucional.

### Added (Características nuevas)
- feat(report): integración de metadatos extendidos y refinamiento del informe técnico
- feat(import): captura de metadatos extendidos del buque y pesquería
- feat(report): integración de plantilla oficial y corrección de nomenclatura
- feat(informes): implementar generación de informes basada en plantilla y nomenclatura oficial
- feat(config): implementar sección de configuración con autoguardado y navegación simplificada
- feat(import): reactivar validaciones y correcciones automáticas en la importación de mareas
- feat: lograr integridad de datos 1:1 en exportación DBF y corregir errores de desbordamiento en tallas
- feat: lograr paridad bit a bit en exportación DBF y mejorar captura de datos en importación
- feat(export): homologación total de estructuras DBF con formato institucional INIDEP
- feat(export): optimización masiva de exportación DBF y telemetría de tiempos
- feat(export): implementación de exportación a DBF (en progreso)

### Fixed (Correcciones)
- fix(informes): corregir errores de sintaxis y acceso a pies de página en generación de Word
- fix: añadir migración para los nuevos campos de integridad 1:1

### Changed (Cambios y mejoras)
- chore(data): desactivar validaciones y correcciones para prueba de integridad round-trip

---

## [v1.1.0] - 2026-05-11

**Descripción**: Suite completa de pruebas unitarias y validación integral.

### Added (Características nuevas)
- test(core): agregar suite de pruebas unitarias para base de datos y seeding
- feat(import): implementar pipeline de validación legacy y suite completa de pruebas
- feat: agregar tablas de auditoría de mareas y modelos de metadatos

### Fixed (Correcciones)
- (Sin cambios específicos en esta versión)

### Changed (Cambios y mejoras)
- (Sin cambios específicos en esta versión)

---

## [v1.0.0] - 2026-05-11

**Descripción**: Primera versión productiva estable del sistema completo.

### Added (Características nuevas)
- feat: implementar dashboard de procesos administrativo y corregir transparencia visual
- feat: estandarizar persistencia de encoding y carpetas de trabajo mediante metadatos
- feat(metadata): añadir campo metadata a entidades principales y estandarizar rutas de exportación
- feat(control-produccion): corregir lógica de agrupación de rayas en reporte PDF para consistencia con UI
- feat: implementación de cálculos dinámicos de pesos de captura y descarte
- feat: mejorar sistema de filtros en listas de mareas y lances
- feat: implementar auditoría de mareas desde base de datos local con optimización de performance y UX
- feat(biometria): mejorar resolución largo-peso y soporte para sexos indeterminados
- feat: implementar importación unificada de mareas (JSON portable + DBF legacy)
- feat(biometria): agregar parámetros largo-peso para merluza austral, bacalao, sardinero y espinoso
- feat: persistencia de totales corregidos, mejoras en auditoría y columnas de tiempo en lances
- feat(map): implementar reproducción de trayectoria y marcador de buque interactivo
- feat(map): implementar controles de reproducción de trayectoria y sincronización de fecha
- feat(ui): implementar HUD de navegación y unificar formato de coordenadas y tooltips en mapa
- feat(validation): implementar integridad de muestras y balance de masa diario
- feat: implementar totales en panel de detalle y detección dinámica de codificación en DBF
- feat(control-produccion): mejora relación de rayas y detalle de especies en lances
- feat: implementar reconciliación de Rajidae y añadir pruebas unitarias
- feat(ui): implementar SpeciesSelectorPremium y asegurar compatibilidad de temas en edición
- feat: implementar sistema de puente y fallback para especies legadas
- feat: implementar selección automática de primer registro en detalle de producción
- feat: agregar controles de visibilidad para capas de trayectoria en el mapa
- feat(map): implementar escala granular de ventana temporal y optimizar enfoque inicial basado en lances
- feat(map): unificar horas a local, sincronización lance-track y ajustes iniciales
- feat: implementar borrado global en cascada en todas las listas operativas
- feat(import): mapeo de campos adicionales en lances desde DBF
- feat(lances): reemplazar profundidad_arte_m por distancia_portones_m
- feat(mapa): añadir HUD de coordenadas en tiempo real bajo el puntero
- feat: implementar funcionalidad de reemplazo masivo de especies
- feat(marea): estandarización de unidades de descarte y actualización masiva
- feat(report): inclusión de fechas de marea y etapas en el encabezado del PDF
- feat(muestras): agregar columnas de Langostino a la tabla de frecuencias
- feat(ui): implementar diseño maestro-detalle con panel lateral en submuestras
- feat(ui): persistir el año seleccionado en el filtro de mareas
- feat: incluir especies sin producción en control y ordenar por captura
- feat(produccion): estandarizar visualización de especies y optimizar diseño horizontal
- feat(report): implementar exportación de resumen de producción a PDF en orientación apaisada
- feat(report): mejorar reporte de control de producción con resumen por área y detalle detallado
- feat(report): agrupar y totalizar reporte de control de producción por etapas de marea
- feat(report): añadir columna de horas y agrupar detalle de producción en el reporte
- feat(ui): implementar agrupación por etapas en lista de control de producción
- feat(model): agregar campo NroTotal a FrecuenciaTalla y aplicar migración
- feat: implementar integridad de conteos NroTotal y cálculo de peso alométrico
- feat(ui): mejorar visualización de la lista de producción y agregar columna categoría
- feat(reports): ocultar encabezado de etapa en PDF de producción si solo hay una etapa
- feat(reports): implementar generación de mapas estáticos y reportes asíncronos
- feat(ui): implementar diálogo y navegación para exportar recursos del informe
- feat(reporte): implementar módulo de exportación de recursos informe y optimización de mapas
- feat(reporte): implementar generación de libro Excel 'Tablas' con estadísticas por especie y etapa
- feat(reporte): añadir hoja de 'Áreas' al libro Excel y unificar formatos
- feat(reporte): agregar hoja de distribución estadística por sexo en el libro excel
- feat(reporte): agregar hoja GIS con posiciones de inicio de lances
- feat(reporte): agregar hoja de producción agrupada y sumarizada
- feat(reportes): implementar hojas de frecuencia biológica consolidada
- feat: integrar metadatos de buque y observador en importación y reportes
- feat: implementar reporte PDF de resumen de marea y consolidar UI de acciones
- feat: actualizar catálogo de biometría y tallas comerciales
- feat: separar hojas de frecuencia en Excel por tipo de muestra
- feat(report): incrustar imagen del mapa en la hoja GIS del excel
- feat(report): añadir porcentajes por sexo en hojas de frecuencia de tallas
- feat(report): añadir columna % TOTAL en hojas de frecuencia de tallas
- feat(report): generar gráficos de frecuencia de tallas con SkiaSharp

### Fixed (Correcciones)
- fix(import): corregir integridad de submuestras y validación fatal de barco
- fix(validation): implementar validación de velocidad y optimizar reporte de auditoría
- fix(import): asegurar que las especies existan antes de sembrar el catálogo largo-peso
- fix: solucionar cálculo de peso Abadejo (trimming de códigos) y agregar redondeo a 2 decimales
- fix: solucionar detección de abadejo (marea 0), aplicar trim sistemático y auto-inferir flags de muestras
- fix(ui): corregir visualización de tiles en el mapa replicando lógica legacy
- fix(audit): validar que peso de muestra no exceda captura de especie
- fix(audit): corregir alcance de variables en validación de peso de muestra
- fix(audit): usar código inidep puenteado para búsqueda de parámetros biometría
- fix(audit): refactorizar identificadores de especie a string para garantizar integridad de capturas y corregir totales en cero
- fix(audit): implementar regla de excepción para Granadero vinculando nombre a código INIDEP 7210090401
- fix(biometria): mejorar resolución largo-peso y soporte para sexos indeterminados
- fix: soportar sexo indeterminado legado (3) con fallback en parámetros largo-peso
- fix: asegurar limpieza de datos previos y compatibilidad de campos en importación
- fix(import): corregir error de casting en extractor y completar flujo de validación
- fix(import): corregir codificación de catálogos DBF y vulnerabilidades de seguridad
- fix(ui): corregir perdida de selección en SpeciesSelectorPremium al editar lances
- fix(ui): mejorar visualización y estabilidad del selector de especies
- fix(import): estandarización de formatos de fecha y corrección de ruta de reporte PDF
- fix: eliminar validaciones y correcciones de área en el motor de validación
- fix(report): calcular porcentaje de descarte total como (total descarte / total kilos) * 100
- fix(report): mostrar porcentajes con 2 decimales en resumen PDF
- fix(test): sincronizar constructor de MainWindowViewModel en tests
- fix(report): corregir escala de estadísticas en hoja Distribución (quitar x100 redundante)
- fix(report): formatear sumatorias como enteros en hoja Distribución
- fix(report): ordenar hojas de frecuencia (Captura antes que Descarte)

### Changed (Cambios y mejoras)
- refactor: simplificar menú lateral y centralizar procesos en el dashboard
- refactor(exportacion): simplificar estructura y nomenclatura de archivos
- refactor: eliminar sección inicio y dashboard
- refactor(especies): ajustar resolución de especies y limpiar lógica de granadero
- style: unificar DataGrid y scrollbars para temas claro/oscuro
- style(validation): renombrar categoría a Balance de Captura Diaria
- style(map): ajustar márgenes y clipping para mejorar legibilidad de etiquetas
- style(reports): refinamiento de gráficos de tallas y orden de muestras
- style: alinear datos numéricos y habilitar edición por doble clic en detalle lateral
- style(ui): eliminar encabezado redundante y reubicar botón de importación
- style(report): ajustes estéticos al gráfico de frecuencia (L6, suavizado, cuadrícula)
- ui(control-produccion): optimizar alineación, visibilidad y anchos de columna en detalle por especie
- ui: corregir alineación de columnas en listas y activar visualización de track por defecto
- ui: mejorar diseño del overlay de carga en exportación de recursos
- refactor(ui): implementar formulario lateral de altura completa y corregir visibilidad condicional en muestras
- feat: refinar reporte de resumen y UI de validación

---

## [v0.9.0] - 2026-05-06

**Descripción**: Sistema completo de reportes multiformat (PDF, Excel, Word).

### Added (Características nuevas)
- feat(report): ordenar observaciones de balance de masa cronológicamente
- feat(report): implementar exportación de resumen de producción a PDF en orientación apaisada
- feat(report): mejorar reporte de control de producción con resumen por área y detalle detallado
- feat(report): agrupar y totalizar reporte de control de producción por etapas de marea
- feat(report): añadir columna de horas y agrupar detalle de producción en el reporte
- feat(reporte): implementar módulo de exportación de recursos informe y optimización de mapas
- feat(reporte): implementar generación de libro Excel 'Tablas' con estadísticas por especie y etapa
- feat(reporte): añadir hoja de 'Áreas' al libro Excel y unificar formatos
- feat(reporte): agregar hoja de distribución estadística por sexo en el libro excel
- feat(reporte): agregar hoja GIS con posiciones de inicio de lances
- feat(reporte): agregar hoja de producción agrupada y sumarizada
- feat(reportes): implementar hojas de frecuencia biológica consolidada
- feat: refinar reporte de resumen y UI de validación
- feat: implementar reporte PDF de resumen de marea y consolidar UI de acciones
- feat: integrar metadatos de buque y observador en importación y reportes
- feat: actualizar catálogo de biometría y tallas comerciales
- feat: separar hojas de frecuencia en Excel por tipo de muestra
- feat(report): incrustar imagen del mapa en la hoja GIS del excel
- feat(report): añadir porcentajes por sexo en hojas de frecuencia de tallas
- feat(report): añadir columna % TOTAL en hojas de frecuencia de tallas
- feat(report): generar gráficos de frecuencia de tallas con SkiaSharp

### Fixed (Correcciones)
- fix(report): calcular porcentaje de descarte total como (total descarte / total kilos) * 100
- fix(report): mostrar porcentajes con 2 decimales en resumen PDF
- fix(report): ordenar hojas de frecuencia (Captura antes que Descarte)
- fix(report): corregir escala de estadísticas en hoja Distribución (quitar x100 redundante)
- fix(report): formatear sumatorias como enteros en hoja Distribución
- fix(report): correcciones de sintaxis en SkiaSharp

### Changed (Cambios y mejoras)
- style(report): ajustes estéticos al gráfico de frecuencia (L6, suavizado, cuadrícula)
- style(reports): refinamiento de gráficos de tallas y orden de muestras
- style(map): ajustar márgenes y clipping para mejorar legibilidad de etiquetas
- ui: mejorar diseño del overlay de carga en exportación de recursos

---

## [v0.8.0] - 2026-04-24

**Descripción**: Sistema de mapas avanzado con Mapsui y tracking satelital.

### Added (Características nuevas)
- feat(ui): integrar motor de mapas Mapsui y panel de visualización en lances
- feat(map): visualización de track satelital y lances como segmentos de arrastre
- feat(mapa): integrar proveedor de tiles Argenmap (IGN Argentina)
- feat(mapa): implementar soporte para capas GeoJSON estáticas
- feat(mapa): implementar selección y zoom automático de lances
- feat(map): implementar reproducción de trayectoria y marcador de buque interactivo
- feat(map): implementar controles de reproducción de trayectoria y sincronización de fecha
- feat(ui): implementar HUD de navegación y unificar formato de coordenadas y tooltips en mapa
- feat(mapa): añadir HUD de coordenadas en tiempo real bajo el puntero
- feat(map): implementar escala granular de ventana temporal y optimizar enfoque inicial basado en lances
- feat(map): unificar horas a local, sincronización lance-track y ajustes iniciales

### Fixed (Correcciones)
- fix(ui): corregir visualización de tiles en el mapa replicando lógica legacy
- fix(mapa): refinamientos en lógica de selección y auto-zoom

---

## [v0.7.0] - 2026-04-27

**Descripción**: Módulo completo de producción con biometría y cálculos alométricos.

### Added (Características nuevas)
- feat: implementar módulo de producción (lista y edición)
- feat: mejorar formulario de edición de producción con selector de especie avanzado y visualización de códigos de producto
- feat(biometria): implementar cálculo automatizado de peso alométrico largo-peso
- feat(biometria): mejorar resolución largo-peso y soporte para sexos indeterminados
- feat: soportar sexo indeterminado legado (3) con fallback en parámetros largo-peso
- feat: integrar metadatos de buque y observador en importación y reportes
- feat(ui): implementar SpeciesSelectorPremium y asegurar compatibilidad de temas en edición

### Fixed (Correcciones)
- fix(biometria): mejorar resolución largo-peso y soporte para sexos indeterminados
- fix: soportar sexo indeterminado legado (3) con fallback en parámetros largo-peso
- fix(audit): validar que peso de muestra no exceda captura de especie
- fix(audit): corregir alcance de variables en validación de peso de muestra
- fix(audit): usar código inidep puenteado para búsqueda de parámetros biometría
- fix(ui): corregir perdida de selección en SpeciesSelectorPremium al editar lances
- fix(ui): mejorar visualización y estabilidad del selector de especies

### Changed (Cambios y mejoras)
- style: alinear datos numéricos y habilitar edición por doble clic en detalle lateral
- style: unificar DataGrid y scrollbars para temas claro/oscuro

---

## [v0.6.0] - 2026-04-27

**Descripción**: Muestras biológicas con frecuencias de talla y análisis biométrico.

### Added (Características nuevas)
- feat: implementar lista y edición de muestras biológicas con frecuencias de talla
- feat(muestras): implementar gestión de muestras biológicas y mejorar navegación en listas
- feat: implementar módulo de edición de submuestras y mejorar seguridad de borrado en grillas

### Fixed (Correcciones)
- (Sin cambios específicos en esta versión)

### Changed (Cambios y mejoras)
- style: unificar diseño y comportamiento del formulario de muestras con submuestras

---

## [v0.5.0] - 2026-04-16

**Descripción**: Gestión de lances y coordenadas geográficas con validación.

### Added (Características nuevas)
- feat(lances): implementa la interfaz de gestión de lances y capturas
- feat: implementar gestión de lances, capturas y control CoordinatePickerPremium
- feat(import): implementar VMS, flexibilizar producción y corregir encoding DBF
- feat(validation): implementar validación de velocidad y optimizar reporte de auditoría
- feat: mejorar sistema de filtros en listas de mareas y lances
- feat(import): mapeo de campos adicionales en lances desde DBF

### Fixed (Correcciones)
- fix(import): corregir integridad de submuestras y validación fatal de barco
- fix(import): estandarización de formatos de fecha y corrección de ruta de reporte PDF
- fix: eliminar validaciones y correcciones de área en el motor de validación

### Changed (Cambios y mejoras)
- feat(ui): rediseño de planilla de lances y optimización de controles premium
- feat(ui): optimizar tabla de especies y buscador interactivo

---

## [v0.4.0] - 2026-04-14

**Descripción**: Módulo de mareas con filtrado avanzado y validación.

### Added (Características nuevas)
- feat(ui): implementada lista de mareas real con filtrado avanzado y control DatePickerPremium
- feat: agregar tablas de auditoría de mareas y modelos de metadatos
- feat(ui): implementar sistema de dialogos premium integrado con el tema de la aplicación
- feat(mareas): implementar validación de fechas, soporte para Enter y ajuste de anchos en barra de filtros
- feat: implementar navegación inteligente y optimización de data-entry (Enter as Tab, atajos Esc/Ctrl+N)
- feat: implementar persistencia de preferencias de usuario (tema visual y estado de ventana)
- feat: implementar control TimePickerPremium e integrarlo en etapas de marea
- feat(mareas): eliminar campos de hora en el formulario de etapas para agilizar la carga

### Fixed (Correcciones)
- (Sin cambios específicos en esta versión)

### Changed (Cambios y mejoras)
- feat(ui): implementar sistema de diseño de bordes rectos y remover campo código
- ui: configurar ventana principal para inicio centrado y maximizado

---

## [v0.3.0] - 2026-04-15

**Descripción**: Importación DBF legacy con validación y transformación de datos.

### Added (Características nuevas)
- feat(mareas): implementar UI de selección de archivos para importación DBF
- feat(import): finalizar importación legacy con feedback visual, limpieza de datos y soporte biométrico
- feat(import): implementar pipeline de validación legacy y suite completa de pruebas

### Fixed (Correcciones)
- fix(import): corregir error de casting en extractor y completar flujo de validación

### Changed (Cambios y mejoras)
- (Sin cambios específicos en esta versión)

---

## [v0.2.0] - 2026-04-13

**Descripción**: Arquitectura de datos con Entity Framework Core y SQLite.

### Added (Características nuevas)
- feat: implementar sistema de sincronización y sembrado de datos desde DBF
- feat(data): implementar arquitectura de datos completa y validación real-time
- feat(data): perfeccionar sincronización DBF, optimizar arranque y flexibilizar esquema
- test(core): agregar suite de pruebas unitarias para base de datos y seeding

### Fixed (Correcciones)
- (Sin cambios específicos en esta versión)

### Changed (Cambios y mejoras)
- chore: actualizar .gitignore
- chore: ignorar archivos .lscache

---

## [v0.1.0] - 2026-04-09

**Descripción**: UI premium con diseño visual 2026 y arquitectura MVVM.

### Added (Características nuevas)
- feat(app): configurar la base WPF y agregar el shell mock inicial
- feat(ui): pulir el shell mock y agregar principios de arquitectura
- feat(ui): elevación visual a premium 2026 (tokens semánticos, temas claro/oscuro)
- feat(ui): implementar mockup del formulario de edición de mareas con Smart Accordion Timeline

### Fixed (Correcciones)
- fix(ui): reemplazar óvalos por rectángulos redondeados

### Changed (Cambios y mejoras)
- feat(ui): rediseño a layout plano estilo Linear/Fluent (HeroBackground, SidebarItem, TableRow)

---

## [v0.0.1] - 2026-04-09

**Descripción**: Commit inicial y setup base del proyecto.

### Added (Características nuevas)
- Inicialización de repositorio Git
- Setup inicial de estructura de proyecto

### Changed (Cambios y mejoras)
- (N/A - Versión inicial)

---

## Guía de Versionamiento

Este CHANGELOG sigue:
- **Semantic Versioning 2.0.0** (MAJOR.MINOR.PATCH)
- **Conventional Commits 1.0.0** para mensajes de commit
- **Keep a Changelog** para formato de documentación

### Mapeo de Commits a Versiones

| Tipo de Commit | Impacto de Versión |
|---|---|
| `feat(...)` | MINOR (nueva característica) |
| `fix(...)` | PATCH (corrección de bug) |
| `feat!` o `fix!` | MAJOR (breaking change) |
| `docs`, `test`, `refactor`, `style`, `chore`, `build`, `ci` | Sin cambio de versión |

### Próximas Versiones

Para determinar la siguiente versión automáticamente:

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 next
```

Para generar changelog de cambios pendientes:

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 changelog --draft
```

---

**Última actualización**: 2026-05-17
**Período de desarrollo**: 2026-04-09 a 2026-05-16 (39 días)
**Total de commits documentados**: 270  
**Versión actual**: v1.5.0
