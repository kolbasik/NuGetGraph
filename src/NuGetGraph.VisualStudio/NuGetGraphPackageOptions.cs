using Microsoft.VisualStudio.Shell;

namespace NuGetGraph.VisualStudio
{
    public sealed class NuGetGraphPackageOptions : DialogPage
    {
        public const string Category = "NuGet Graph";
        public const string PageName = "Settings";

        public bool ExcludeSystemLibraries { get; set; } = true;
        public bool ExcludeStandardLibraries { get; set; } = true;
        public bool ExcludeMicrosoftLibraries { get; set; } = false;

        public bool UseNamespaces { get; set; } = false;
        public bool UseVersions { get; set; } = true;
        public bool Simplify { get; set; } = true;
        public bool UseStyles { get; set; } = true;
    }
}