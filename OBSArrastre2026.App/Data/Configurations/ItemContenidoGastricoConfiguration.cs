using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Data.Configurations;

public sealed class ItemContenidoGastricoConfiguration : IEntityTypeConfiguration<ItemContenidoGastrico>
{
    public void Configure(EntityTypeBuilder<ItemContenidoGastrico> builder)
    {
        builder.ToTable("item_contenido_gastrico");

        builder.HasKey(x => x.ID);

        builder.Property(x => x.ID).HasColumnName("ID").IsRequired();
        builder.Property(x => x.ItemSubmuestraID).HasColumnName("ItemSubmuestraID");
        builder.Property(x => x.Grupo).HasColumnName("Grupo").IsRequired();
        builder.Property(x => x.Porcentaje).HasColumnName("Porcentaje").IsRequired();
        builder.Property(x => x.CantPiezas).HasColumnName("CantPiezas").IsRequired();
        builder.Property(x => x.Comentarios).HasColumnName("Comentarios");

        builder.HasOne(x => x.ItemSubmuestra)
            .WithMany(s => s.ContenidosGastricos)
            .HasForeignKey(x => x.ItemSubmuestraID)
            .HasConstraintName("fk_item_contenido_gastrico_items_submuestras_item_submuestra_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ItemSubmuestraID).HasDatabaseName("ix_item_contenido_gastrico_item_submuestra_id");
    }
}
