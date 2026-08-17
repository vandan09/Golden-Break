using System.Runtime.CompilerServices;

// Lets EditMode tests call internal test seams (e.g. SaveManager.LoadFrom,
// HapticManager.GetPatternMilliseconds) without widening them to public
// just to make them reachable from a separate test assembly.
[assembly: InternalsVisibleTo("GoldenBreak.Tests.EditMode")]
