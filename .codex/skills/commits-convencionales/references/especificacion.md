# Especificación base

Fuente oficial: [Conventional Commits 1.0.0 en español](https://www.conventionalcommits.org/es/v1.0.0/)

## Estructura

- Formato general: `<tipo>[ámbito opcional]: <descripción>`
- Cuerpo opcional después de una línea en blanco.
- Notas al pie opcionales después de otra línea en blanco.

## Reglas clave

- `feat` se usa para nuevas funcionalidades.
- `fix` se usa para correcciones de errores.
- Un cambio incompatible puede indicarse con `!` en el encabezado o con `BREAKING CHANGE:` en el pie.
- Se permiten tipos adicionales como `docs`, `build`, `refactor`, `test`, `ci`, `perf`, `style`, `chore` y `revert`.
- El ámbito es opcional y va entre paréntesis.

## Interpretación práctica

- `feat` se correlaciona con un cambio menor de versión en SemVer.
- `fix` se correlaciona con un cambio de parche.
- `BREAKING CHANGE` se correlaciona con un cambio mayor.

## Ejemplos de referencia

- `feat: agrega exportación por lote`
- `fix(parser): corrige el manejo de espacios múltiples`
- `refactor!: elimina compatibilidad con una API anterior`

  `BREAKING CHANGE: cambia el contrato de inicialización del módulo`
