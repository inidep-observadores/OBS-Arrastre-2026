# Control de mareas Agent Guide

## Scope

This repository contains a .NET 10 WPF desktop application for the Control de mareas domain.
Use this file as the project-specific operating guide for Codex-compatible agents.

## Project Defaults

- Primary stack: C#, .NET 10, WPF, XAML, EF Core, SQLite.
- Solution file: `OBSArrastre2026.sln`.
- Main app project: `OBSArrastre2026.App/OBSArrastre2026.App.csproj`.
- Current canonical schema source: `docs/esquema.sql`.
- Default branch model: Git Flow AVH with `develop` as the ongoing integration branch.
- Application startup uses `Microsoft.Extensions.Hosting` and dependency injection.
- Persistence uses EF Core with SQLite through `IDbContextFactory<AppDbContext>`.

## Working Rules

- Prefer incremental changes over broad scaffolding rewrites.
- Keep UI code simple and readable; do not introduce frameworks unless requested.
- Preserve nullable reference types and implicit usings unless there is a clear reason to change them.
- Prefer MVVM-style separation and keep business logic out of WPF code-behind.
- Apply principles such as SOLID, Clean Code, Clean Architecture, separation of concerns, and Repository Pattern when they are appropriate and materially improve the solution.
- Avoid introducing layers or patterns that do not add clear value to the current problem.
- Treat `docs/esquema.sql` as the source of truth for domain naming until the project defines entities or migrations.
- When creating new application structure, prefer folders such as `Views`, `ViewModels`, `Models`, `Services`, and `Infrastructure`.
- Keep text files in UTF-8 to avoid mojibake in XAML and markdown files.
- Write all project documentation in Spanish.
- Write Git messages in Spanish, including commits, merges, and related workflow messages.

## Build And Verification

- Restore/build solution: `dotnet build .\OBSArrastre2026.sln`
- Run the WPF app: `dotnet run --project .\OBSArrastre2026.App\OBSArrastre2026.App.csproj`
- When adding tests later, prefer `dotnet test` from the solution root.
- The repo pins the SDK through `global.json`; keep it aligned with the installed LTS SDK.

## Git Workflow

- Assume Git is already initialized.
- Use Git Flow AVH conventions.
- Base regular feature work from `develop`.
- Prefer branch names compatible with Git Flow, for example `feature/<topic>`, unless the user requests another pattern.
- Do not rewrite history or squash existing user commits unless explicitly requested.

## Agent Context

- Project-local skills live under `.codex/skills/`.
- Use the local skill `obs-arrastre-wpf` when working on architecture, naming, domain mapping, or implementation tasks in this repository.
- Use the local skill `commits-convencionales` when drafting commit, merge, squash, or revert messages.
- Use the local skill `principios-arquitectura` when evaluating or applying design principles and structural patterns.
- Use the local skill `versionamiento-semantico` for version management, changelog generation, and release workflows. See `.codex/skills/versionamiento-semantico/QUICK-START.md` for usage.
- Read `.codex/skills/obs-arrastre-wpf/references/domain-context.md` when changes depend on the fishing-domain schema.

## Semantic Versioning and Conventional Commits

All commits must follow Conventional Commits format: `type(scope): description`

**Type mapping to SemVer:**
- `feat`: New feature → MINOR version bump
- `fix`: Bug fix → PATCH version bump
- `feat!` or `fix!`: Breaking change → MAJOR version bump
- `docs`, `refactor`, `test`, `style`, `build`, `ci`, `chore`: No version bump

**Examples:**
```
feat(reportes): agregar soporte para exportación a Excel
fix(importacion): corregir validación de especies
feat(api)!: cambiar estructura de respuesta JSON
```

The local skill `versionamiento-semantico` automatically calculates versions, generates changelogs, and manages release tags based on these commits.

## Early Architecture Guidance

- Start with a clean vertical slice before adding abstractions.
- If a feature touches persistence, map table and column names from the schema explicitly instead of inventing alternative names.
- If a feature touches the UI, keep business logic out of code-behind except for thin event wiring.
- Prefer configuration and startup wiring in the host builder instead of manual singleton bootstrapping.
