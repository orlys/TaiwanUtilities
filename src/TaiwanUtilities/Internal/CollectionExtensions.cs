namespace System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#if NETSTANDARD2_0 || NETFRAMEWORK
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[StackTraceHidden]
internal static class CollectionExtensions
{
    public static bool TryPop<T>(this Stack<T> stack, out T value)
    {
        if (stack is { Count: > 0 })
        {
            value = stack.Pop();
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
    public static bool TryDequeue<T>(this Queue<T> queue, out T value)
    {
        if (queue is { Count: > 0 })
        {
            value = queue.Dequeue();
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
}
#endif