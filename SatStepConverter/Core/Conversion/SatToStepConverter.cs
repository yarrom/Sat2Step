using SatStepConverter.Core.Sat;
using SatStepConverter.Core.Step;

namespace SatStepConverter.Core.Conversion;

public class SatToStepConverter
{
    private readonly SatParser _parser;
    private readonly StepWriter _writer;

    public SatToStepConverter()
    {
        _parser = new SatParser();
        _writer = new StepWriter();
    }

    public void Convert(string inputPath, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("Input path must be provided.", nameof(inputPath));
        }

        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input SAT file not found.", inputPath);
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path must be provided.", nameof(outputPath));
        }

        using var inputStream = File.OpenRead(inputPath);
        var document = _parser.Parse(inputStream);

        using var outputStream = File.Create(outputPath);
        _writer.Write(document, outputStream);
    }
}

