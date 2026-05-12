# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Proyecto

Aplicación de escritorio WPF (.NET 10) para el control de mareas y lances de pesca de arrastre del INIDEP. Gestiona buques, mareas, etapas, lances, muestras, producciones, exportaciones DBF y generación de reportes.

- Solución: `OBSArrastre2026.sln`
- Proyecto principal: `OBSArrastre2026.App/OBSArrastre2026.App.csproj`
- Proyecto de tests: `OBSArrastre2026.Tests/OBSArrastre2026.Tests.csproj`
- Fuente canónica del dominio: `docs/esquema.sql`

## Comandos esenciales

```powershell
# Compilar
dotnet build .\OBSArrastre2026.sln

# Ejecutar la aplicación
dotnet run --project .\OBSArrastre2026.App\OBSArrastre2026.App.csproj

# Correr todos los tests
dotnet test

# Correr un test específico
dotnet test --filter "NombreDelTest"

# Publicar instalador (requiere NSIS instalado)
.\build_installer.bat
```

## Arquitectura

### Patrón general

MVVM estricto con `CommunityToolkit.Mvvm`. La lógica de negocio vive en servicios y ViewModels; el code-behind XAML solo contiene cableado visual delgado.

El host de la aplicación (`App.xaml.cs`) usa `Microsoft.Extensions.Hosting` para DI. Todos los servicios, ViewModels y fábricas se registran allí.

### Acceso a datos

- EF Core 10 con SQLite.
- Siempre inyectar `IDbContextFactory<AppDbContext>` (nunca un `DbContext` de larga vida).
- Las migraciones están en `OBSArrastre2026.App/Migrations/`.
- `DatabaseInitializer` inicializa y migra la base de datos al arrancar.
- La base de datos se ubica en `%APPDATA%\OBSArrastre2026\obs-arrastre-2026.db` en tiempo de ejecución.

### Organización de carpetas (dentro de `OBSArrastre2026.App/`)

| Carpeta | Contenido |
|---|---|
| `Data/` | `AppDbContext`, entidades EF Core, configuraciones Fluent API |
| `Features/` | Slices verticales agrupados por área (Mareas, Lances) |
| `ViewModels/` | ViewModels MVVM con lógica y estado |
| `Views/` | Archivos XAML |
| `Services/` | Servicios de aplicación e integración |
| `Models/` | DTOs y modelos de presentación |
| `Infrastructure/` | Transversales: logging, theming, settings de usuario |
| `Converters/` | Value converters XAML |
| `Controls/` | Controles WPF personalizados |
| `Resources/` | ResourceDictionaries, plantillas Word (.docx) |

### Validación

FluentValidation con validadores por entidad (`MareaValidator`, `LanceValidator`, etc.). Los ViewModels editables heredan de `ValidatableViewModelBase`. Los validadores se registran automáticamente vía `AddValidatorsFromAssemblyContaining<App>()`.

### Integración DBF

`DbfExtractorService` / `DbfExporterService` manejan importación y exportación de archivos DBF del sistema legado. Los archivos fuente están en `source_data/*.DBF`.

### Reportes

- PDF: QuestPDF (`MareaReportService`)
- Excel: ClosedXML (`ExcelReportService`)
- Word: DocX con plantillas en `Resources/Templates/*.docx`

## Dominio (resumen desde `docs/esquema.sql`)

Entidades principales y relaciones clave:

- `buques` → `mareas` → `marea_etapas` → `lances` / `registros_produccion`
- `especies` referenciada desde `marea_etapas.EspecieObjetivoID`
- `productos` referenciada desde `registros_produccion.id_producto`

Restricciones importantes a preservar:
- `buques.IdRadial` único; `buques.Nombre` único
- `especies.CodigoInidep` único
- `registros_produccion` clave compuesta única en `(marea_etapa_id, fecha, id_producto)`
- Varios campos de fecha se almacenan como `TEXT` en SQLite; tratar explícitamente

Cuando el trabajo toca persistencia, identificar tablas, FK e índices en `docs/esquema.sql` antes de modificar código. Si la referencia rápida en `.codex/skills/obs-arrastre-wpf/references/domain-context.md` discrepa con `docs/esquema.sql`, prevalece el SQL.

## Convenciones de código

- Documentación del proyecto en **español**.
- Mensajes de Git en **español** con formato Conventional Commits: `tipo(ámbito): descripción`.
- Tipos válidos: `feat`, `fix`, `docs`, `refactor`, `style`, `test`, `build`, `ci`, `chore`, `perf`, `revert`.
- Archivos en **UTF-8** (crítico para XAML y markdown con tildes).
- Nullable reference types e implicit usings habilitados; no desactivarlos sin razón clara.
- Nombres de entidades C# alineados con los nombres de tabla del esquema SQL; documentar cualquier divergencia en código.

## Workflow Git

Git Flow AVH. Rama de integración: `develop`. Ramas de trabajo: `feature/<tema>`, `bugfix/<tema>`. No reescribir historia ni hacer squash de commits existentes salvo solicitud explícita.

## Skills locales (`.codex/skills/`)

| Skill | Cuándo usarlo |
|---|---|
| `obs-arrastre-wpf` | Arquitectura, naming, mapeo de dominio, tareas de implementación en este repo |
| `commits-convencionales` | Redactar mensajes de commit, merge, squash o revert |
| `principios-arquitectura` | Evaluar o aplicar SOLID, Clean Architecture, Repository Pattern |
