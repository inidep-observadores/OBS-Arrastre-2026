namespace ControlMareas.App.Data.Entities;

public sealed class Producto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Orden { get; set; }

    // Navigation properties
    public ICollection<RegistroProduccion> RegistrosProduccion { get; set; } = new List<RegistroProduccion>();
}
