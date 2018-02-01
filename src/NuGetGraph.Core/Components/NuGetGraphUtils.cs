using System;
using System.Collections.Generic;
using System.Linq;
using NuGet;

namespace NuGetGraph.Components
{
    public static class NuGetGraphUtils
    {
        public static void Simplify(this NuGetGraph.Graph graph)
        {
            foreach (var node in graph.GetNodes())
            {
                foreach (var root in node.WalkUp().Where(w => w.Item1 > 0).Select(w => w.Item2).Where(a => a.Descendants.Contains(node)).ToList())
                {
                    root.Descendants.Remove(node);
                }
            }
        }

        private static IEnumerable<Tuple<int, NuGetGraph.Node>> WalkUp(this NuGetGraph.Node leaf)
        {
            return Walk(leaf, node => node.Ancestors);
        }

        private static IEnumerable<Tuple<int, NuGetGraph.Node>> WalkDown(this NuGetGraph.Node leaf)
        {
            return Walk(leaf, node => node.Descendants);
        }

        private static IEnumerable<Tuple<int, NuGetGraph.Node>> Walk(NuGetGraph.Node leaf, Func<NuGetGraph.Node, ICollection<NuGetGraph.Node>> evaluate)
        {
            var level = 0;
            var nodes = evaluate(leaf);
            while (nodes.Count > 0)
            {
                var temp = new HashSet<NuGetGraph.Node>();
                foreach (var node in nodes)
                {
                    yield return Tuple.Create(level, node);
                    temp.AddRange(evaluate(node));
                }
                ++level;
                nodes = temp;
            }
        }
    }
}