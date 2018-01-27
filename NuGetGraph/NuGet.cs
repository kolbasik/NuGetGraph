using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet;

namespace NugetGraph
{
    /// <summary>
    /// https://stackoverflow.com/questions/6653715/view-nuget-package-dependency-hierarchy
    /// </summary>
    public static class NuGet
    {
        public sealed class GraphOptions
        {
            public GraphOptions(string path)
            {
                Path = path;
            }

            public string Path { get; }
            public List<string> ExcludeConfigs { get; } = new List<string>();
            public List<string> ExcludeLibraries { get; } = new List<string>();
            public bool ExcludeMicrosoftLibraries { get; set; }
            public bool UseNamespaces { get; set; }
        }

        public static Graph GetGraph(GraphOptions options)
        {
            var index = 0;
            var graph = new Graph();
            var sharedPackageRepository = new SharedPackageRepository(Path.Combine(options.Path, "packages"));
            foreach (var config in Directory.GetFiles(options.Path, "packages.config", SearchOption.AllDirectories).Where(x => !options.ExcludeConfigs.Any(e => x.ToUpperInvariant().Contains(e))))
            {
                var ns = options.UseNamespaces ? ++index + ":" : null;
                var node = graph.GetOrAdd(ns + Path.GetFileName(Path.GetDirectoryName(config)));
                if (graph.Root.Append(node))
                {
                    var projectPackageRepository = new PackageReferenceRepository(config, sharedPackageRepository);
                    var packages = projectPackageRepository.GetPackages();
                    Walk(projectPackageRepository, packages, node, ns);
                }
            }
            return graph;

            void Walk(IPackageRepository packageRepository, IEnumerable<IPackage> packages, Node root, string ns)
            {
                foreach (IPackage package in packages.Where(x => !options.ExcludeLibraries.Any(e => x.Id.ToUpperInvariant().Contains(e))))
                {
                    if (options.ExcludeMicrosoftLibraries && (package.Id.StartsWith("Microsoft.") || package.Id.StartsWith("System.") || package.Id.StartsWith("NETStandard.")))
                    {
                        continue;
                    }
                    var node = graph.GetOrAdd(ns + package.Id);
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
            public Node Root { get; } = new Node("ROOT");
            private ConcurrentDictionary<string, Node> Nodes { get; } = new ConcurrentDictionary<string, Node>();
            public IEnumerable<Node> GetNodes() => Nodes.Values;
            public Node GetOrAdd(string id) => Nodes.GetOrAdd(id, x => new Node(x));
            public override string ToString() => $"Graph:{Nodes.Count}";
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
                node.Ancestors.Add(this);
                return Descendants.Add(node);
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