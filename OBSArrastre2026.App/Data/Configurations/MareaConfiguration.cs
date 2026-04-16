using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Data.Configurations;

public sealed class MareaConfiguration : IEntityTypeConfiguration<Marea>
{
    public void Configure(EntityTypeBuilder<Marea> builder)
    {
        builder.ToTable("mareas");

        builder.HasKey(x => x.ID);

        builder.Property(x => x.ID)
            .HasColumnName("ID")
            .IsRequired();

        builder.Property(x => x.AnioInidep).HasColumnName("AnioInidep").IsRequired();
        builder.Property(x => x.NumeroInidep).HasColumnName("NumeroInidep").IsRequired();
        builder.Property(x => x.Comentarios).HasColumnName("Comentarios");
        builder.Property(x => x.FechaInicio).HasColumnName("FechaInicio").IsRequired();
        builder.Property(x => x.FechaFin).HasColumnName("FechaFin");

        builder.Property(x => x.BuqueID).HasColumnName("BuqueID");

        builder.HasOne(x => x.Buque)
            .WithMany(b => b.Mareas)
            .HasForeignKey(x => x.BuqueID)
            .HasConstraintName("fk_mareas_buques_buque_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.BuqueID).HasDatabaseName("ix_mareas_buque_id");
    }
}
