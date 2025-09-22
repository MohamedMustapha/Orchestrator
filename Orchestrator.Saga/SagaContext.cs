using System;
using System.Collections.Concurrent;

namespace Orchestrator.Saga;

/// <summary>
/// Shared state that travels with a saga execution. The context is a thread-safe bag of values that
/// can be populated by each step and consumed by subsequent steps or compensations.
/// </summary>
public sealed class SagaContext
{
    private readonly ConcurrentDictionary<string, object?> _items = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a value on the context. Existing values will be overwritten.
    /// </summary>
    public void Set<TValue>(string key, TValue value) => _items[key] = value;

    /// <summary>
    /// Attempts to retrieve a value from the context.
    /// </summary>
    public bool TryGet<TValue>(string key, out TValue? value)
    {
        if (_items.TryGetValue(key, out var raw) && raw is TValue typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Retrieves a value or adds it when missing using the supplied factory.
    /// </summary>
    public TValue GetOrAdd<TValue>(string key, Func<string, TValue> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return (TValue)_items.GetOrAdd(key, k => factory(k))!;
    }
}
