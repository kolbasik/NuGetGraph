using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;
using NuGetGraph.Core;

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

                command.OnExecute(() =>
                {
                    var cli = Options.Default(o =>
                    {
                        o.Input.Path = args.ElementAtOrDefault(0) ?? ".";
                        o.Input.ExcludeConfigs.Add(".TEST");
                        o.Output.Type = Options.OutputTypes.File;
                        o.Output.OpenFile = true;
                    });

                    var nugetGraph = NuGets.GetGraph(cli.ToNuGetGraphOptions());
                    if (cli.Graph.Simplify)
                    {
                        nugetGraph.Simplify();
                    }

                    var dgmlGraph = nugetGraph.ToDgmlGraph();
                    if (cli.Graph.UseStyles)
                    {
                        dgmlGraph.UseStyles();
                    }

                    var dgml = dgmlGraph.ToDgml(cli.ToDgmlOptions());
                    if (cli.Output.Type.HasFlag(Options.OutputTypes.Console))
                    {
                        Console.WriteLine(dgml);
                    }
                    if (cli.Output.Type.HasFlag(Options.OutputTypes.File))
                    {
                        File.WriteAllText(cli.Output.FilePath, dgml);
                        if (cli.Output.OpenFile)
                        {
                            Process.Start(new ProcessStartInfo("explorer.exe", cli.Output.FilePath));
                        }
                    }
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

        private sealed class Options
        {
            public static Options Default(Action<Options> configure = null)
            {
                var options = new Options
                {
                    Input =
                    {
                        Path = ".",
                        ExcludeMicrosoftLibraries = true
                    },
                    Graph =
                    {
                        UseNamespaces = false,
                        UseVersions = true,
                        Simplify = true,
                        UseStyles = true,
                        UseFormatting = false,
                    },
                    Output =
                    {
                        Type = OutputTypes.Console,
                        FilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dgml"),
                        OpenFile = false
                    }
                };
                configure?.Invoke(options);
                return options;
            }

            public InputOptions Input { get; } = new InputOptions();
            public GraphOptions Graph { get; } = new GraphOptions();
            public OutputOptions Output { get; } = new OutputOptions();

            public NuGets.GraphOptions ToNuGetGraphOptions()
            {
                var options = new NuGets.GraphOptions(Input.Path)
                {
                    ExcludeMicrosoftLibraries = Input.ExcludeMicrosoftLibraries,
                    UseNamespaces = Graph.UseNamespaces,
                    UseVersions = Graph.UseVersions
                };
                options.ExcludeConfigs.AddRange(Input.ExcludeConfigs);
                options.ExcludeLibraries.AddRange(Input.ExcludeLibraries);
                return options;
            }

            public SaveOptions ToDgmlOptions()
            {
                return Graph.UseFormatting ? SaveOptions.None : SaveOptions.DisableFormatting;
            }

            public sealed class InputOptions
            {
                public string Path { get; set; }
                public List<string> ExcludeConfigs { get; } = new List<string>();
                public List<string> ExcludeLibraries { get; } = new List<string>();
                public bool ExcludeMicrosoftLibraries { get; set; }
            }

            public sealed class GraphOptions
            {
                public bool UseNamespaces { get; set; }
                public bool UseVersions { get; set; }
                public bool Simplify { get; set; }
                public bool UseStyles { get; set; }
                public bool UseFormatting { get; set; }
            }

            public sealed class OutputOptions
            {
                public OutputTypes Type { get; set; }
                public string FilePath { get; set; }
                public bool OpenFile { get; set; }
            }

            [Flags]
            public enum OutputTypes
            {
                None = 0,
                File = 1,
                Console = 2
            }
        }
    }
}
