using Axis.Luna.Common;
using Axis.Luna.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace Axis.Pulsar.Core.Utils
{

    //public readonly struct Segment :
    //    ICountable,
    //    IOffsetable,
    //    IEquatable<Segment>,
    //    IDefaultValueProvider<Segment>
    //{
    //    #region Props
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public int Offset { get; }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public int Count { get; }

    //    public int EndOffset => Count switch
    //    {
    //        0 => Offset,
    //        > 0 => Offset + Count - 1,
    //        _ => throw new InvalidOperationException($"Invalid {nameof(Count)}: {Count}")
    //    };
    //    #endregion

    //    #region DefaultValueProvider

    //    public bool IsDefault => Offset == 0 && Count == 0;

    //    public static Segment Default => default;

    //    #endregion

    //    #region Construction
    //    public Segment(int offset, int length)
    //    {
    //        if (length < 0)
    //            throw new ArgumentOutOfRangeException(nameof(length));

    //        Offset = offset;
    //        Count = length;
    //    }

    //    public static Segment Of(
    //        int offset,
    //        int length)
    //        => new(offset, length);

    //    public static Segment Of(
    //        int offset)
    //        => new(offset, 1);

    //    public static implicit operator Segment((int Offset, int Count) segment) => new(segment.Offset, segment.Count);
    //    #endregion

    //    #region Helpers

    //    public bool Contains(Segment other)
    //    {
    //        return Offset <= other.Offset
    //            && EndOffset >= other.EndOffset;
    //    }

    //    public bool Intersects(Segment other)
    //    {
    //        return (Offset <= other.Offset && other.Offset <= EndOffset)
    //            || (other.Offset <= Offset && Offset <= other.EndOffset);
    //    }

    //    public Segment Merge(Segment other)
    //    {
    //        var newOffset = Math.Min(Offset, other.Offset);
    //        var newEndOffset = Math.Max(EndOffset, other.EndOffset);
    //        return Segment.Of(newOffset, newEndOffset - newOffset + 1);
    //    }

    //    public static Segment operator +(Segment first, Segment second) => first.Merge(second);

    //    public Range ToRange() => new(Offset, EndOffset);

    //    #endregion

    //    #region Overrides

    //    public override int GetHashCode() => HashCode.Combine(Offset, Count);

    //    public bool Equals(Segment other)
    //    {
    //        return Offset == other.Offset
    //            && Count == other.Count;
    //    }

    //    public override bool Equals([NotNullWhen(true)] object? obj)
    //    {
    //        return obj is Segment other && Equals(other);
    //    }

    //    public override string ToString() => $"[offset: {Offset}, length: {Count}]";

    //    public static bool operator ==(Segment first, Segment second) => first.Equals(second);

    //    public static bool operator !=(Segment first, Segment second) => !first.Equals(second);

    //    #endregion
    //}
}
