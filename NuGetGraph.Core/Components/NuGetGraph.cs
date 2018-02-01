using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet;

namespace NuGetGraph.Components
{
    /// <summary>
    /// https://stackoverflow.com/questions/6653715/view-nuget-package-dependency-hierarchy
    /// </summary>
    public static class NuGetGraph
    {
        public sealed class Options
        {
            public Options(string path)
            {
                Path = path;
            }

            public string Path { get; }
            public List<string> ExcludeConfigs { get; } = new List<string>();
            public List<string> ExcludeLibraries { get; } = new List<string>();
            public bool ExcludeMicrosoftLibraries { get; set; }
            public bool UseNamespaces { get; set; }
            public bool UseVersions { get; set; }
        }

        public static Graph Build(Options options)
        {
            var index = 0;
            var graph = new Graph();
            var configs = GetConfigs().Where(x => !options.ExcludeConfigs.Any(e => x.Item1.ToUpperInvariant().Contains(e)));
            foreach (var grouping in configs.GroupBy(x => x.Item2))
            {
                switch (grouping.Key)
                {
                    case "packages":
                        {
                            var sharedPackageRepository = new SharedPackageRepository(Path.Combine(options.Path, "packages"));
                            Scan(grouping, path => new PackageReferenceRepository(path, sharedPackageRepository));
                            break;
                        }
                    case "projects":
                        {
                            var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            var sharedPackageRepository = new DotNetCore.SharedPackageRepository(Path.Combine(userProfilePath, @".nuget\packages"));
                            Scan(grouping, path => new DotNetCore.PackageReferenceRepository(path, sharedPackageRepository));
                            break;
                        }
                }
            }
            return graph;

            IEnumerable<Tuple<string, string, string>> GetConfigs()
            {
                foreach (var project in Directory.GetFiles(options.Path, "*proj", SearchOption.AllDirectories))
                {
                    var directory = Path.GetDirectoryName(project) ?? throw new InvalidOperationException(project);
                    var directoryName = Path.GetFileName(directory);
                    var config = Path.Combine(directory, "packages.config");
                    if (File.Exists(config))
                    {
                        yield return Tuple.Create(directoryName, "packages", config);
                    }
                    else
                    {
                        yield return Tuple.Create(directoryName, "projects", project);
                    }
                }
            }

            void Scan(IEnumerable<Tuple<string, string, string>> grouping, Func<string, IPackageLookup> resolve)
            {
                foreach (var config in grouping)
                {
                    var ns = options.UseNamespaces ? ++index + ":" : null;
                    var node = graph.GetOrAdd(ns + config.Item1);
                    if (graph.Root.Append(node))
                    {
                        var packageRepository = resolve(config.Item3);
                        var packages = packageRepository.GetPackages();
                        Walk(packageRepository, packages, node, ns);
                    }
                }
            }

            void Walk(IPackageRepository packageRepository, IEnumerable<IPackage> packages, Node root, string ns)
            {
                foreach (IPackage package in packages.Where(x => !options.ExcludeLibraries.Any(e => x.Id.ToUpperInvariant().Contains(e))))
                {
                    if (options.ExcludeMicrosoftLibraries && (package.Id.StartsWith("Microsoft.") || package.Id.StartsWith("System.") || package.Id.StartsWith("NETStandard.")))
                    {
                        continue;
                    }
                    var node = graph.GetOrAdd(ns + package.Id + (options.UseVersions ? ":" + package.Version : null));
                    if (root.Append(node))
                    {
                        var dependentPackages = new List<IPackage>();
                        foreach (var dependency in package.DependencySets.SelectMany(x => x.Dependencies))
                        {
                            var dependentPackage = packageRepository.FindPackage(dependency.Id, dependency.VersionSpec, true, true);
                            if (dependentPackage != null)
                            {
                                dependentPackages.Add(dependentPackage);
                            }
                        }
                        Walk(packageRepository, dependentPackages, node, ns);
                    }
                }
            }
        }

        public sealed class Graph
        {
            private readonly ConcurrentDictionary<string, Node> nodes = new ConcurrentDictionary<string, Node>();

            public Node Root { get; } = new Node("ROOT");
            public IEnumerable<Node> GetNodes() => nodes.Values;
            public Node GetOrAdd(string id) => nodes.GetOrAdd(id, x => new Node(x));
            public override string ToString() => $"Graph:{nodes.Count}";
        }

        public sealed class Node : IEquatable<Node>
        {
            public Node(string id)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
            }

            public string Id { get; }
            public HashSet<Node> Ancestors { get; } = new HashSet<Node>();
            public HashSet<Node> Descendants { get; } = new HashSet<Node>();

            public bool Append(Node node)
            {
                return Descendants.Add(node) && node.Ancestors.Add(this);
            }

            public override string ToString()
            {
                return $"Node:{Id}";
            }

            public override int GetHashCode()
            {
                return Id?.GetHashCode() ?? 0;
            }

            public bool Equals(Node other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Id, other.Id);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Node node && Equals(node);
            }

            public static bool operator ==(Node left, Node right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Node left, Node right)
            {
                return !Equals(left, right);
            }
        }
    }
}