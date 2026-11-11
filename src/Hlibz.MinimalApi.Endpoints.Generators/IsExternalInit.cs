// netstandard2.0 lacks the type records' init accessors need; the compiler only looks it up by
// name.
namespace System.Runtime.CompilerServices;

/// <summary>
/// Lets records compile for netstandard2.0.
/// </summary>
internal static class IsExternalInit;
