# Domain Context

This file summarizes the current domain model described in `docs/esquema.sql`.
Use it as a quick reference before opening the full SQL file.

## Core Areas

- `buques`: vessel master data with `Nombre`, `Matricula`, `IdRadial`, `IMO`, `MMSI`.
- `mareas`: trip/campaign header linked to `buques`.
- `marea_etapas`: stage or voyage segment linked to `mareas` and target `especies`.
- `registros_produccion`: production records by date, stage, and product.
- `lances`: haul or fishing operation records linked to a `marea_etapa`.
- `especies`: biological species catalog.
- `productos`: product catalog and ordering metadata.

## Important Relationships

- `mareas.BuqueID -> buques.ID`
- `marea_etapas.MareaID -> mareas.ID`
- `marea_etapas.EspecieObjetivoID -> especies.ID`
- `registros_produccion.marea_etapa_id -> marea_etapas.id`
- `registros_produccion.id_producto -> productos.id`
- `lances.marea_etapa_id -> marea_etapas.id`

## Constraints Worth Preserving

- `buques.IdRadial` is unique.
- `buques.Nombre` is unique.
- `especies.CodigoInidep` is unique.
- `registros_produccion` has a unique composite key on `(marea_etapa_id, fecha, id_producto)`.

## Practical Guidance

- Use IDs from the SQL schema as canonical persistence identifiers.
- Keep date/time handling explicit because several schema fields are stored as `TEXT`.
- Check cascading behavior before delete operations; for example, `registros_produccion` cascades from `marea_etapa`.
- When modeling the domain in C#, keep table boundaries visible instead of collapsing unrelated concepts too early.
