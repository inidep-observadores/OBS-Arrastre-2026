# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [v1.8.0] - 2026-05-20

### Added
- feat(ui): agregar zoom al lance cuando se hace clic en la lista de lances
- feat(ui): mejorar contraste de segmentos de lances y cambiar color de seleccionado a magenta

### Fixed
- fix(validacion): usar FechaFin de lance en calculos de validacion y auditoria
- fix(lance): usar FechaFin en tooltip y calculo de fecha-hora de fin
- fix(reportes): usar FechaFin en calculos de horas de lance en reportes
- fix(ui): usar FechaFin en tooltip del marcador de fin de lance en mapa

## [v1.7.0] - 2026-05-18

### Added
- feat(exportacion): permitir exportar utilizando nombres de archivo y carpeta originales
- feat(captura): hacer responsivo el panel de lances con limites de ancho y columnas proporcionales
- feat(reportes): agregar validacion proactiva de bloqueo de archivos antes de exportar
- feat(reportes): agregar seccion de comentarios de marea en PDF de produccion
- feat(reportes): mejorar rotulación y lógica de gráficos/tablas por sexo en frecuencias de tallas

### Fixed
- fix(captura): alinear columnas y ajustar ancho de panel de lances en sidebar
- fix(procesos): corregir secuencia en catch para asegurar la visualizacion del dialogo de error
- fix(procesos): usar ShowMessageAsync en exportaciones para evitar que los dialogos de error se cierren al instante
- fix(procesos): resolver DialogResult al fallar exportacion para evitar botones bloqueados
- fix(procesos): corregir bloqueo de botones en el lanzador usando Button nativo
- fix(validacion): usar magnitudes físicas calculadas y omitir heurística de consenso en descartes
- fix(reportes): agregar fila de totales al pdf de captura producción
