using System;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGetGraph.VisualStudio
{
    [Guid(PackageGuidString)]
    [PackageRegistration(UseManagedResourcesOnly = true)] // Info on this package for Help/About
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideBindingPath]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    [ProvideOptionPage(typeof(NuGetGraphPackageOptions), NuGetGraphPackageOptions.Category, NuGetGraphPackageOptions.PageName, 0, 0, false)]
    public sealed class NuGetGraphPackage : Package
    {
        private const string PackageGuidString = "cb052230-74ff-4614-bf38-c72ab154c17b";

        protected override void Initialize()
        {
            NuGetGraphShowOptionsCommand.AttachTo(this);
            NuGetGraphShowGraphCommand.AttachTo(this);
            base.Initialize();
        }

        public DTE2 DTE => GetGlobalService(typeof(DTE)) as DTE2;
        public Solution Solution => DTE?.Solution;

        public TService GetRequiredService<TService>() => (TService) GetService(typeof(TService));
        public TOptions GetOptions<TOptions>() where TOptions : DialogPage => (TOptions) GetDialogPage(typeof(TOptions));

        public void ShowOptions()
        {
            if (Zombied) return;

            ShowOptionPage(typeof(NuGetGraphPackageOptions));
        }

        public void ShowGraph()
        {
            if (Zombied) return;

            var solutionFileName = Solution?.FileName;
            if (string.IsNullOrEmpty(solutionFileName))
            {
                ShowMessageBox("This extension requires an open solution.", OLEMSGICON.OLEMSGICON_WARNING);
            }
            else
            {
                try
                {
                    var args = NuGetGraphProcess.Arguments.Default(a =>
                    {
                        a.Input.Path = Path.GetDirectoryName(solutionFileName);
                        a.Output.Type = NuGetGraphProcess.Arguments.OutputTypes.File;
                        a.Output.FilePath = Path.GetTempFileName() + ".dgml";
                        a.Output.OpenFile = false;
                    });

                    var options = GetOptions<NuGetGraphPackageOptions>();
                    if (options != null)
                    {
                        args.Input.ExcludeSystemLibraries = options.ExcludeSystemLibraries;
                        args.Input.ExcludeStandardLibraries = options.ExcludeStandardLibraries;
                        args.Input.ExcludeMicrosoftLibraries = options.ExcludeMicrosoftLibraries;
                        args.Graph.UseNamespaces = options.UseNamespaces;
                        args.Graph.UseVersions = options.UseVersions;
                        args.Graph.UseStyles = options.UseStyles;
                        args.Graph.Simplify = options.Simplify;
                    }

                    NuGetGraphProcess.Start(args);
                    DTE.ItemOperations.OpenFile(args.Output.FilePath);
                }
                catch (Exception ex)
                {
                    ShowMessageBox(ex.Message, OLEMSGICON.OLEMSGICON_CRITICAL);
                }
            }
        }

        private void ShowMessageBox(string message, OLEMSGICON icon = OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON button = OLEMSGBUTTON.OLEMSGBUTTON_OK)
        {
            VsShellUtilities.ShowMessageBox(this, message, nameof(NuGetGraphShowGraphCommand), icon, button, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
