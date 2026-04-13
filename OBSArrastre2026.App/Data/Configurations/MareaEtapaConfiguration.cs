using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Data.Configurations;

public sealed class MareaEtapaConfiguration : IEntityTypeConfiguration<MareaEtapa>
{
    public void Configure(EntityTypeBuilder<MareaEtapa> builder)
    {
        builder.ToTable("marea_etapas");

        builder.HasKey(x => x.ID);

        builder.Property(x => x.ID).HasColumnName("ID").IsRequired();
        builder.Property(x => x.FechaZarpada).HasColumnName("FechaZarpada").IsRequired();
        builder.Property(x => x.FechaArribo).HasColumnName("FechaArribo");
        builder.Property(x => x.MareaID).HasColumnName("MareaID");
        builder.Property(x => x.EspecieObjetivoID).HasColumnName("EspecieObjetivoID");
        builder.Property(x => x.NombreCapitan).HasColumnName("NombreCapitan");
        builder.Property(x => x.NombreOficialCubierta).HasColumnName("NombreOficialCubierta");
        builder.Property(x => x.NombreOficialPesca).HasColumnName("NombreOficialPesca");
        builder.Property(x => x.AnioMareaBuque).HasColumnName("AnioMareaBuque");
        builder.Property(x => x.NumeroMareaBuque).HasColumnName("NumeroMareaBuque");

        builder.HasOne(x => x.Marea)
            .WithMany(m => m.Etapas)
            .HasForeignKey(x => x.MareaID)
            .HasConstraintName("fk_marea_etapas_mareas_marea_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EspecieObjetivo)
            .WithMany(e => e.MareaEtapas)
            .HasForeignKey(x => x.EspecieObjetivoID)
            .HasConstraintName("fk_marea_etapas_especies_especie_objetivo_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.MareaID).HasDatabaseName("ix_marea_etapas_marea_id");
        builder.HasIndex(x => x.EspecieObjetivoID).HasDatabaseName("ix_marea_etapas_especie_objetivo_id");
    }
}
