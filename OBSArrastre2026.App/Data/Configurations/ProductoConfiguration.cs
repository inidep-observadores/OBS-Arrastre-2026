using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Data.Configurations;

public sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("productos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").IsRequired();
        builder.Property(x => x.Codigo).HasColumnName("codigo").IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion").IsRequired();
        builder.Property(x => x.Categoria).HasColumnName("categoria").IsRequired();
        builder.Property(x => x.Orden).HasColumnName("orden").HasDefaultValue(0);
    }
}
