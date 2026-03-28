using System.Runtime.CompilerServices;

namespace BaseLib.Utils;

/// <summary>
/// A basic wrapper around <seealso cref="ConditionalWeakTable{TKey, TValue}"/> for convenience.
/// While this can be used to store value types, they will be boxed and thus is somewhat inefficient.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TVal"></typeparam>
public class SpireField<TKey, TVal> where TKey : class
{
    private readonly ConditionalWeakTable<TKey, object?> _table = [];
    private readonly Func<TKey, TVal?> _defaultVal;

    public SpireField(Func<TVal?> defaultVal)
    {
        _defaultVal = _ => defaultVal();
    }

    public SpireField(Func<TKey, TVal?> defaultVal)
    {
        _defaultVal = defaultVal;
    }

    public TVal? this[TKey obj]
    {
        get => Get(obj);
        set => Set(obj, value);
    }

    public TVal? Get(TKey obj) {
        if (_table.TryGetValue(obj, out var result)) return (TVal?)result;

        _table.Add(obj, result = _defaultVal(obj));
        return (TVal?)result;
    }

    public void Set(TKey obj, TVal? val)
    {
        _table.AddOrUpdate(obj, val);
    }
}