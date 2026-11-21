namespace MirrorImaged;

/// <summary>
/// A call of a Recorded native: the step it was made at, the native's name, and its arguments as the native took them
/// (a number, a string, a boolean, a handle, a function, or null for nil and a missing one).
/// </summary>
public sealed record RecordedCall(long Step, string Native, IReadOnlyList<object?> Arguments);
