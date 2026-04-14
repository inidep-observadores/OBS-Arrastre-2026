using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Data.Configurations;

public sealed class AuditoriaMareaRegistroConfiguration : IEntityTypeConfiguration<AuditoriaMareaRegistro>
{
    public void Configure(EntityTypeBuilder<AuditoriaMareaRegistro> builder)
    {
        builder.ToTable("auditoria_mareas_registros");

        builder.HasKey(x => x.ID);

        builder.Property(x => x.ID)
            .HasColumnName("ID")
            .IsRequired();

        builder.Property(x => x.LoteID)
            .HasColumnName("LoteID")
            .IsRequired();

        builder.Property(x => x.Nivel)
            .HasColumnName("Nivel")
            .IsRequired();

        builder.Property(x => x.Entidad)
            .HasColumnName("Entidad");

        builder.Property(x => x.EntidadID)
            .HasColumnName("EntidadID");

        builder.Property(x => x.Mensaje)
            .HasColumnName("Mensaje")
            .IsRequired();

        builder.Property(x => x.Metadatos)
            .HasColumnName("Metadatos");

        builder.HasOne(x => x.Lote)
            .WithMany(l => l.Registros)
            .HasForeignKey(x => x.LoteID)
            .HasConstraintName("fk_auditoria_mareas_registros_lotes_lote_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LoteID)
            .HasDatabaseName("ix_auditoria_mareas_registros_lote_id");
    }
}
