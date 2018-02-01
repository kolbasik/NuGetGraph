using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using NuGetGraph.Components;

namespace NuGetGraph
{
    public static class NuGetGraphProcess
    {
        public sealed class Arguments
        {
            public static Arguments Default(Action<Arguments> configure = null)
            {
                var arguments = new Arguments
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
                configure?.Invoke(arguments);
                return arguments;
            }

            public InputOptions Input { get; } = new InputOptions();
            public GraphOptions Graph { get; } = new GraphOptions();
            public OutputOptions Output { get; } = new OutputOptions();

            public Components.NuGetGraph.Options ToNuGetGraphOptions()
            {
                var options = new Components.NuGetGraph.Options(Input.Path)
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

        public static void Start(Arguments arguments)
        {
            var nugetGraph = Components.NuGetGraph.Build(arguments.ToNuGetGraphOptions());
            if (arguments.Graph.Simplify)
            {
                nugetGraph.Simplify();
            }

            var dgmlGraph = nugetGraph.ToDgmlGraph();
            if (arguments.Graph.UseStyles)
            {
                dgmlGraph.UseStyles();
            }

            var dgml = dgmlGraph.ToString(arguments.ToDgmlOptions());

            if (arguments.Output.Type.HasFlag(Arguments.OutputTypes.Console))
            {
                Console.WriteLine(dgml);
            }
            if (arguments.Output.Type.HasFlag(Arguments.OutputTypes.File))
            {
                File.WriteAllText(arguments.Output.FilePath, dgml);
                if (arguments.Output.OpenFile)
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", arguments.Output.FilePath));
                }
            }
        }
    }
}
