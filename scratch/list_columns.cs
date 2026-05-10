using System;
using System.IO;
using System.Text;
using DbfDataReader;

public class ColumnLister
{
    public static void Main(string[] args)
    {
        string path = @"c:\Users\danieldt\Documents\Desarrollo\.net\WPF\OBS-Arrastre-2026\OBSArrastre2026.App\Data\Import\Raw\m0326.dbf";
        if (!File.Exists(path)) { Console.WriteLine("File not found"); return; }

        using var reader = new DbfDataReader.DbfDataReader(path);
        Console.WriteLine("Columns in m0326.dbf:");
        foreach (var column in reader.Columns)
        {
            Console.WriteLine($"- {column.Name} ({column.ColumnType})");
        }
    }
}
