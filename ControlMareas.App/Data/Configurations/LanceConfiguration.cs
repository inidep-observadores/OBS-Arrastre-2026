using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Data.Configurations;

public sealed class LanceConfiguration : IEntityTypeConfiguration<Lance>
{
    public void Configure(EntityTypeBuilder<Lance> builder)
    {
        builder.ToTable("lances");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").IsRequired();
        builder.Property(x => x.MareaEtapaId).HasColumnName("marea_etapa_id").IsRequired();
        builder.Property(x => x.NroLance).HasColumnName("nro_lance").IsRequired();
        builder.Property(x => x.Fecha).HasColumnName("fecha").IsRequired();
        builder.Property(x => x.HoraInicio).HasColumnName("hora_inicio");
        builder.Property(x => x.HoraFinal).HasColumnName("hora_final");

        builder.Property(x => x.LatitudInicioDecimal).HasColumnName("latitud_inicio_decimal");
        builder.Property(x => x.LongitudInicioDecimal).HasColumnName("longitud_inicio_decimal");
        builder.Property(x => x.LatitudFinalDecimal).HasColumnName("latitud_final_decimal");
        builder.Property(x => x.LongitudFinalDecimal).HasColumnName("longitud_final_decimal");

        builder.Property(x => x.ProfundidadInicioM).HasColumnName("profundidad_inicio_m");
        builder.Property(x => x.ProfundidadFinalM).HasColumnName("profundidad_final_m");

        builder.Property(x => x.EstadoTiempoCodigo).HasColumnName("estado_tiempo_codigo");
        builder.Property(x => x.EstadoMarCodigo).HasColumnName("estado_mar_codigo");
        builder.Property(x => x.VientoDireccionGrados).HasColumnName("viento_direccion_grados");
        builder.Property(x => x.VientoFuerzaBeaufort).HasColumnName("viento_fuerza_beaufort");

        builder.Property(x => x.TemperaturaAireC).HasColumnName("temperatura_aire_c");
        builder.Property(x => x.TemperaturaRedC).HasColumnName("temperatura_red_c");
        builder.Property(x => x.PresionHpa).HasColumnName("presion_hpa");

        builder.Property(x => x.CapturaTotalKg).HasColumnName("captura_total_kg");
        builder.Property(x => x.DescarteTotalKg).HasColumnName("descarte_total_kg");
        builder.Property(x => x.VelocidadArrastreNudos).HasColumnName("velocidad_arrastre_nudos");
        builder.Property(x => x.RumboGrados).HasColumnName("rumbo_grados");

        builder.Property(x => x.MallaCopoMm).HasColumnName("malla_copo_mm");
        builder.Property(x => x.MallaAlasMm).HasColumnName("malla_alas_mm");
        builder.Property(x => x.CableFiladoM).HasColumnName("cable_filado_m");
        builder.Property(x => x.AberturaVerticalM).HasColumnName("abertura_vertical_m");
        builder.Property(x => x.DistanciaAlasM).HasColumnName("distancia_alas_m");
        builder.Property(x => x.DistanciaPortonesM).HasColumnName("distancia_portones_m");

        builder.Property(x => x.SelectividadSiNo).HasColumnName("selectividad_si_no");
        builder.Property(x => x.Comentarios).HasColumnName("comentarios");
        builder.Property(x => x.Metadata).HasColumnName("Metadata");

        builder.HasOne(x => x.MareaEtapa)
            .WithMany(e => e.Lances)
            .HasForeignKey(x => x.MareaEtapaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.MareaEtapaId, x.NroLance }).IsUnique();
        builder.HasIndex(x => x.MareaEtapaId).HasDatabaseName("idx_lances_marea_etapa_id");
        builder.HasIndex(x => x.Fecha).HasDatabaseName("idx_lances_fecha");
    }
}
