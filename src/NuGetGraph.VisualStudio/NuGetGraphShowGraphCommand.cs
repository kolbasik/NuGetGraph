using System;
using System.ComponentModel.Design;
using System.Threading;

namespace NuGetGraph.VisualStudio
{
    internal sealed class NuGetGraphShowGraphCommand
    {
        public static void AttachTo(NuGetGraphPackage package)
        {
            NuGetGraphCommands.Register(new NuGetGraphShowGraphCommand(package));
        }

        private NuGetGraphShowGraphCommand(NuGetGraphPackage package)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));
            var menuItem = new MenuCommand(Execute, new CommandID(NuGetGraphCommands.CommandSet, commandID: 255));
            package.GetRequiredService<IMenuCommandService>().AddCommand(menuItem);
        }

        private NuGetGraphPackage Package { get; }

        private void Execute(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ => Package.ShowGraph());
        }
    }
}
