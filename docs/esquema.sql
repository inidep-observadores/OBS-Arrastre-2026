-- buques definition

CREATE TABLE buques (
	ID TEXT NOT NULL PRIMARY KEY,
	Nombre TEXT NOT NULL,
	Matricula INTEGER NOT NULL,
	IdRadial INTEGER NOT NULL,
	IMO INTEGER NULL,
	MMSI INTEGER NULL
);

CREATE UNIQUE INDEX ix_buques_id_radial ON buques (IdRadial);
CREATE UNIQUE INDEX ix_buques_nombre ON buques (Nombre);


-- especies definition

CREATE TABLE especies (
    ID TEXT NOT NULL CONSTRAINT pk_especies PRIMARY KEY,
    CodigoInidep TEXT NULL,
    DocumentoInformativo TEXT NULL,
    Especifico TEXT NULL,
    Familia TEXT NULL,
    Frecuente INTEGER NOT NULL,
    Genero TEXT NULL,
    NombreCientifico TEXT NULL,
    NombreVulgar TEXT NULL,
    Orden TEXT NULL
);

CREATE UNIQUE INDEX ix_especies_codigo_inidep ON especies (CodigoInidep);
 
 
 -- especies_viejas definition
 
 CREATE TABLE especies_viejas (
     ID TEXT NOT NULL CONSTRAINT pk_especies_viejas PRIMARY KEY,
     CodigoInidep TEXT NULL,
     DocumentoInformativo TEXT NULL,
     Especifico TEXT NULL,
     Familia TEXT NULL,
     Frecuente INTEGER NOT NULL,
     Genero TEXT NULL,
     NombreCientifico TEXT NULL,
     NombreVulgar TEXT NULL,
     Orden TEXT NULL
 );
 
 CREATE UNIQUE INDEX ix_especies_viejas_codigo_inidep ON especies_viejas (CodigoInidep);


-- productos definition

CREATE TABLE productos (
    id TEXT NOT NULL PRIMARY KEY,
    codigo TEXT NOT NULL,
    descripcion TEXT NOT NULL,
    categoria TEXT NOT NULL,
    orden INTEGER NOT NULL DEFAULT 0
);


-- mareas definition

CREATE TABLE mareas (
    ID TEXT NOT NULL CONSTRAINT pk_mareas PRIMARY KEY,
    AnioInidep INTEGER NOT NULL,
    NumeroInidep INTEGER NOT NULL,
    Comentarios TEXT NULL,
    FechaInicio TEXT NOT NULL,
    FechaFin TEXT NULL,
    BuqueID TEXT NULL,
    BuqueCodigo INTEGER NULL,
    ObservadorNombre TEXT NULL,
    ObservadorApellido TEXT NULL,
    ObservadorCodigo INTEGER NULL,
    Metadata TEXT NULL,
    CONSTRAINT fk_mareas_buques_buque_id FOREIGN KEY (BuqueID) REFERENCES buques (ID)
);

CREATE INDEX ix_mareas_buque_id ON mareas (BuqueID);


-- marea_etapas definition

CREATE TABLE marea_etapas (
	ID TEXT NOT NULL PRIMARY KEY,
	FechaZarpada TEXT NOT NULL,
	FechaArribo TEXT,
	MareaID TEXT,
	EspecieObjetivoID TEXT,
	NombreCapitan TEXT,
	NombreOficialCubierta TEXT,
	NombreOficialPesca TEXT,
	AnioMareaBuque INTEGER,
	NumeroMareaBuque INTEGER,
	Metadata TEXT NULL,
	CONSTRAINT fk_marea_etapas_especies_especie_objetivo_id FOREIGN KEY (EspecieObjetivoID) REFERENCES especies(ID) ON DELETE SET NULL,
	CONSTRAINT fk_marea_etapas_mareas_marea_id FOREIGN KEY (MareaID) REFERENCES mareas(ID) ON DELETE CASCADE
);

CREATE INDEX ix_marea_etapas_marea_id ON marea_etapas(MareaID);
CREATE INDEX ix_marea_etapas_especie_objetivo_id ON marea_etapas(EspecieObjetivoID);


-- registros_produccion definition

CREATE TABLE registros_produccion (
    id TEXT NOT NULL PRIMARY KEY,
    marea_etapa_id TEXT NOT NULL,
    fecha TEXT NOT NULL,
    id_producto TEXT NOT NULL,
    categoria TEXT,
    kg REAL,
    comentarios TEXT,
    Metadata TEXT NULL,
    UNIQUE(marea_etapa_id, fecha, id_producto),
    FOREIGN KEY (marea_etapa_id) REFERENCES marea_etapas(id) ON DELETE CASCADE,
    FOREIGN KEY (id_producto) REFERENCES productos(id) ON DELETE RESTRICT
);

CREATE INDEX idx_registros_produccion_marea_etapa_id ON registros_produccion(marea_etapa_id);
CREATE INDEX idx_registros_produccion_fecha ON registros_produccion(fecha);
CREATE INDEX idx_registros_produccion_producto ON registros_produccion(id_producto);


-- lances definition

CREATE TABLE lances (
    id TEXT NOT NULL PRIMARY KEY,
    marea_etapa_id TEXT NOT NULL,
    nro_lance INTEGER NOT NULL,
    fecha TEXT NOT NULL,
    hora_inicio TEXT,
    hora_final TEXT,
    latitud_inicio_decimal REAL,
    longitud_inicio_decimal REAL,
    latitud_final_decimal REAL,
    longitud_final_decimal REAL,
    profundidad_inicio_m INTEGER,
    profundidad_final_m INTEGER,
    estado_tiempo_codigo INTEGER,
    estado_mar_codigo INTEGER,
    viento_direccion_grados INTEGER,
    viento_fuerza_beaufort INTEGER,
    temperatura_aire_c REAL,
    temperatura_red_c REAL,
    presion_hpa INTEGER,
    captura_total_kg REAL,
    descarte_total_kg REAL,
    velocidad_arrastre_nudos REAL,
    rumbo_grados INTEGER,
    malla_copo_mm INTEGER,
    malla_alas_mm INTEGER,
    cable_filado_m INTEGER,
    abertura_vertical_m REAL,
    distancia_alas_m REAL,
    distancia_portones_m REAL,
    selectividad_si_no INTEGER CHECK (selectividad_si_no IN (0,1)),
    Metadata TEXT NULL,
    UNIQUE(marea_etapa_id, nro_lance),
    FOREIGN KEY (marea_etapa_id) REFERENCES marea_etapas(id) ON DELETE CASCADE
);

CREATE INDEX idx_lances_marea_etapa_id ON lances(marea_etapa_id);
CREATE INDEX idx_lances_fecha ON lances(fecha);


-- muestras definition

CREATE TABLE muestras (
    ID TEXT NOT NULL CONSTRAINT pk_muestras PRIMARY KEY,
    LanceID TEXT NULL,
    EspecieID TEXT NULL,
    Comentarios TEXT NULL,
    EjemplaresPorKg INTEGER NOT NULL,
    Intervalo REAL NOT NULL,
    UnidadMedidaTalla INTEGER NOT NULL,
    ModoMedicionTalla INTEGER NOT NULL,
    Origen INTEGER NOT NULL,
    DiscriminaSexo INTEGER NOT NULL,
    HayIndeterminados INTEGER NOT NULL,
    PesoMuestra_PesoGramos REAL NULL,
    TipoMuestra INTEGER NOT NULL DEFAULT 1, -- 1=Estandar, 2=Descarte
    Metadata TEXT NULL,
    automatica INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT fk_muestras_especies_especie_id FOREIGN KEY (EspecieID) REFERENCES especies (ID) ON DELETE RESTRICT,
    CONSTRAINT fk_muestras_lances_lance_id FOREIGN KEY (LanceID) REFERENCES lances (ID) ON DELETE CASCADE
);

CREATE INDEX ix_muestras_especie_id ON muestras (EspecieID);
CREATE INDEX ix_muestras_lance_id ON muestras (LanceID);


-- frecuencias_de_tallas definition

CREATE TABLE frecuencias_de_tallas (
    ID TEXT NOT NULL CONSTRAINT pk_frecuencias_de_tallas PRIMARY KEY,
    MuestraID TEXT NULL,
    Talla REAL NOT NULL,
    NroMachos INTEGER NOT NULL,
    NroHembras INTEGER NOT NULL,
    NroIndeterminados INTEGER NOT NULL,
    NroTotal INTEGER NOT NULL,
    NroLangostinosMachoMaduros INTEGER NOT NULL,
    NroLangostinosHembraMaduras INTEGER NOT NULL,
    NroLangostinosHembraImpregnadas INTEGER NOT NULL,
    Metadata TEXT NULL,
    CONSTRAINT fk_frecuencias_de_tallas_muestras_muestra_id FOREIGN KEY (MuestraID) REFERENCES muestras (ID) ON DELETE CASCADE
);

CREATE INDEX ix_frecuencias_de_tallas_muestra_id ON frecuencias_de_tallas (MuestraID);


-- frecuencias_de_tallas_con_estadio definition

CREATE TABLE frecuencias_de_tallas_con_estadio (
    ID TEXT NOT NULL CONSTRAINT pk_frecuencias_de_tallas_con_estadio PRIMARY KEY,
    MuestraID TEXT NULL,
    Talla REAL NOT NULL,
    EstadiosHembras TEXT NULL,
    EstadiosMachos TEXT NULL,
    NroIndeterminados INTEGER NOT NULL,
    Metadata TEXT NULL,
    CONSTRAINT fk_frecuencias_de_tallas_con_estadio_muestras_muestra_id FOREIGN KEY (MuestraID) REFERENCES muestras (ID) ON DELETE CASCADE
);

CREATE INDEX ix_frecuencias_de_tallas_con_estadio_muestra_id ON frecuencias_de_tallas_con_estadio (MuestraID);


-- items_captura definition

CREATE TABLE items_captura (
    ID TEXT NOT NULL CONSTRAINT pk_items_captura PRIMARY KEY,
    LanceID TEXT NULL,
    EspecieID TEXT NULL,
    NumeroOrden INTEGER NOT NULL,
    TipoDatoCaptura INTEGER NOT NULL,
    DatoCaptura REAL NOT NULL,
    TipoDatoDescarte INTEGER NOT NULL,
    DatoDescarte REAL NOT NULL,
    Metadata TEXT NULL,
    CONSTRAINT fk_items_captura_especies_especie_id FOREIGN KEY (EspecieID) REFERENCES especies (ID) ON DELETE RESTRICT,
    CONSTRAINT fk_items_captura_lances_lance_id FOREIGN KEY (LanceID) REFERENCES lances (ID) ON DELETE CASCADE
);

CREATE INDEX ix_items_captura_especie_id ON items_captura (EspecieID);
CREATE INDEX ix_items_captura_lance_id ON items_captura (LanceID);


-- items_submuestras definition

CREATE TABLE items_submuestras (
    ID TEXT NOT NULL CONSTRAINT pk_items_submuestras PRIMARY KEY,
    MuestraID TEXT NULL,
    NroEjemplar INTEGER NOT NULL,
    Sexo INTEGER NULL,
    Estadio INTEGER NULL,
    ReplecionGastrica INTEGER NULL,
    Edad REAL NULL,
    Comentarios TEXT NULL,
    Metadata TEXT NULL,
    CONSTRAINT fk_items_submuestras_muestras_muestra_id FOREIGN KEY (MuestraID) REFERENCES muestras (ID) ON DELETE CASCADE
);

CREATE INDEX ix_items_submuestras_muestra_id ON items_submuestras (MuestraID);
CREATE INDEX ix_items_submuestras_sexo ON items_submuestras (Sexo);
CREATE INDEX ix_items_submuestras_estadio ON items_submuestras (Estadio);


-- item_contenido_gastrico definition

CREATE TABLE item_contenido_gastrico (
    ID TEXT NOT NULL CONSTRAINT pk_item_contenido_gastrico PRIMARY KEY,
    ItemSubmuestraID TEXT NULL,
    Grupo INTEGER NOT NULL,
    Porcentaje REAL NOT NULL,
    CantPiezas INTEGER NOT NULL,
    Comentarios TEXT NULL,
    Metadata TEXT NULL,
    CONSTRAINT fk_item_contenido_gastrico_items_submuestras_item_submuestra_id FOREIGN KEY (ItemSubmuestraID) REFERENCES items_submuestras (ID) ON DELETE CASCADE
);

CREATE INDEX ix_item_contenido_gastrico_item_submuestra_id ON item_contenido_gastrico (ItemSubmuestraID);


-- auditoria_mareas_lotes definition

CREATE TABLE auditoria_mareas_lotes (
    ID TEXT NOT NULL PRIMARY KEY,
    MareaID TEXT NOT NULL,
    Fecha TEXT NOT NULL,
    Tipo TEXT NOT NULL,
    Resultado TEXT NOT NULL,
    Metadatos TEXT NULL, -- JSON
    FOREIGN KEY (MareaID) REFERENCES mareas (ID) ON DELETE CASCADE
);

CREATE INDEX ix_auditoria_mareas_lotes_marea_id ON auditoria_mareas_lotes (MareaID);


-- auditoria_mareas_registros definition

CREATE TABLE auditoria_mareas_registros (
    ID TEXT NOT NULL PRIMARY KEY,
    LoteID TEXT NOT NULL,
    Nivel TEXT NOT NULL, -- ERROR, ADVERTENCIA, INFO
    Entidad TEXT NULL,   -- Tabla afectada (ej: lances)
    EntidadID TEXT NULL, -- ID del registro afectado
    Mensaje TEXT NOT NULL,
    Metadatos TEXT NULL, -- JSON
    FOREIGN KEY (LoteID) REFERENCES auditoria_mareas_lotes (ID) ON DELETE CASCADE
);

CREATE INDEX ix_auditoria_mareas_registros_lote_id ON auditoria_mareas_registros (LoteID);