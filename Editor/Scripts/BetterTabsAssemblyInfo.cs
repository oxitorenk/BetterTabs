using System.Runtime.CompilerServices;

// Lets the separate test assembly call internal BetterTabs code directly without making it public or using reflection.
[assembly: InternalsVisibleTo("BetterTabs.Editor.Tests")]
