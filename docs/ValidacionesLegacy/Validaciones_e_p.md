# Validaciones de Datos - e_p.PRG

Este programa (`e_p.PRG`) realiza validaciones preliminares de los datos antes de su procesamiento.

---

## Archivos Requeridos

El usuario debe especificar al iniciar:

- **CAPTURA** - Archivo de capturas
- **MUESTRA** - Archivo de muestras
- **SUBMUESTRA** - Archivo de submuestras
- **PRODUCC** - Archivo de producción

El programa también utiliza internamente:

- **especie1.DBF** - Tabla de especies válida
- **especievie.DBF** - Tabla de especies anteriores/históricas

---

## Validaciones Implementadas

### 1. Validación de Submuestras (SUBMUES)

**Condición**: `largo_tot > 250`

**Acción**: Si encuentra un registro con largo total mayor a 250 mm, lo reporta

**Campos reportados**:

- Número de registro
- Lance
- Número de ejemplar
- Largo total

**Salida**:

```
Submuestra
Registro N° ; Lance ; Ejemplar ; Largo Total
```

**Criterio**: Cualquier ejemplar con largo total > 250 mm se considera atípico y se reporta para revisión.

---

### 2. Validación de Producción (PRODUCC)

#### 2.1 Validación de Especies

**Condición**: La especie no existe en la tabla `especie1.DBF`

**Validación**: Compara tanto `nomcient` como `nomvulcas`

```foxpro
IF TRIM(nomcient)=TRIM(a) .or. TRIM(nomvulcas)=TRIM(a)
   z=1  && Especie válida encontrada
```

**Acción**: Si la especie no está en la base, reporta:

```
Especie <nombre> de fecha <fecha> No está en la base
```

#### 2.2 Validación de Factor de Conversión

**Condición**: `producto = "ENTERO" (o "Entero")` Y `factor <> 1`

**Regla de negocio**: Cuando el producto es "ENTERO", el factor de conversión debe ser 1

**Acción**: Reporta:

```
Especie <nombre> de fecha <fecha> tiene factor incorrecto
```

**Significado**: El factor de conversión para producto entero debe ser 1 (sin transformación)

---

### 3. Validación de Captura (CAPTURA)

#### 3.1 Validación de Especies por Lance

**Para cada lance**, verifica las 25 especies posibles (especie_1 a especie_25)

**Validación**: El código de especie debe existir en `especie1.DBF`

```foxpro
FOR t=1 TO 25
  es="Especie_"+STR(t,1)  && Construye nombre de campo
  esp=&es                  && Obtiene valor
  IF esp <> 0              && Si no es "sin especie"
    && Busca en tabla de especies
```

**Condición de error**: Si `esp <> 0` Y no se encuentra el código en `codinidep`

**Acción**: Reporta:

```
Especie: <código> ; Lance <número> ; NO pertenece a la base, corresponde a la especie
```

Y busca en `especievie.DBF` para mostrar:

- Nombre científico correcto
- Nombre vulgar correcto

---

## Estructura de Reporte de Errores

El programa genera un archivo `Errores_previos.txt` con formato:

```
Submuestra
<encabezado>
datos...

Producción
<errores de especie>
<errores de factor>

Captura
<errores de especie>
```

---

## Códigos de Error

| Código | Significado |
|--------|-------------|
| E1 | Especie en submuestra con largo > 250 mm |
| E2 | Especie en producción no existe en base |
| E3 | Factor incorrecto para producto ENTERO |
| E4 | Código de especie en captura no existe en base |

---

## Flujo de Validación

```
1. Solicitar archivos al usuario (CAPTURA, SUBMUES, PRODUCC)
2. Verificar existencia de archivos
3. Abrir tablas en work areas:
   - SELECT 1: CAPTURA
   - SELECT 2: especie1 (referencia)
   - SELECT 4: PRODUCC
   - SELECT 5: SUBMUES
   - SELECT 6: especievie (histórico)
4. Ejecutar validaciones en orden
5. Generar reporte de errores
```

---

## Limitaciones

- Solo valida los 25 primeros campos de especie en CAPTURA
- No valida rangos de fechas
- No valida coherencia geográfica (lat/long)
- No valida coherencia entre capturas y muestras
- No valida totales (capt_total vs suma de especies)
