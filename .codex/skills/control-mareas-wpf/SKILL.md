---
name: control-mareas-wpf
description: Project-local guidance for the Control de Mareas .NET 10 WPF application. Use when working in this repository on app structure, WPF/XAML implementation, EF Core with SQLite, dependency injection, domain mapping, or schema-driven features tied to `docs/esquema.sql`.
---

# Control de Mareas WPF

Use this skill to work inside the Control de Mareas repository with minimal re-discovery.

## Quick Start

1. Confirm the current workspace root is the repository root.
2. Read `AGENTS.md` before making broad changes.
3. Build with `dotnet build .\ControlMareas.sln` after meaningful edits.
4. If the task depends on business entities or persistence, read `references/domain-context.md`.

## Project Facts

- The application is a WPF desktop app on `.NET 10`.
- The active solution is `ControlMareas.sln`.
- The main executable project is `ControlMareas.App`.
- The database/domain source of truth currently lives in `docs/esquema.sql`.
- Git workflow follows Git Flow AVH and day-to-day work should align with `develop`.
- Startup uses `Generic Host` and dependency injection.
- Persistence uses EF Core with SQLite and `IDbContextFactory<AppDbContext>`.

## Implementation Guidance

- Prefer straightforward WPF structure first: `Views`, `ViewModels`, `Models`, `Services`, `Infrastructure`.
- Keep code-behind light. Put domain and stateful behavior in view models or services unless the change is purely visual.
- For data access in the desktop app, prefer injecting `IDbContextFactory<AppDbContext>` instead of keeping a long-lived `DbContext`.
- Preserve schema naming intentionally. If introducing C# model names that differ from SQL names, document the mapping in code.
- Avoid speculative libraries and infrastructure before there is a concrete need.
- Keep files UTF-8 encoded, especially XAML and markdown, to avoid broken accents.
- Write repository documentation in Spanish.
- Write Git workflow messages in Spanish, including commit and merge messages.

## Domain-Driven Work

- Use `references/domain-context.md` as the first summary of the SQL schema.
- If the reference and `docs/esquema.sql` disagree, trust `docs/esquema.sql`.
- For persistence-related tasks, identify the affected tables, foreign keys, and unique indexes before changing code.

## Validation

- For project changes, run `dotnet build .\ControlMareas.sln`.
- For future tests, prefer solution-level execution from the repo root.

## Resources

### references/

- `references/domain-context.md`: concise domain summary derived from `docs/esquema.sql`.
