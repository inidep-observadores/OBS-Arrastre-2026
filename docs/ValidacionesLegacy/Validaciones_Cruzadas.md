# Validaciones Cruzadas - Detalle Completo

## Programas de Validacion y Procesamiento

| Programa | Funcion |
|----------|---------|
| e_p.PRG | Validacion preliminar de datos (reporte de errores) |
| qpasar2.PRG | Transferencia y calculo de areas |
| pcorrecc.PRG | Correccion de datos (GeneXus) - Estandariza valores |
| zpasar2.PRG | Conversion de datos PAL (palangre) |
| ypasar2.PRG | Validacion de especies |
| 21arr_mue.PRG | Reemplazo de areas en submuestras |
| LARGOPM.PRG | Calculo de peso de muestras |

---

## pcorrecc.PRG - Logica Detallada

Este programa es el mas completo para correccion de datos. Opera en "LEVELS" (niveles) secuenciales.

### Fuentes de Datos

- **RV40Bar**: Se extrae el primer nombre de BARCO no vacio encontrado en CAPTURA
- **RV12Maa**: Se extrae el NUMERO de MAREA desde el campo ARCHIVOS->arcmar (substr 1-3)

**PROBLEMA POTENCIAL**: El programa usa el ULTIMO valor de ARCHIVOS y el PRIMER barco de CAPTURA, lo aplica a TODOS los registros. Esto puede ser problematico si hay datos de multiples barcos/mareas.

---

### LEVEL 2: Lectura de ARCHIVOS

```
Para cada registro en ARCHIVOS:
  RV12Maa = VAL( SUBSTR(arcmar, 1, 3) )
```

Extrae el numero de marea del campo `arcmar`. Al final del loop, RV12Maa contiene el valor del ULTIMO registro de ARCHIVOS.

---

### LEVEL 3: Extraccion de BARCO

```
Para cada registro en CAPTURA:
  Si barco NO esta vacio:
    RV40Bar = barco
    SALIR del loop
```

Busca el primer registro con BARCO no vacio y lo almacena como valor canonico.

---

### LEVEL 4: Estandarizacion de CAPTURA

```
Para cada registro en CAPTURA:
  SI barco <> RV40Bar: REPLACE barco WITH RV40Bar
  SI marea <> RV12Maa: REPLACE marea WITH RV12Maa
```

**Validacion**: Asegura que todos los registros de CAPTURA tengan el mismo barco y marea. Si un registro tiene valores diferentes, los sobrescribe.

---

### LEVEL 5: Estandarizacion de MUESTRAS

```
Para cada registro en MUESTRAS:
  SI barmue <> RV40Bar: REPLACE barmue WITH RV40Bar
  SI marmue <> RV12Maa: REPLACE marmue WITH RV12Maa
```

**Validacion**: Estandariza los campos `barmue` y `marmue` en la tabla de muestras. Asegura consistencia con los valores de ARCHIVOS.

---

### LEVEL 6: Estandarizacion de SUBMUES

```
Para cada registro en SUBMUES:
  SI barsub <> RV40Bar: REPLACE barsub WITH RV40Bar
  SI marsub <> RV12Maa: REPLACE marsub WITH RV12Maa
```

**Validacion**: Estandariza los campos `barsub` y `marsub` en submuestras.

---

### LEVEL 7: Estandarizacion de PRODUCC

```
Para cada registro en PRODUCC:
  SI barPROD <> RV40Bar: REPLACE barPROD WITH RV40Bar
  SI marPROD <> RV12Maa: REPLACE marPROD WITH RV12Maa
```

**Validacion**: Estandariza los campos `barPROD` y `marPROD` en produccion.

---

### LEVEL 8: Estandarizacion de MUESDES

```
Para cada registro en MUESDES:
  SI barmud <> RV40Bar: REPLACE barmud WITH RV40Bar
  SI marmud <> RV12Maa: REPLACE marmud WITH RV12Maa
```

**Validacion**: Estandariza los campos `barmud` y `marmud` en muestras de descarte.

---

### LEVEL 9: Estandarizacion de SUDMUES

```
Para cada registro en SUDMUES:
  SI barsud <> RV40Bar: REPLACE barsud WITH RV40Bar
  SI marsud <> RV12Maa: REPLACE marsud WITH RV12Maa
```

**Validacion**: Estandariza los campos `barsud` y `marsud`.

---

### LEVEL 10-11: Correccion de Fechas de MUESTRAS

```
Para cada registro en CAPTURA (lance):
  fecha_capt = CAPTURA.fecha

  Para cada MUESTRAS con ese mismo lance:
    SI MUESTRAS.fecmue <> fecha_capt:
      REPLACE MUESTRAS.fecmue WITH fecha_capt
      RV9T = 1 (flag de correccion)
```

**Validacion**: La fecha de la muestra debe coincidir con la fecha del lance correspondiente. Si difiere, se corrige la muestra.

**Reporte**: Si se corrigio alguna fecha, imprime "Se corrigieron fechas en la base de muestras".

---

### LEVEL 12-13: Correccion de AREA en MUESTRAS

```
Para cada registro en MUESTRAS:
  SI area = 0 O area < 3500:
    Buscar CAPTURA con el mismo lance
    Recalcular area desde coordenadas de CAPTURA:

    lat = CAPTURA.lat_inic
    lon = CAPTURA.long_inic

    base = INT(lat) * 100 + INT(lon)

    lat_min = (lat - INT(lat)) * 100   // minutos de latitud
    lon_min = (lon - INT(lon)) * 100   // minutos de longitud

    // Correccion por cuadrante geografico
    SI lat_min <= 30 Y lon_min > 30:  area = base + 0.1
    SI lat_min <= 30 Y lon_min <= 30: area = base + 0.2
    SI lat_min > 30 Y lon_min > 30:   area = base + 0.3
    SI lat_min > 30 Y lon_min <= 30:   area = base + 0.4

    REPLACE MUESTRAS.area WITH area
```

**Regla de Validacion**:
- El area valida debe ser >= 3500 (representa coordenadas geograficas del Atlantico Southwest)
- Si area < 3500, es invalida y se recalcula

**Logica del Calculo**:
El area representa la posicion geografica codificada:
- 3500 = 35° latitud, 00° longitud (aproximado Uruguay)
- El sufijo decimal (.1, .2, .3, .4) indica el cuadrante segun minutos de lat/long

**Reporte**: Si se corrigio alguna area, imprime "Se corrigieron areas en la base de muestras".

---

### LEVEL 14: Correccion de Fechas de PRODUCC (year < 2000)

```
Para cada registro en PRODUCC:
  SI year(fecPROD) < 2000:
    ano = year(fecPROD) + 100
    fecPROD = CTOD( substr(fecPROD,1,6) + STR(ano,4) )
    REPLACE fecPROD WITH fecPROD
```

**Problema**: Dates before 2000 (like 1925) get their year incremented by 100. This suggests a Y2K compatibility fix - when the original data was entered in the 1900s, dates were stored incorrectly.

---

### LEVEL 15: Correccion de Fechas de CAPTURA (year < 2000)

```
Para cada registro en CAPTURA:
  SI year(fecha) < 2000:
    ano = year(fecha) + 100
    fecha = CTOD( substr(fecha,1,6) + STR(ano,4) )
    REPLACE fecha WITH fecha
```

**Misma logica de Y2K** - corrige fechas de capturas mal ingresadas.

---

### LEVEL 16: Correccion de Fechas de MUESTRAS (year < 2000)

```
Para cada registro en MUESTRAS:
  SI year(fecmue) < 2000:
    ano = year(fecmue) + 100
    fecmue = CTOD( substr(fecmue,1,6) + STR(ano,4) )
    REPLACE fecmue WITH fecmue
```

---

### LEVEL 17: Correccion de Fechas de SUBMUES (year < 2000)

```
Para cada registro en SUBMUES:
  SI year(fecsub) < 2000:
    ano = year(fecsub) + 100
    fecsub = CTOD( substr(fecsub,1,6) + STR(ano,4) )
    REPLACE fecsub WITH fecsub
```

---

## Resumen de Correcciones en pcorrecc.PRG

| Level | Tabla | Campo | Correccion |
|-------|-------|-------|------------|
| 4 | CAPTURA | barco, marea | Estandariza valores desde ARCHIVOS |
| 5 | MUESTRAS | barmue, marmue | Estandariza valores |
| 6 | SUBMUES | barsub, marsub | Estandariza valores |
| 7 | PRODUCC | barPROD, marPROD | Estandariza valores |
| 8 | MUESDES | barmud, marmud | Estandariza valores |
| 9 | SUDMUES | barsud, marsud | Estandariza valores |
| 10-11 | MUESTRAS | fecmue | Corrige a fecha del lance |
| 12-13 | MUESTRAS | area | Recalcula desde coordenadas CAPTURA si < 3500 |
| 14 | PRODUCC | fecPROD | Y2K: suma 100 al ano si < 2000 |
| 15 | CAPTURA | fecha | Y2K: suma 100 al ano si < 2000 |
| 16 | MUESTRAS | fecmue | Y2K: suma 100 al ano si < 2000 |
| 17 | SUBMUES | fecsub | Y2K: suma 100 al ano si < 2000 |

---

## e_p.PRG - Logica Detallada

### Validacion de Submuestras (largo_tot > 250)

```
Para cada registro en SUBMUES:
  SI largo_tot > 250:
    Reportar: "Registro;Lance;Ejemplar;Largo Total"
```

**Proposito**: Identificar individuos atipicos o datos erroneos. Ejemplares con largo total > 250mm son considerados atipicos para muchas especies y se marcan para revision.

---

### Validacion de Especies en PRODUCC

```
Para cada registro en PRODUCC:
  especie = espepro
  Buscar en especie1.DBF:
    SI nomcient = especie O nomvulcas = especie:
      z = 1 (valido)

  SI z = 0:
    Reportar: "Especie <nombre> de fecha <fecha> No esta en la base"
```

**Proposito**: Verificar que las especies registradas en produccion existan en el catalogo de especies validas.

---

### Validacion de Factor para Producto ENTERO

```
Para cada registro en PRODUCC:
  SI (producto = "ENTERO" O producto = "Entero") Y factor <> 1:
    Reportar: "Especie <nombre> de fecha <fecha> tiene factor incorrecto"
```

**Regla de Negocio**: Cuando el producto es "ENTERO" (sin procesamiento), el factor de conversion debe ser 1. Un factor diferente indica un error en el registro.

---

### Validacion de Especies en CAPTURA

```
Para cada registro en CAPTURA:
  Para cada campo especie_1 a especie_25:
    esp = campo especie
    SI esp <> 0:
      Buscar en especie1.DBF WHERE codinidep = esp
      SI no encontrado:
        Reportar: "Especie <codigo> Lance <n> NO pertenece a la base"
        Buscar en especievie.DBF para mostrar nombre correcto
```

**Proposito**: Verificar que los codigos de especie en la captura existan en la base de especies. Si no se encuentra, busca en la tabla historica (especievie) para identificar la especie correcta.

---

## qpasar2.PRG - Logica Detallada

### Calculo de Totales de Captura

```
Para cada registro en CAPTURA:
  capt_total = kg_1 + kg_2 + ... + kg_25
  descarte = descar_1 + descar_2 + ... + descar_25
  REPLACE capt_total, descarte
```

**Proposito**: Recalcula los totales de captura y descarte como suma de los valores parciales por especie. Corrige inconsistencias entre los totales registrados y la suma de partes.

---

### Propagacion de Datos a Archivos de Trabajo

```
MUE <> ".dbf":
  USE &mue EXCLUSIVE
  ZAP
  APPEND FROM muestras
  // Copia fecha, tarte, marea, barco, intervalo=1, fact_pond=1

SUB <> ".dbf":
  USE &sub EXCLUSIVE
  ZAP
  APPEND FROM submues
  // Copia fecha, marea, barco

PRO <> ".dbf":
  USE &pro EXCLUSIVE
  ZAP
  APPEND FROM producc
  // Copia fecha, marea, barco, especie
```

**Proposito**: Transfiere los datos de las tablas temporales a los archivos definitivos de la marea, agregando campos calculados como intervalo=1 y fact_pond=1.

---

## 21arr_mue.PRG - Calculo de Area

Este programa calcula el area desde coordenadas y la reemplaza en submuestras:

```
Para cada registro en CAPTURA:
  lat = lat_inic
  lon = long_inic

  // Calcular area base
  a = INT(lat) * 100 + INT(lon)

  // Minutos
  b = (lat - INT(lat)) * 100
  c = (lon - INT(lon)) * 100

  // Correccion por cuadrante
  SI b < 30 Y c < 30:  ar = a + 0.2
  SI b < 30 Y c >= 30: ar = a + 0.1
  SI b >= 30 Y c < 30: ar = a + 0.4
  SI b >= 30 Y c >= 30: ar = a + 0.3

  // Propagar a todas las submuestras del lance
  Para cada SUBMUES con ese lance:
    REPLACE area WITH ar

  Para cada MUESTRAS con ese lance:
    REPLACE area WITH ar
```

**Diferencia con pcorrecc.PRG**: Este programa NO tiene la validacion `area < 3500`. Reemplaza el area无条件 para todos los lances.

---

## LARGOPM.PRG - Calculo de Peso de Muestra

Este programa recalcula el peso de la muestra cuando `peso_mues = 0`:

### Logica de Calculo

```
Para cada registro en LG.DBF (parametros alometricos):
  SI CODIGO = especie:
    A1, B1 = parametros para machos
    A2, B2 = parametros para hembras
    A3, B3 = parametros para totales
```

### Formula de Peso

```
Para cada muestra en MUESTRAS (cuando peso_mues = 0):
  Para cada talla (talla_1 a talla_90):
    b = talla / 1000  // convertir a metros

    // Extraer componentes de la codificacion
    m2 = parte_entera(b / 1000000000000)  // numero de individuos
    itot = parte_entera((b/1000 - INT(b/1000)) * 1000)  // indeterminados
    htot = parte_entera( ... )  // hembras
    mtot = parte_entera( ... )  // machos

    SI prod="S" Y SN="o" Y PESO_MUES=0:
      // Calcular por sexos
      pm = A1 * (m2 ^ B1) * mtot  // peso machos
      pH = A2 * (m2 ^ B2) * htot  // peso hembras
      PESOM = PESOM + pm + pH

    SI prod="N" Y SN="o" Y PESO_MUES=0:
      // Calcular total (sin distincion de sexo)
      p = A3 * (m2 ^ B3) * (m2 + itot)
      PESOM = PESOM + p

  REPLACE PESO_MUES WITH PESOM
```

**Proposito**: Cuando el peso de la muestra no fue registrado, se estima usando la distribucion de tallas y parametros alometricos de la especie.

---

## Validacion de Area - Cuadrantes Geograficos

El sistema de codificacion de area usa 4 cuadrantes basados en minutos de latitud y longitud:

```
        lon_min <= 30    lon_min > 30
lat_min <= 30     0.2              0.1
lat_min > 30      0.4              0.3
```

**Ejemplo**: Si lat = 35.25 (25 minutos) y lon = 55.40 (40 minutos):
- base = 35 * 100 + 55 = 3555
- lat_min = 25 (<=30), lon_min = 40 (>30)
- area = 3555 + 0.1 = 3555.1

---

## Archivos Involucrados en Validaciones

### Archivos de Datos de Marea (ej: C15225.DBF)
- **CAPTURA (C)**: Lances/operaciones de pesca
- **MUESTRAS (M)**: Muestras de captura
- **SUBMUES (S)**: Submuestras (individuos medidos)
- **MUESDES (MD)**: Muestras de descarte
- **PRODUCC (P)**: Produccion/desembarque
- **LG (L)**: Parametros alometricos

### Tablas de Referencia
- **especie1.DBF**: Catalogo de especies actual
- **especievie.DBF**: Catalogo de especies historico
- **ARCHIVOS.DBF**: Configuracion de archivos de la marea

### Tablas de Conversion (PAL - Palangre)
- **PALCAPT.DBF, PALMUE.DBF, PALSUB.DBF, PALPRO.DBF**: Formato de datos de palangre
