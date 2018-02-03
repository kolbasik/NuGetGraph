using System;
using System.Collections.Generic;

namespace NuGetGraph.VisualStudio
{
    internal static class NuGetGraphCommands
    {
        public static readonly Guid CommandSet = new Guid("7dcdc588-85f7-44ee-94d5-c6d65445f314");
        private static readonly Dictionary<Type, object> Commands = new Dictionary<Type, object>();

        public static void Register(object command)
        {
            Commands[command.GetType()] = command;
        }
    }
}