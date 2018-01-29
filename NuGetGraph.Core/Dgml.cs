using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NuGetGraph.Core
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/visualstudio/modeling/customize-code-maps-by-editing-the-dgml-files
    /// </summary>
    public static class Dgml
    {
        public static string ToDgml(this Graph graph, SaveOptions options = SaveOptions.None)
        {
            XNamespace dgml = "http://schemas.microsoft.com/vs/2009/dgml";
            var xNodes = new XElement(dgml + "Nodes");
            foreach (var node in graph.Nodes.Where(x => x.Id != null))
            {
                xNodes.Add(new XElement(dgml + "Node",
                    new XAttribute("Id", node.Id),
                    new XAttribute("Type", node.Type)));
            }
            var xLinks = new XElement(dgml + "Links");
            foreach (var link in graph.Links.Where(x => x.Source != null && x.Target != null))
            {
                xLinks.Add(new XElement(dgml + "Link",
                    new XAttribute("Source", link.Source),
                    new XAttribute("Target", link.Target)));
            }
            var xStyles = new XElement(dgml + "Styles");
            foreach (var style in graph.Styles)
            {
                var xStyle = new XElement(dgml + "Style",
                    new XAttribute("TargetType", style.TargetType),
                    new XElement(dgml + "Condition",
                        new XAttribute("Expression", style.Condition.Expression)));
                foreach (var setter in style.Setters)
                {
                    xStyle.Add(new XElement(dgml + "Setter",
                        new XAttribute("Property", setter.Property),
                        new XAttribute("Value", setter.Value)));
                }
                xStyles.Add(xStyle);
            }
            var xGraph = new XElement(dgml + "DirectedGraph", xNodes, xLinks, xStyles);
            var xDocument = new XDocument(xGraph);
            return xDocument.ToString(options);
        }

        public sealed class Graph
        {
            public HashSet<Node> Nodes { get; } = new HashSet<Node>();
            public List<Link> Links { get; } = new List<Link>();
            public List<Style> Styles { get; } = new List<Style>();
        }

        public sealed class Node : IEquatable<Node>
        {
            public Node(string id, string type)
            {
                Id = id;
                Type = type;
            }

            public string Id { get; }
            public string Type { get; }

            public override string ToString() => $"{Type}:{Id}";

            public bool Equals(Node other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Id, other.Id) && string.Equals(Type, other.Type);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Node node && Equals(node);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Id != null ? Id.GetHashCode() : 0) * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                }
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

        public sealed class Link
        {
            public Link(string source, string target)
            {
                Source = source;
                Target = target;
            }

            public string Source { get; }
            public string Target { get; }

            public override string ToString() => $"{Source} links to {Target}";
        }

        public sealed class Style
        {
            public Style(StyleTargetType targetType = StyleTargetType.Node)
            {
                TargetType = targetType;
            }

            public StyleTargetType TargetType { get; }
            public StyleCondition Condition { get; } = new StyleCondition();
            public List<StyleSetter> Setters { get; } = new List<StyleSetter>();

            public override string ToString()
            {
                return $"{TargetType}: if {Condition.Expression} then set {string.Join(" and ", Setters.Select(x => $"'{x.Property}' to '{x.Value}'"))}";
            }
        }

        public enum StyleTargetType
        {
            Node,
            Link,
            Graph
        }

        public class StyleCondition
        {
            public string Expression { get; set; } = bool.TrueString;
        }

        public class StyleSetter
        {
            public StyleSetter(string property, string value)
            {
                Property = property ?? throw new ArgumentNullException(nameof(property));
                Value = value ?? throw new ArgumentNullException(nameof(value));
            }

            public string Property { get; }
            public string Value { get; }
        }
    }
}