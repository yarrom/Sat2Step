
namespace SimpleSatParser
{

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: SimpleSatParser <input.sat> [out.step]");
                return 1;
            }

            var input = args[0];
            var output = args.Length >= 2 ? args[1] : Path.ChangeExtension(input, ".step");

            if (!File.Exists(input))
            {
                Console.WriteLine($"Input file not found: {input}");
                return 2;
            }

            var text = File.ReadAllText(input);
            var parser = new SatParser();
            var model = parser.Parse(text);

            Console.WriteLine($"Parsed entities: {model.EntitiesCount()}");
            Console.WriteLine("Unresolved references (if any) will be printed below:");
            foreach (var pid in model.Reg.PendingIds())
                Console.WriteLine($"  Pending id {pid}");

            var exporter = new StepExporter();
            exporter.Export(model, output);

            Console.WriteLine($"STEP written to {output}");
            return 0;
        }
    }

}