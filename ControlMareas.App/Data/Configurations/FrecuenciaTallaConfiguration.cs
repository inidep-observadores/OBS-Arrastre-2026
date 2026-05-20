using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Data.Configurations;

public sealed class FrecuenciaTallaConfiguration : IEntityTypeConfiguration<FrecuenciaTalla>
{
    public void Configure(EntityTypeBuilder<FrecuenciaTalla> builder)
    {
        builder.ToTable("frecuencias_de_tallas");

        builder.HasKey(x => x.ID);

        builder.Property(x => x.ID).HasColumnName("ID").IsRequired();
        builder.Property(x => x.MuestraID).HasColumnName("MuestraID");
        builder.Property(x => x.Talla).HasColumnName("Talla").IsRequired();
        builder.Property(x => x.NroMachos).HasColumnName("NroMachos").IsRequired();
        builder.Property(x => x.NroHembras).HasColumnName("NroHembras").IsRequired();
        builder.Property(x => x.NroIndeterminados).HasColumnName("NroIndeterminados").IsRequired();

        builder.Property(x => x.NroTotal).HasColumnName("NroTotal").IsRequired();
        builder.Property(x => x.NroLangostinosMachoMaduros).HasColumnName("NroLangostinosMachoMaduros").IsRequired();
        builder.Property(x => x.NroLangostinosHembraMaduras).HasColumnName("NroLangostinosHembraMaduras").IsRequired();
        builder.Property(x => x.NroLangostinosHembraImpregnadas).HasColumnName("NroLangostinosHembraImpregnadas").IsRequired();
        builder.Property(x => x.Metadata).HasColumnName("Metadata");

        builder.HasOne(x => x.Muestra)
            .WithMany(m => m.FrecuenciasTallas)
            .HasForeignKey(x => x.MuestraID)
            .HasConstraintName("fk_frecuencias_de_tallas_muestras_muestra_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MuestraID).HasDatabaseName("ix_frecuencias_de_tallas_muestra_id");
    }
}
