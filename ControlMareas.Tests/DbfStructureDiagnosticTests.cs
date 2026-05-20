using System.IO;
using System.Text;
using System.Reflection;
using DotNetDBF;
using Xunit;
using Xunit.Abstractions;

namespace ControlMareas.Tests;

public class DbfStructureDiagnosticTests
{
    private readonly ITestOutputHelper _output;
    private const string ExamplesPath = @"d:\Desarrollo\_INIDEP\OBS\OBS-Arrastre-2026\ejemplos\Entrada";
    private const string OutputPath = @"C:\Users\danie\.gemini\antigravity\brain\e4cf11d5-e50b-4223-bc9e-deeb185ac626\dbf_structures.txt";

    public DbfStructureDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public void DumpExampleDbfStructures()
    {
        using var logWriter = new StreamWriter(OutputPath, false, Encoding.UTF8);
        var files = Directory.GetFiles(ExamplesPath, "*.DBF");
        foreach (var file in files)
        {
            logWriter.WriteLine($"--- STRUCTURE OF {Path.GetFileName(file)} ---");
            using var stream = File.OpenRead(file);
            var reader = new DBFReader(stream);
            
            for (int i = 0; i < reader.Fields.Length; i++)
            {
                var field = reader.Fields[i];
                var props = field.GetType().GetProperties();
                var sb = new StringBuilder();
                sb.Append($"Field {i}: ");
                foreach(var prop in props)
                {
                    try { sb.Append($"{prop.Name}={prop.GetValue(field)}, "); } catch {}
                }
                logWriter.WriteLine(sb.ToString());
            }
            logWriter.WriteLine("");
        }
    }
}
