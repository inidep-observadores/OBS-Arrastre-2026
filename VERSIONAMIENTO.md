# Guía de Versionamiento Semántico

Documentación rápida para gestionar versiones en este proyecto.

## 📍 Ubicación de la skill

La skill está en: `.codex/skills/versionamiento-semantico/`

Documentación completa en: `.codex/skills/versionamiento-semantico/QUICK-START.md`

## 🚀 Comandos rápidos

### Determinar siguiente versión

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 next
```

Muestra la siguiente versión automáticamente calculada basada en commits.

### Listar commits nuevos

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 commits
```

Muestra todos los commits desde la última versión tagueada.

### Generar changelog

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 changelog --output CHANGELOG.md
```

Crea o actualiza CHANGELOG.md con cambios automáticamente agrupados.

### Ver borrador de changelog

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 changelog --draft
```

Muestra el changelog sin guardar a archivo (útil para revisión).

### Crear tag de versión

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 tag v1.2.3 --message "Release v1.2.3"
```

Crea un tag anotado en Git. Luego: `git push origin --tags`

### Validar versión

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 validate v1.2.3
```

Verifica que una versión cumple el formato SemVer.

### Ver ayuda

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 help
```

## 📝 Cómo escribir commits

### Nueva característica

```powershell
git commit -m "feat(reportes): agregar exportación a Excel"
# Incrementa MINOR (1.2.0 → 1.3.0)
```

### Corrección de bug

```powershell
git commit -m "fix(validacion): corregir validación de especies"
# Incrementa PATCH (1.2.0 → 1.2.1)
```

### Breaking change

```powershell
git commit -m "feat(api)!: cambiar estructura de respuesta JSON

BREAKING CHANGE: El campo 'cantidad' ahora es 'cantidad_kg'"
# Incrementa MAJOR (1.2.0 → 2.0.0)
```

### Sin impacto en versión

```powershell
git commit -m "refactor(services): simplificar inyección de dependencias"
git commit -m "docs: actualizar README"
git commit -m "test(validacion): agregar tests"
# Sin cambio de versión
```

## 🔄 Workflow de release completo

### 1. Revisar commits y versión siguiente

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 commits
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 next
# Ej: Resultado → v1.3.0
```

### 2. Generar y revisar changelog

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 changelog --draft
```

Revisar el output y editar manualmente si es necesario.

### 3. Guardar changelog

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 changelog --output CHANGELOG.md
git add CHANGELOG.md
git commit -m "docs(changelog): actualizar para v1.3.0"
```

### 4. Crear tag (en rama main con Git Flow)

```powershell
git checkout main
git pull origin main
git merge --no-ff develop

.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 tag v1.3.0 --message "Release v1.3.0: nuevas características de reportes"

git push origin main
git push origin --tags

git checkout develop
git merge main
git push origin develop
```

## ⚙️ Configuración recomendada: Alias en PowerShell

Editar `$PROFILE`:

```powershell
notepad $PROFILE
```

Agregar:

```powershell
$semverPath = Join-Path (git rev-parse --show-toplevel) ".codex\skills\versionamiento-semantico\scripts\semver.ps1"
New-Alias -Name semver -Value $semverPath -Force -Scope Global
```

Luego usar directamente:

```powershell
semver next
semver changelog --output CHANGELOG.md
semver tag v1.2.3
```

## 📊 Mapeo de commits a versiones

| Tipo de commit | Versión | Ejemplo |
|---|---|---|
| `feat` | MINOR | 1.2.0 → 1.3.0 |
| `fix` | PATCH | 1.2.0 → 1.2.1 |
| `feat!` / `fix!` | MAJOR | 1.2.0 → 2.0.0 |
| `docs`, `refactor`, `test` | Sin cambio | 1.2.0 → 1.2.0 |

## 📚 Documentación completa

- **Especificación SemVer**: `.codex/skills/versionamiento-semantico/references/semver-spec.md`
- **Conventional Commits**: `.codex/skills/versionamiento-semantico/references/conventional-commits.md`
- **Formato de Changelog**: `.codex/skills/versionamiento-semantico/references/changelog-format.md`
- **Guía rápida**: `.codex/skills/versionamiento-semantico/QUICK-START.md`
- **Índice completo**: `.codex/skills/versionamiento-semantico/INDEX.md`

## 🆘 Troubleshooting

### "No hay commits nuevos"

Normal si ya hiciste release. Crea un nuevo commit o verifica con:

```powershell
git log --oneline v1.0.0..HEAD
```

### "Versión inválida"

Usa formato `vMAJOR.MINOR.PATCH`. Ejemplos válidos:
- ✅ `v1.0.0`
- ✅ `v1.2.3`
- ❌ `1.0.0` (falta v)
- ❌ `v1.2` (falta PATCH)

### "Commit inválido"

Usa Conventional Commits: `tipo(scope): descripción`
- ✅ `feat(reportes): agregar Excel`
- ❌ `Feature: add Excel`
- ❌ `fixed bug`

## 📞 Referencias

- 🌐 [Semantic Versioning 2.0.0](https://semver.org/)
- 🌐 [Conventional Commits 1.0.0](https://www.conventionalcommits.org/)
- 📖 CLAUDE.md - Sección "Versionamiento Semántico"
- 📖 AGENTS.md - Sección "Semantic Versioning and Conventional Commits"

---

**Última actualización**: 2026-05-17  
**Skill**: versionamiento-semantico v1.0.0
