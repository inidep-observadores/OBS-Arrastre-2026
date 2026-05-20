using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Data.Configurations;

public sealed class AuditoriaMareaLoteConfiguration : IEntityTypeConfiguration<AuditoriaMareaLote>
{
    public void Configure(EntityTypeBuilder<AuditoriaMareaLote> builder)
    {
        builder.ToTable("auditoria_mareas_lotes");

        builder.HasKey(x => x.ID);

        builder.Property(x => x.ID)
            .HasColumnName("ID")
            .IsRequired();

        builder.Property(x => x.MareaID)
            .HasColumnName("MareaID")
            .IsRequired();

        builder.Property(x => x.Fecha)
            .HasColumnName("Fecha")
            .IsRequired();

        builder.Property(x => x.Tipo)
            .HasColumnName("Tipo")
            .IsRequired();

        builder.Property(x => x.Resultado)
            .HasColumnName("Resultado")
            .IsRequired();

        builder.Property(x => x.Metadatos)
            .HasColumnName("Metadatos");

        builder.HasOne(x => x.Marea)
            .WithMany() // No navegamos desde Marea a sus auditorías por ahora (opcional)
            .HasForeignKey(x => x.MareaID)
            .HasConstraintName("fk_auditoria_mareas_lotes_mareas_marea_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MareaID)
            .HasDatabaseName("ix_auditoria_mareas_lotes_marea_id");
    }
}
