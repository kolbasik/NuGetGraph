# NuGet Graph

Alternative Tools:

* https://github.com/brentkrueger/VisualStudio2015PackageVisualizer

Useful Tools:

* [ChrisLovett.DgmlPowerTools2017](https://marketplace.visualstudio.com/items?itemName=ChrisLovett.DgmlPowerTools2017)

Useful links:

* https://github.com/NuGet/Home
* https://stackoverflow.com/questions/6653715/view-nuget-package-dependency-hierarchy
* https://docs.microsoft.com/en-us/visualstudio/modeling/customize-code-maps-by-editing-the-dgml-files
* https://cakebuild.net

## CI/CD

### Build

```powershell
PS> .\build.ps1
```

### Release

```powershell
PS> .\build.ps1 -Target Release -ScriptArgs @("-u=kolbasik","-p=$((New-Object PSCredential "user", $(Read-Host -Prompt "password" -AsSecureString)).GetNetworkCredential().Password)")
```

## Command line interface

The `console` help:

```cmd
c:>NuGetGraph.CLI.exe --help

Usage: NuGetGraph.CLI.exe [options] [command]

Options:
  -?|-h|--help  Show help information

Commands:
  graph    build a NuGet graph
  version  show version information
```

The `graph` command help:

```cmd
c:> NuGetGraph.CLI.exe graph --help

Usage: NuGetGraph.CLI.exe graph [arguments] [options]

Arguments:
  [path]  source path

Options:
  -?|-h|--help             Show help information
  -ec|--exclude-configs    exclude configs e.g. '.Test'
  -el|--exclude-libraries  exclude libraries e.g. 'log4net'
  -il|--include-libraries  include libraries e.g. system, standard, microsoft.
  -un|--use-namespaces     to separate libraries per projects
  -uv|--use-versions       to include the versions of libraries
  -us|--simplify           to simplify the nuget graph
  -ut|--use-styles         to use default dgml styles
  -uf|--use-formatting     to use dgml formatting
  -o|--output              output type e.g. File, Console, default: Console
  -f|--output-file-path    output file path e.g. NuGet.dgml, default: temp file
  -l|--output-open-file    open the output file
```

Example:

```cmd
c:> NuGetGraph.CLI.exe graph "c:\code\src" -ec=".Test" -uv -us -ut -o=file -l
```

## Visual Studio 2017

Visual Studio 2017 is supported right now:

* download the latest version from [releases](./releases)
* or run the `.\build.ps1` script and after run the `.\artifacts\NuGetGraph.VisualStudio.vsix`
* or open the solution and run the NuGetGraph.VisualStudio project

TBD: https://marketplace.visualstudio.com/search?target=VS

## ReSharper

TBD

## Chocolatey

TBD