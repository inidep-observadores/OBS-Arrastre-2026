using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Data.Configurations;

public sealed class LancePalangreConfiguration : IEntityTypeConfiguration<LancePalangre>
{
    public void Configure(EntityTypeBuilder<LancePalangre> builder)
    {
        builder.ToTable("lances_palangre");

        builder.HasKey(x => x.LanceId);

        builder.Property(x => x.LanceId)
            .HasColumnName("lance_id")
            .IsRequired();

        builder.Property(x => x.Identificacion).HasColumnName("identificacion");

        // Filado
        builder.Property(x => x.HoraInicioFilado).HasColumnName("hora_inicio_filado");
        builder.Property(x => x.HoraFinFilado).HasColumnName("hora_fin_filado");
        builder.Property(x => x.LatitudInicioFiladoDecimal).HasColumnName("latitud_inicio_filado_decimal");
        builder.Property(x => x.LongitudInicioFiladoDecimal).HasColumnName("longitud_inicio_filado_decimal");
        builder.Property(x => x.LatitudFinFiladoDecimal).HasColumnName("latitud_fin_filado_decimal");
        builder.Property(x => x.LongitudFinFiladoDecimal).HasColumnName("longitud_fin_filado_decimal");
        builder.Property(x => x.ProfundidadInicioFiladoM).HasColumnName("profundidad_inicio_filado_m");
        builder.Property(x => x.ProfundidadFinFiladoM).HasColumnName("profundidad_fin_filado_m");

        // Virado
        builder.Property(x => x.FechaVirado).HasColumnName("fecha_virado");
        builder.Property(x => x.HoraInicioVirado).HasColumnName("hora_inicio_virado");
        builder.Property(x => x.HoraFinVirado).HasColumnName("hora_fin_virado");
        builder.Property(x => x.LatitudInicioViradoDecimal).HasColumnName("latitud_inicio_virado_decimal");
        builder.Property(x => x.LongitudInicioViradoDecimal).HasColumnName("longitud_inicio_virado_decimal");
        builder.Property(x => x.LatitudFinViradoDecimal).HasColumnName("latitud_fin_virado_decimal");
        builder.Property(x => x.LongitudFinViradoDecimal).HasColumnName("longitud_fin_virado_decimal");
        builder.Property(x => x.ProfundidadInicioViradoM).HasColumnName("profundidad_inicio_virado_m");
        builder.Property(x => x.ProfundidadFinViradoM).HasColumnName("profundidad_fin_virado_m");
        builder.Property(x => x.RumboViradoGrados).HasColumnName("rumbo_virado_grados");

        // Arte
        builder.Property(x => x.TipoPalangre).HasColumnName("tipo_palangre");
        builder.Property(x => x.MaterialLineaMadre).HasColumnName("material_linea_madre");
        builder.Property(x => x.LargoLineaMadreM).HasColumnName("largo_linea_madre_m");
        builder.Property(x => x.TramosLineaMadre).HasColumnName("tramos_linea_madre");
        builder.Property(x => x.LargoBrazoladaCm).HasColumnName("largo_brazolada_cm");
        builder.Property(x => x.SeparacionBrazoladasM).HasColumnName("separacion_brazoladas_m");
        builder.Property(x => x.NumeroBrazoladasPorTramo).HasColumnName("numero_brazoladas_por_tramo");
        builder.Property(x => x.TipoAnzuelo).HasColumnName("tipo_anzuelo");
        builder.Property(x => x.LargoAnzueloMm).HasColumnName("largo_anzuelo_mm");
        builder.Property(x => x.AnchoAnzueloMm).HasColumnName("ancho_anzuelo_mm");
        builder.Property(x => x.AnzuelosTotal).HasColumnName("anzuelos_total");
        builder.Property(x => x.AnzuelosConCarnada).HasColumnName("anzuelos_con_carnada");
        builder.Property(x => x.AnzuelosSinCarnada).HasColumnName("anzuelos_sin_carnada");
        builder.Property(x => x.Carnada).HasColumnName("carnada");
        builder.Property(x => x.Observaciones).HasColumnName("observaciones");

        builder.HasOne(x => x.Lance)
            .WithOne(l => l.DetallePalangre)
            .HasForeignKey<LancePalangre>(x => x.LanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
