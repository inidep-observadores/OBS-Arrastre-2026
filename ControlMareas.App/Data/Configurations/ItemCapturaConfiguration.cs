using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Data.Configurations;

public sealed class ItemCapturaConfiguration : IEntityTypeConfiguration<ItemCaptura>
{
    public void Configure(EntityTypeBuilder<ItemCaptura> builder)
    {
        builder.ToTable("items_captura");

        builder.HasKey(x => x.ID);

        builder.Property(x => x.ID).HasColumnName("ID").IsRequired();
        builder.Property(x => x.LanceID).HasColumnName("LanceID");
        builder.Property(x => x.EspecieID).HasColumnName("EspecieID");
        builder.Property(x => x.NumeroOrden).HasColumnName("NumeroOrden").IsRequired();
        builder.Property(x => x.TipoDatoCaptura).HasColumnName("TipoDatoCaptura").IsRequired();
        builder.Property(x => x.DatoCaptura).HasColumnName("DatoCaptura").IsRequired();
        builder.Property(x => x.TipoDatoDescarte).HasColumnName("TipoDatoDescarte").IsRequired();
        builder.Property(x => x.DatoDescarte).HasColumnName("DatoDescarte").IsRequired();
        builder.Property(x => x.EspecieOriginal).HasColumnName("EspecieOriginal");
        builder.Property(x => x.Metadata).HasColumnName("Metadata");

        builder.HasOne(x => x.Lance)
            .WithMany(l => l.ItemsCaptura)
            .HasForeignKey(x => x.LanceID)
            .HasConstraintName("fk_items_captura_lances_lance_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Especie)
            .WithMany(e => e.ItemsCaptura)
            .HasForeignKey(x => x.EspecieID)
            .HasConstraintName("fk_items_captura_especies_especie_id")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.LanceID).HasDatabaseName("ix_items_captura_lance_id");
        builder.HasIndex(x => x.EspecieID).HasDatabaseName("ix_items_captura_especie_id");
    }
}
