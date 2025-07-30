using System.Text;

namespace NRedis.Core.DataTypes;

/// <summary>
/// A C# implementation of Redis's Simple Dynamic String (sds).
/// This is more efficient than native strings for frequent modifications
/// because it avoids reallocations by pre-allocating space and storing length/capacity.
/// It is also binary-safe.
/// </summary>
public class Sds : IComparable<Sds>, IEquatable<Sds>
{
    private byte[] _buffer;
    private int _length;

    public int Length => _length;
    public int Capacity => _buffer.Length;

    public Sds(string initial)
    {
        var bytes = Encoding.UTF8.GetBytes(initial);
        _length = bytes.Length;
        _buffer = new byte[GetNextPowerOfTwo(_length)];
        Buffer.BlockCopy(bytes, 0, _buffer, 0, _length);
    }

    public Sds(byte[] initial)
    {
        _length = initial.Length;
        _buffer = new byte[GetNextPowerOfTwo(_length)];
        Buffer.BlockCopy(initial, 0, _buffer, 0, _length);
    }

    public void Append(Sds other)
    {
        EnsureCapacity(_length + other._length);
        Buffer.BlockCopy(other._buffer, 0, _buffer, _length, other._length);
        _length += other._length;
    }

    public void Trim(int start, int end)
    {
        if (start < 0 || end >= _length || start > end)
            throw new ArgumentOutOfRangeException();

        int newLength = (end - start) + 1;
        Buffer.BlockCopy(_buffer, start, _buffer, 0, newLength);
        _length = newLength;
    }

    public Sds Range(int start, int end)
    {
        if (start < 0 || end >= _length || start > end)
            throw new ArgumentOutOfRangeException();

        int len = (end - start) + 1;
        var newBytes = new byte[len];
        Buffer.BlockCopy(_buffer, start, newBytes, 0, len);
        return new Sds(newBytes);
    }

    private void EnsureCapacity(int required)
    {
        if (required > Capacity)
        {
            int newCapacity = GetNextPowerOfTwo(required);
            Array.Resize(ref _buffer, newCapacity);
        }
    }

    public override string ToString() => Encoding.UTF8.GetString(_buffer, 0, _length);
    public byte[] ToBytes()
    {
        var bytes = new byte[_length];
        Buffer.BlockCopy(_buffer, 0, bytes, 0, _length);
        return bytes;
    }

    public int CompareTo(Sds other) => _buffer.AsSpan(0, _length).SequenceCompareTo(other._buffer.AsSpan(0, other._length));
    public bool Equals(Sds other) => other != null && _buffer.AsSpan(0, _length).SequenceEqual(other._buffer.AsSpan(0, other._length));
    public override int GetHashCode()
    {
        // FNV-1a hash algorithm for byte arrays
        const int fnvPrime = 16777619;
        int hash = -2128831035; // FNV offset basis
        for (int i = 0; i < _length; i++)
        {
            hash ^= _buffer[i];
            hash *= fnvPrime;
        }
        return hash;
    }
    public override bool Equals(object obj) => obj is Sds other && Equals(other);

    private static int GetNextPowerOfTwo(int v)
    {
        if (v == 0) return 2;
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;
        return v;
    }
}