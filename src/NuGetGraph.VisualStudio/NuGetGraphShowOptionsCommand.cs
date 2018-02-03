using System;
using System.ComponentModel.Design;
using System.Threading;

namespace NuGetGraph.VisualStudio
{
    internal sealed class NuGetGraphShowOptionsCommand
    {
        public static void AttachTo(NuGetGraphPackage package)
        {
            NuGetGraphCommands.Register(new NuGetGraphShowOptionsCommand(package));
        }

        private NuGetGraphShowOptionsCommand(NuGetGraphPackage package)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));
            var menuItem = new MenuCommand(Execute, new CommandID(NuGetGraphCommands.CommandSet, commandID: 256));
            package.GetRequiredService<IMenuCommandService>().AddCommand(menuItem);
        }

        private NuGetGraphPackage Package { get; }

        private void Execute(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ => Package.ShowOptions());
        }
    }
}
