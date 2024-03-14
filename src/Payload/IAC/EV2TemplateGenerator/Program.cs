using Juno.Payload.EV2;

using Microsoft.EV2.Templates.Fluent;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("A path argument is missing");
        }

        if (string.IsNullOrWhiteSpace(args[0]))
        {
            Console.WriteLine($"Invalid path specified:{args[0]}");
        }

        EV2TemplateGenerator exporter = new(new EV2TemplateGeneratorConfiguration(args[0]));
        exporter.GenerateAsync().GetAwaiter().GetResult();
    }
}
