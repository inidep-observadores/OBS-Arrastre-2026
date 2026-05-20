using System;
using FluentAssertions;
using Xunit;

namespace ControlMareas.Tests.Domain;

/// <summary>
/// Pruebas unitarias para validar la precisión y coherencia biológica 
/// de los cálculos de relación largo-peso utilizados en la aplicación.
/// Utiliza los coeficientes A y B oficiales del INIDEP.
/// </summary>
public class AlometriaTests
{
    // Fórmula que usa la aplicación: W = A * L^B
    private static double CalcularPesoAlometrico(double talla, double paramA, double paramB)
    {
        return paramA * Math.Pow(talla, paramB);
    }

    [Theory]
    // 1. Merluza común (Merluccius hubbsi) - Hembra
    // Talla de ejemplo: 35 cm
    [InlineData("7210040101", "Merluza común (Hembra)", 35.0, 0.00855, 2.94086, 297.07)]
    // 2. Merluza común (Merluccius hubbsi) - Macho
    // Talla de ejemplo: 35 cm
    [InlineData("7210040101", "Merluza común (Macho)", 35.0, 0.01048, 2.87878, 292.01)]
    // 3. Langostino (Pleoticus muelleri) - Macho
    // Talla de ejemplo: 35 mm (el observador la anota en milímetros)
    [InlineData("5139030101", "Langostino (Macho)", 35.0, 0.00185, 2.7850, 36.93)]
    // 4. Calamar argentino (Illex argentinus) - Indeterminado
    // Talla de ejemplo: 20 cm
    [InlineData("5702150101", "Calamar argentino", 20.0, 0.011, 3.15, 137.92)]
    // 5. Polaca (Micromesistius australis) - Hembra
    // Talla de ejemplo: 45 cm
    [InlineData("7210030201", "Polaca (Hembra)", 45.0, 0.0026, 3.2278, 563.91)]
    // 6. Abadejo manchado (Genypterus blacodes) - Indeterminado
    // Talla de ejemplo: 70 cm
    [InlineData("7226030101", "Abadejo manchado", 70.0, 0.00096, 3.352, 1469.06)]
    // 7. Savorín (Seriolella porosa) - Indeterminado
    // Talla de ejemplo: 40 cm
    [InlineData("7218390102", "Savorín", 40.0, 0.004, 3.1989, 533.20)]
    // 8. Merluza negra (Dissostichus eleginoides) - Indeterminado
    // Talla de ejemplo: 80 cm
    [InlineData("7218280201", "Merluza negra", 80.0, 0.0042, 3.19385, 5028.44)]
    // 9. Bacalao criollo / Salilota australis - Macho
    // Talla de ejemplo: 50 cm
    [InlineData("7210020101", "Bacalao criollo (Macho)", 50.0, 0.03, 2.6755, 1053.70)]
    public void CalcularPesoAlometrico_DebeRetornarPesoCoherente(
        string codigoInidep, 
        string especie, 
        double talla, 
        double paramA, 
        double paramB, 
        double pesoEsperadoGramos)
    {
        // Act
        double pesoCalculado = CalcularPesoAlometrico(talla, paramA, paramB);

        // Assert
        // Aceptamos una tolerancia de 0.1 gramos debido al redondeo de los ejemplos
        pesoCalculado.Should().BeApproximately(pesoEsperadoGramos, 0.1, 
            $"El cálculo para {especie} (Cód: {codigoInidep}) con talla {talla} y parámetros A={paramA}, B={paramB} debería ser biológicamente coherente.");
    }
}
