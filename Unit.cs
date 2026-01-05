namespace RopStarterPack;

/// <summary>
/// Represents "no value" - use with Result&lt;Unit, E&gt; for operations that can fail but return nothing.
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = default;
}
