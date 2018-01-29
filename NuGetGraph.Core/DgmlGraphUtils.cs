using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGetGraph.Core
{
    public static class DgmlGraphUtils
    {
        public static Dgml.Graph ToDgmlGraph(this NuGets.Graph nugetGraph)
        {
            var dgmlGraph = new Dgml.Graph();
            var solution = new Dgml.Node("Solution", "solution");
            dgmlGraph.Nodes.Add(solution);
            foreach (var project in nugetGraph.Root.Descendants)
            {
                dgmlGraph.Nodes.Add(new Dgml.Node(project.Id, "project"));
                dgmlGraph.Links.Add(new Dgml.Link(solution.Id, project.Id));
                var tuples = project.Descendants.Select(desc => Tuple.Create(project, desc)).ToList();
                while (tuples.Count > 0)
                {
                    var temp = new List<Tuple<NuGets.Node, NuGets.Node>>();
                    foreach (var tuple in tuples)
                    {
                        var level1 = tuple.Item1;
                        var level2 = tuple.Item2;
                        dgmlGraph.Nodes.Add(new Dgml.Node(level2.Id, "nuget"));
                        dgmlGraph.Links.Add(new Dgml.Link(level1.Id, level2.Id));
                        temp.AddRange(level2.Descendants.Select(desc => Tuple.Create(level2, desc)));
                    }
                    tuples = temp;
                }
            }
            return dgmlGraph;
        }

        public static Dgml.Graph UseStyles(this Dgml.Graph dgmlGraph)
        {
            dgmlGraph.Styles.Add(new Dgml.Style
            {
                Condition = { Expression = "Type='solution'" },
                Setters = { new Dgml.StyleSetter("Background", "Lime"), new Dgml.StyleSetter("Foreground", "#111") }
            });
            dgmlGraph.Styles.Add(new Dgml.Style
            {
                Condition = { Expression = "Type='project'" },
                Setters = { new Dgml.StyleSetter("Background", "Royalblue"), new Dgml.StyleSetter("Foreground", "#eee") }
            });
            return dgmlGraph;
        }
    }
}