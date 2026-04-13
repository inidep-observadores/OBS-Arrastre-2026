using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Data.Configurations;

public sealed class RegistroProduccionConfiguration : IEntityTypeConfiguration<RegistroProduccion>
{
    public void Configure(EntityTypeBuilder<RegistroProduccion> builder)
    {
        builder.ToTable("registros_produccion");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").IsRequired();
        builder.Property(x => x.MareaEtapaId).HasColumnName("marea_etapa_id").IsRequired();
        builder.Property(x => x.Fecha).HasColumnName("fecha").IsRequired();
        builder.Property(x => x.IdProducto).HasColumnName("id_producto").IsRequired();
        builder.Property(x => x.Categoria).HasColumnName("categoria");
        builder.Property(x => x.Kg).HasColumnName("kg");
        builder.Property(x => x.Comentarios).HasColumnName("comentarios");

        builder.HasOne(x => x.MareaEtapa)
            .WithMany(e => e.RegistrosProduccion)
            .HasForeignKey(x => x.MareaEtapaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Producto)
            .WithMany(p => p.RegistrosProduccion)
            .HasForeignKey(x => x.IdProducto)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MareaEtapaId, x.Fecha, x.IdProducto }).IsUnique();
        
        builder.HasIndex(x => x.MareaEtapaId).HasDatabaseName("idx_registros_produccion_marea_etapa_id");
        builder.HasIndex(x => x.Fecha).HasDatabaseName("idx_registros_produccion_fecha");
        builder.HasIndex(x => x.IdProducto).HasDatabaseName("idx_registros_produccion_producto");
    }
}
