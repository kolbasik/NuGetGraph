using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGetGraph.VisualStudio
{
    internal sealed class NuGetGraphCommand
    {
        public static void AttachTo(Package package)
        {
            Instance = new NuGetGraphCommand(package);
        }

        private static NuGetGraphCommand Instance { get; set; }

        private NuGetGraphCommand(Package package)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));
            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var commandSet = new Guid("7dcdc588-85f7-44ee-94d5-c6d65445f314");
                var commandId = 4129;

                var menuItem = new MenuCommand(Execute, new CommandID(commandSet, commandId));
                commandService.AddCommand(menuItem);
            }
        }

        private Package Package { get; }
        private IServiceProvider ServiceProvider => Package;

        private void Execute(object sender, EventArgs e)
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            var sln = dte?.Solution;
            if (dte == null || sln == null)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (string.IsNullOrEmpty(sln.FileName))
                {
                    ShowMessageBox("This extension requires an open solution.", OLEMSGICON.OLEMSGICON_WARNING);
                }
                else
                {
                    try
                    {
                        var args = NuGetGraphProcess.Arguments.Default(a =>
                        {
                            a.Input.Path = Path.GetDirectoryName(sln.FileName);
                            a.Output.Type = NuGetGraphProcess.Arguments.OutputTypes.File;
                            a.Output.FilePath = Path.GetTempFileName() + ".dgml";
                            a.Output.OpenFile = false;
                        });
                        NuGetGraphProcess.Start(args);
                        dte.ItemOperations.OpenFile(args.Output.FilePath);
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox(ex.Message, OLEMSGICON.OLEMSGICON_CRITICAL);
                    }
                }
            });
        }

        private void ShowMessageBox(string message, OLEMSGICON icon = OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON button = OLEMSGBUTTON.OLEMSGBUTTON_OK)
        {
            VsShellUtilities.ShowMessageBox(ServiceProvider, message, nameof(NuGetGraphCommand), icon, button, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
