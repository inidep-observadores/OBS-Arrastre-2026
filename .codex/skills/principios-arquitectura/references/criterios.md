# Criterios rápidos

## SOLID

- Úsalo al dividir responsabilidades, diseñar servicios o interfaces.
- Evítalo como checklist rígido cuando complique código sencillo.

## Clean Code

- Úsalo siempre como criterio general de legibilidad.
- No uses "limpieza" como excusa para refactors grandes sin beneficio tangible.

## Clean Architecture

- Úsala cuando haya múltiples capas, reglas de negocio y dependencias de infraestructura.
- Simplifícala si el caso actual es un vertical slice pequeño.

## Repository Pattern

- Úsalo cuando centralice consultas complejas, reglas de acceso o composición reutilizable.
- Evítalo si solo envuelve `DbContext` con métodos CRUD triviales.

## Regla transversal

- Si un patrón no mejora claridad, testabilidad o desacoplamiento real, no introducirlo.
