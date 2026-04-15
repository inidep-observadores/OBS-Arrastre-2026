using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Data.Configurations;

public sealed class ItemSubmuestraConfiguration : IEntityTypeConfiguration<ItemSubmuestra>
{
    public void Configure(EntityTypeBuilder<ItemSubmuestra> builder)
    {
        builder.ToTable("items_submuestras");

        builder.HasKey(x => x.ID);

        builder.Property(x => x.ID).HasColumnName("ID").IsRequired();
        builder.Property(x => x.MuestraID).HasColumnName("MuestraID");
        builder.Property(x => x.NroEjemplar).HasColumnName("NroEjemplar").IsRequired();
        builder.Property(x => x.Sexo).HasColumnName("Sexo");
        builder.Property(x => x.Estadio).HasColumnName("Estadio");
        builder.Property(x => x.ReplecionGastrica).HasColumnName("ReplecionGastrica");
        builder.Property(x => x.Edad).HasColumnName("Edad");
        builder.Property(x => x.LargoTotalMm).HasColumnName("LargoTotalMm");
        builder.Property(x => x.LargoEstandarMm).HasColumnName("LargoEstandarMm");
        builder.Property(x => x.PesoTotalGramos).HasColumnName("PesoTotalGramos");
        builder.Property(x => x.Comentarios).HasColumnName("Comentarios");

        builder.HasOne(x => x.Muestra)
            .WithMany(m => m.ItemsSubmuestras)
            .HasForeignKey(x => x.MuestraID)
            .HasConstraintName("fk_items_submuestras_muestras_muestra_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MuestraID).HasDatabaseName("ix_items_submuestras_muestra_id");
        builder.HasIndex(x => x.Sexo).HasDatabaseName("ix_items_submuestras_sexo");
        builder.HasIndex(x => x.Estadio).HasDatabaseName("ix_items_submuestras_estadio");
    }
}
