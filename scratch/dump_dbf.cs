using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NDbfReader;

public class DbfDumper
{
    public static void Main(string[] args)
    {
        string path = @"c:\Users\danieldt\Documents\Desarrollo\.net\WPF\ControlMareas\ControlMareas.App\Data\Import\Raw\m0326.dbf";
        if (!File.Exists(path)) { Console.WriteLine("File not found"); return; }

        using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            var table = Table.Open(stream);
            var reader = table.OpenReader(Encoding.GetEncoding(850));
            
            Console.WriteLine("Dumping Abadejo (Genypterus blacodes) records:");
            int count = 0;
            while (reader.Read())
            {
                string especie = reader.GetString("ESPECIE")?.Trim();
                if (especie != null && (especie.Contains("Genypterus") || especie.Contains("blacodes") || especie.Contains("ABADEJO")))
                {
                    decimal codEspec = reader.GetDecimal("CODESPEC");
                    decimal marea = reader.GetDecimal("MAREA");
                    decimal lance = reader.GetDecimal("LANCE");
                    Console.WriteLine($"Marea: {marea}, Lance: {lance}, Especie: {especie}, CodEspec: {codEspec}");
                    count++;
                    if (count > 10) break;
                }
            }
        }
    }
}
