using Axis.Luna.Common;
using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core;

public readonly struct Segment
    : IDefaultValueProvider<Segment>
{
    /// <summary>
    /// 
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// 
    /// </summary>
    public int Length { get; }

    public int EndOffset => Length switch
    {
        0 => Offset,
        > 0 => Offset + Length - 1,
        _ => throw new InvalidOperationException($"Invalid {nameof(Length)}: {Length}")
    };

    #region DefaultValueProvider

    public bool IsDefault => Offset == 0 && Length == 0;

    public static Segment Default => default;

    #endregion

    public Segment(int offset, int length)
    {
        Offset = offset;
        Length = length.ThrowIf(
            i => i < 0,
            new ArgumentOutOfRangeException(nameof(length)));
    }

    public static Segment Of(
        int offset,
        int length)
        => new(offset, length);

    public static Segment Of(
        int offset)
        => new(offset, 1);

    public static implicit operator Segment((int Offset, int Length) segment) => new(segment.Offset, segment.Length);

    public override string ToString() => $"{{offset: {Offset}, length: {Length}}}";

    #region Helpers

    public bool Contains(Segment other)
    {
        return Offset <= other.Offset
            && EndOffset >= other.EndOffset;
    }

    public bool Intersects(Segment other)
    {
        return (Offset <= other.Offset && other.Offset <= EndOffset)
            || (other.Offset <= Offset && Offset <= other.EndOffset);
    }

    public Segment Merge(Segment other)
    {
        var newOffset = Math.Min(Offset, other.Offset);
        var newEndOffset = Math.Max(EndOffset, other.EndOffset);
        return Segment.Of(newOffset, newEndOffset - newOffset + 1);
    }

    public static Segment operator+(Segment first, Segment second) => first.Merge(second);

    public Range ToRange() => new(Offset, EndOffset);

    #endregion
}
