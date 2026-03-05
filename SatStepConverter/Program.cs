using SatStepConverter.Core.Conversion;

namespace SatStepConverter;

internal class Program
{
    private static int Main(string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            PrintUsage();
            return 1;
        }

        var inputPath = args[0];
        var outputPath = args.Length == 2
            ? args[1]
            : System.IO.Path.ChangeExtension(inputPath, ".step");

        try
        {
            var converter = new SatToStepConverter();
            converter.Convert(inputPath, outputPath);

            Console.WriteLine($"Converted '{inputPath}' to '{outputPath}'.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("SAT to STEP Converter");
        Console.WriteLine("Usage:");
        Console.WriteLine("  SatStepConverter <input.sat> [output.step]");
        Console.WriteLine();
        Console.WriteLine("If output is omitted, the STEP file will be written next to the input with .step extension.");
    }
}
