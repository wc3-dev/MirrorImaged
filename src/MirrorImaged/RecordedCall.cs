namespace MirrorImaged;

/// <summary>
/// A call of a Recorded native: the step it was made at, the native's name, and its arguments, as Lua's
/// <c>sim:recorded()</c> gives them: a number, a string or a boolean as the native took it, a handle as its id (an
/// <see cref="int"/>), a function as the string <c>"function"</c>, and null for nil, a missing argument and a destroyed
/// handle. The native's parameters in <c>common.j</c> say which argument is which.
/// </summary>
public sealed record RecordedCall(long Step, string Native, IReadOnlyList<object?> Arguments)
{
    // An argument as the native took it, as a test reads it: the handle object and the delegate stay inside
    internal static object? Argument(object? value) => value switch
    {
        Natives.handle h => h.Id,
        Delegate => "function",
        _ => value,
    };
}
