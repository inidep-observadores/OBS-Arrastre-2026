---
name: principios-arquitectura
description: Guía local para aplicar, cuando corresponda, principios de diseño y calidad como SOLID, Clean Code, Clean Architecture, separación por capas y Repository Pattern. Usar al diseñar arquitectura, refactorizar, definir límites entre UI/dominio/datos o evaluar si una abstracción realmente mejora el código.
---

# Principios de Arquitectura

Usa este skill para tomar decisiones de diseño con criterio, sin caer en sobreingeniería.

## Regla principal

Aplica principios de arquitectura y calidad solo cuando aporten claridad, mantenibilidad, testabilidad o menor acoplamiento.
No introduzcas patrones por moda ni por anticipación innecesaria.

## Criterios base

- Preferir simplicidad primero.
- Introducir abstracciones cuando exista una razón concreta.
- Mantener límites claros entre UI, aplicación, dominio e infraestructura.
- Reducir acoplamiento y aumentar cohesión.
- Diseñar para cambios probables, no para todos los cambios imaginables.

## SOLID en práctica

- Responsabilidad única: cada clase debería tener un motivo principal de cambio.
- Abierto/cerrado: extender comportamiento sin reescribir piezas estables cuando sea razonable.
- Sustitución de Liskov: no romper contratos esperados al heredar o implementar interfaces.
- Segregación de interfaces: preferir interfaces pequeñas y enfocadas.
- Inversión de dependencias: depender de abstracciones útiles en puntos de borde o coordinación.

## Clean Code

- Nombres precisos y consistentes con el dominio.
- Métodos cortos y con intención clara.
- Evitar booleanos y parámetros ambiguos cuando compliquen la lectura.
- Comentarios solo cuando agreguen contexto no obvio.
- Eliminar duplicación significativa, pero no forzar unificación prematura.

## Clean Architecture

- La UI no debe contener lógica de negocio relevante.
- La infraestructura no debería dictar el modelo del dominio más de lo necesario.
- Los casos de uso o servicios de aplicación coordinan reglas y flujos.
- Las dependencias deben apuntar hacia el centro lógico del sistema, no al revés.

## Repository Pattern

- Usarlo cuando ayude a aislar persistencia compleja o a estabilizar el acceso a datos.
- No usarlo si solo encapsula trivialmente lo que ya hace `DbContext` sin agregar valor.
- En EF Core, evaluar primero si un servicio de aplicación con `DbContext` o `IDbContextFactory` es suficiente.

## Señales de que sí conviene abstraer

- Hay lógica repetida de acceso a datos en múltiples lugares.
- Se mezclan responsabilidades de UI, persistencia y negocio.
- El código cuesta probar porque depende directamente de infraestructura.
- Un módulo cambia demasiado por causas no relacionadas.

## Señales de que no conviene abstraer todavía

- Hay una sola implementación simple y estable.
- El patrón solo agrega capas pasantes sin lógica.
- La abstracción hace menos evidente el flujo real del sistema.
- El costo cognitivo supera el beneficio inmediato.

## Aplicación en este repositorio

- Mantener MVVM como separación primaria en la UI WPF.
- Mantener la lógica de negocio fuera del code-behind.
- Evaluar repositorios solo cuando el acceso a EF Core crezca en complejidad real.
- Preservar nombres del dominio según `docs/esquema.sql` o documentar la traducción.

## Revisión rápida

Antes de aceptar una estructura o refactor, comprobar:

1. ¿La solución es más clara que antes?
2. ¿Reduce acoplamiento real?
3. ¿Evita mezclar capas?
4. ¿La abstracción tiene una razón concreta hoy?
5. ¿Sigue siendo fácil de navegar para el equipo?

## Recurso asociado

- Consulta `references/criterios.md` para una tabla rápida de cuándo aplicar cada principio o patrón.
