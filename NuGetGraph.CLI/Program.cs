using System;
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

                command.OnExecute(() =>
                {
                    var options = NuGetGraphProcess.Arguments.Default(o =>
                    {
                        o.Input.Path = args.ElementAtOrDefault(0) ?? ".";
                        o.Input.ExcludeConfigs.Add(".TEST");
                        o.Output.Type = NuGetGraphProcess.Arguments.OutputTypes.File;
                        o.Output.OpenFile = true;
                    });
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
    }
}
