using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;

namespace NuGetGraph.CLI
{
    public static class Program
    {
        private static void Main(params string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = Path.GetFileName(Environment.GetCommandLineArgs()[0]),
                FullName = "The NuGet Graph CLI",
            };
            app.HelpOption("-?|-h|--help");

            app.Command("version", command =>
            {
                command.Description = "show version information";
                command.HelpOption("-?|-h|--help");

                command.OnExecute(() =>
                {
                    var assembly = typeof(Program).Assembly;
                    var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                                  ?? assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
                    Console.WriteLine(version);
                    return 0;
                });
            });

            app.Command("graph", command =>
            {
                command.Description = "build a NuGet graph";
                command.HelpOption("-?|-h|--help");

                var options = NuGetGraphProcess.Arguments.Default();

                var source = command.Argument("[path]", "source path");
                var excludeConfigs = command.Option("-ec|--exclude-configs", "exclude configs e.g. '.Test'", CommandOptionType.MultipleValue);
                var excludeLibraries = command.Option("-el|--exclude-libraries", "exclude libraries e.g. 'log4net'", CommandOptionType.MultipleValue);
                var includeLibraries = command.Option("-il|--include-libraries", "include libraries e.g. system, standard, microsoft.", CommandOptionType.MultipleValue);

                var useNamespaces = command.Option("-un|--use-namespaces", "to separate libraries per projects", CommandOptionType.NoValue);
                var useVersions = command.Option("-uv|--use-versions", "to include the versions of libraries", CommandOptionType.NoValue);
                var simplify = command.Option("-us|--simplify", "to simplify the nuget graph", CommandOptionType.NoValue);
                var useStyles = command.Option("-ut|--use-styles", "to use default dgml styles", CommandOptionType.NoValue);
                var useFormatting = command.Option("-uf|--use-formatting", "to use dgml formatting", CommandOptionType.NoValue);

                var outputType = command.Option("-o|--output", $"output type e.g. {string.Join(", ", Enum.GetNames(typeof(NuGetGraphProcess.Arguments.OutputTypes)).Skip(1))}, default: {options.Output.Type:F}", CommandOptionType.MultipleValue);
                var outputFilePath = command.Option("-f|--output-file-path", "output file path e.g. NuGet.dgml, default: temp file", CommandOptionType.SingleValue);
                var outputOpenFile = command.Option("-l|--output-open-file", "open the output file", CommandOptionType.NoValue);

                command.OnExecute(() =>
                {
                    options.Input.Path = source.Value ?? ".";
                    options.Input.ExcludeConfigs.AddRange(excludeConfigs.Values);
                    options.Input.ExcludeLibraries.AddRange(excludeLibraries.Values);
                    options.Input.ExcludeSystemLibraries = !includeLibraries.Values.Contains("system");
                    options.Input.ExcludeStandardLibraries = !includeLibraries.Values.Contains("standard");
                    options.Input.ExcludeMicrosoftLibraries = !includeLibraries.Values.Contains("microsoft");

                    options.Graph.UseNamespaces = GetBoolean(useNamespaces.Value());
                    options.Graph.UseVersions = GetBoolean(useVersions.Value());
                    options.Graph.Simplify = GetBoolean(simplify.Value());
                    options.Graph.UseStyles = GetBoolean(useStyles.Value());
                    options.Graph.UseFormatting = GetBoolean(useFormatting.Value());

                    options.Output.Type = GetFlags<NuGetGraphProcess.Arguments.OutputTypes>(outputType.Values);
                    options.Output.FilePath = outputFilePath.Value() ?? Path.GetTempFileName() + ".dgml";
                    options.Output.OpenFile = outputOpenFile.HasValue();

                    NuGetGraphProcess.Start(options);
                    return 0;
                });
            });

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 0;
            });

            app.Execute(args);
        }

        private static bool GetBoolean(string value)
        {
            return string.Equals("on", value, StringComparison.OrdinalIgnoreCase);
        }

        private static TFlags GetFlags<TFlags>(IEnumerable<string> values) where TFlags : struct
        {
            return (TFlags) (object) values
                .SelectMany(x => x.Split(',').Select(z => z.Trim()))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(v => Enum.Parse(typeof(TFlags), v, true))
                .Cast<int>().Aggregate(0, (r, t) => r | t);
        }
    }
}
