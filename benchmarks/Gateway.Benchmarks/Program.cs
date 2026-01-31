using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Gateway.Benchmarks.Benchmarks;

namespace Gateway.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure exporters
        var config = DefaultConfig.Instance
            .WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(50))
            .AddExporter(MarkdownExporter.GitHub)
            .AddExporter(CsvExporter.Default)
            .AddExporter(HtmlExporter.Default);

        if (args.Length > 0 && args[0] == "--filter")
        {
            // Run specific benchmark class
            var filter = args.Length > 1 ? args[1] : "*";
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                .Run(args, config);
        }
        else if (args.Length > 0 && args[0] == "--list")
        {
            // List available benchmarks
            Console.WriteLine("Available benchmarks:");
            Console.WriteLine("  - ObjectMapperBenchmarks");
            Console.WriteLine("  - FilterBuilderBenchmarks");
            Console.WriteLine("  - PaginationBenchmarks");
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  dotnet run -c Release                    # Run all benchmarks");
            Console.WriteLine("  dotnet run -c Release -- --filter *      # Run all with filter");
            Console.WriteLine("  dotnet run -c Release -- --filter Object # Run ObjectMapper benchmarks");
        }
        else
        {
            // Run all benchmarks
            Console.WriteLine("Gateway.Core Performance Benchmarks");
            Console.WriteLine("====================================");
            Console.WriteLine();

            var summaries = new List<Summary>();

            Console.WriteLine("Running ObjectMapper Benchmarks...");
            summaries.Add(BenchmarkRunner.Run<ObjectMapperBenchmarks>(config));

            Console.WriteLine("\nRunning FilterBuilder Benchmarks...");
            summaries.Add(BenchmarkRunner.Run<FilterBuilderBenchmarks>(config));

            Console.WriteLine("\nRunning Pagination Benchmarks...");
            summaries.Add(BenchmarkRunner.Run<PaginationBenchmarks>(config));

            Console.WriteLine("\n====================================");
            Console.WriteLine("All benchmarks completed!");
            Console.WriteLine($"Results saved to: {Directory.GetCurrentDirectory()}\\BenchmarkDotNet.Artifacts");
        }
    }
}
