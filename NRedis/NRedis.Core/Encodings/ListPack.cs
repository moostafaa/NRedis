/// <summary>
/// A C# port of Redis's listpack.c. This is a memory-efficient structure
/// for storing lists of strings or integers, replacing the older ziplist.
/// It operates on a single contiguous byte array.
/// </summary>
public class ListPack
{
    private byte[] _lp;

    // Header constants
    private const int LP_HDR_SIZE = 6;
    private const int LP_HDR_TOTAL_BYTES_OFF = 0;
    private const int LP_HDR_NUM_ELE_OFF = 4;
    private const byte LP_END = 0xFF;

    // Encoding masks
    private const byte LP_ENCODING_7BIT_UINT = 0;
    private const byte LP_ENCODING_6BIT_STR = 0x80;
    private const byte LP_ENCODING_13BIT_INT = 0xC0;
    private const byte LP_ENCODING_12BIT_STR = 0xE0;
    private const byte LP_ENCODING_16BIT_INT = 0xF1;
    private const byte LP_ENCODING_24BIT_INT = 0xF2;
    private const byte LP_ENCODING_32BIT_INT = 0xF3;
    private const byte LP_ENCODING_64BIT_INT = 0xF4;
    private const byte LP_ENCODING_32BIT_STR = 0xF0;

    public int Count => GetNumElements();
    public int TotalBytes => (int)GetTotalBytes();

    /// <summary>
    /// Creates a new, empty listpack.
    /// </summary>
    public ListPack()
    {
        _lp = new byte[LP_HDR_SIZE + 1];
        SetTotalBytes((uint)(LP_HDR_SIZE + 1));
        SetNumElements(0);
        _lp[LP_HDR_SIZE] = LP_END;
    }

    // --- Public API mirroring listpack.c ---

    public byte[] Prepend(byte[] element) => Insert(element, First(), RedisConstants.LP_BEFORE);
    public byte[] Append(byte[] element) => Insert(element, Seek(Count), RedisConstants.LP_AFTER);
    public byte[] First() => Seek(0);
    public byte[] Last() => Seek(-1);
    public byte[] Next(byte[] p) => GetNext(p);
    public byte[] Prev(byte[] p) => GetPrev(p);
    public int Length() => Count;
    public byte[] Get(byte[] p) => GetValue(p, out _, out _);

    public byte[] Delete(byte[] p)
    {
        int p_offset = GetOffset(p);
        int entryLen = GetEntryLength(p_offset);

        var newLp = new byte[_lp.Length - entryLen];
        Buffer.BlockCopy(_lp, 0, newLp, 0, p_offset);
        Buffer.BlockCopy(_lp, p_offset + entryLen, newLp, p_offset, _lp.Length - (p_offset + entryLen));
        _lp = newLp;

        SetTotalBytes((uint)_lp.Length);
        SetNumElements((ushort)(Count - 1));

        return p_offset < _lp.Length ? GetPointer(p_offset) : null;
    }

    // --- Private Helper and Encoding/Decoding Methods ---

    private uint GetTotalBytes() => BitConverter.ToUInt32(_lp, LP_HDR_TOTAL_BYTES_OFF);
    private void SetTotalBytes(uint val) => Array.Copy(BitConverter.GetBytes(val), 0, _lp, LP_HDR_TOTAL_BYTES_OFF, 4);
    private ushort GetNumElements() => BitConverter.ToUInt16(_lp, LP_HDR_NUM_ELE_OFF);
    private void SetNumElements(ushort val) => Array.Copy(BitConverter.GetBytes(val), 0, _lp, LP_HDR_NUM_ELE_OFF, 2);

    private byte[] GetPointer(int offset)
    {
        // In C#, we can't use real pointers. We simulate a pointer by creating
        // a small object that holds the offset. This is inefficient but demonstrates the logic.
        // A more performant C# version would pass integer offsets everywhere.
        return BitConverter.GetBytes(offset);
    }
    private int GetOffset(byte[] p) => p == null ? -1 : BitConverter.ToInt32(p, 0);

    private byte[] Insert(byte[] element, byte[] p, int where)
    {
        int p_offset = GetOffset(p);
        if (p_offset == -1 && where != RedisConstants.LP_AFTER) return null; // Cannot insert before/replace if p is null

        // Simplified encoding logic for demonstration
        int ele_len = element.Length;
        int req_len = ele_len + 1 + 4; // content + encoding byte + backlen

        int insert_offset = (where == RedisConstants.LP_AFTER) ?
            (p_offset == -1 ? LP_HDR_SIZE : p_offset + GetEntryLength(p_offset)) :
            p_offset;

        var newLp = new byte[_lp.Length + req_len];

        // Copy parts of old listpack
        Buffer.BlockCopy(_lp, 0, newLp, 0, insert_offset);
        Buffer.BlockCopy(_lp, insert_offset, newLp, insert_offset + req_len, _lp.Length - insert_offset);

        // Write new element
        newLp[insert_offset] = (byte)(LP_ENCODING_6BIT_STR | (byte)ele_len);
        Buffer.BlockCopy(element, 0, newLp, insert_offset + 1, ele_len);
        // Simplified backlen
        Array.Copy(BitConverter.GetBytes((uint)req_len), 0, newLp, insert_offset + 1 + ele_len, 4);

        _lp = newLp;
        SetTotalBytes((uint)_lp.Length);
        SetNumElements((ushort)(Count + 1));

        return GetPointer(insert_offset);
    }

    private int GetEntryLength(int p_offset)
    {
        // Highly simplified version of lpCurrentEncodedSize
        byte encoding = _lp[p_offset];
        if ((encoding & 0x80) == LP_ENCODING_6BIT_STR)
        {
            return 1 + (encoding & 0x3F) + 4; // encoding + content + backlen
        }
        // ... other encodings would be handled here
        return 0;
    }

    private byte[] GetValue(byte[] p, out long intVal, out int strLen)
    {
        intVal = 0;
        strLen = 0;
        int p_offset = GetOffset(p);
        if (p_offset == -1) return null;

        byte encoding = _lp[p_offset];
        if ((encoding & 0x80) == LP_ENCODING_6BIT_STR)
        {
            strLen = encoding & 0x3F;
            var val = new byte[strLen];
            Buffer.BlockCopy(_lp, p_offset + 1, val, 0, strLen);
            return val;
        }
        return null;
    }

    public byte[] Seek(long index)
    {
        if (index >= Count || index < -Count) return null;
        if (index < 0) index = Count + index;

        int pos = LP_HDR_SIZE;
        for (int i = 0; i < index; i++)
        {
            pos += GetEntryLength(pos);
        }
        return pos >= _lp.Length - 1 ? null : GetPointer(pos);
    }

    private byte[] GetNext(byte[] p)
    {
        int p_offset = GetOffset(p);
        if (p_offset == -1) return null;
        int next_offset = p_offset + GetEntryLength(p_offset);
        if (_lp[next_offset] == LP_END) return null;
        return GetPointer(next_offset);
    }

    private byte[] GetPrev(byte[] p)
    {
        int p_offset = GetOffset(p);
        if (p_offset == -1 || p_offset == LP_HDR_SIZE) return null;

        // In a real implementation, we'd decode the backlen at p_offset - backlen_size
        // This is a simplified placeholder.
        int prev_offset = -1; // Cannot be implemented without full backlen logic
        return prev_offset != -1 ? GetPointer(prev_offset) : null;
    }
}

