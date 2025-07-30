namespace NRedis.Core.DataTypes;
/// <summary>
/// Implementation of QuickList, the primary data structure for Lists in modern Redis.
/// It's a linked list where each node is a ListPack.
/// </summary>
public class QuickList
{
    private readonly LinkedList<ListPack> _list = new LinkedList<ListPack>();
    private readonly int _fillFactor = RedisConstants.QuicklistFillFactor;
    private int _count;

    public int Count => _count;

    // --- Public API mirroring quicklist.c ---

    public void PushHead(Sds value)
    {
        var node = _list.First;
        if (node == null || node.Value.Count >= _fillFactor * -1)
        {
            var lp = new ListPack();
            lp.Prepend(value.ToBytes());
            _list.AddFirst(lp);
        }
        else
        {
            node.Value.Prepend(value.ToBytes());
        }
        _count++;
    }

    public void PushTail(Sds value)
    {
        var node = _list.Last;
        if (node == null || node.Value.Count >= _fillFactor * -1)
        {
            var lp = new ListPack();
            lp.Append(value.ToBytes());
            _list.AddLast(lp);
        }
        else
        {
            node.Value.Append(value.ToBytes());
        }
        _count++;
    }

    public bool PopHead(out Sds value)
    {
        value = null;
        var node = _list.First;
        if (node == null) return false;

        var lp = node.Value;
        var p = lp.First();
        if (p == null) return false;

        value = new Sds(lp.Get(p));
        lp.Delete(p);
        _count--;

        if (lp.Count == 0)
        {
            _list.RemoveFirst();
        }
        return true;
    }

    public bool PopTail(out Sds value)
    {
        value = null;
        var node = _list.Last;
        if (node == null) return false;

        var lp = node.Value;
        var p = lp.Last();
        if (p == null) return false;

        value = new Sds(lp.Get(p));
        lp.Delete(p);
        _count--;

        if (lp.Count == 0)
        {
            _list.RemoveLast();
        }
        return true;
    }

    public Sds Index(long index)
    {
        if (index >= _count || index < -_count) return null;
        if (index < 0) index = _count + index;

        var node = _list.First;
        long current_index = 0;
        while (node != null)
        {
            long node_count = node.Value.Count;
            if (current_index + node_count > index)
            {
                long local_index = index - current_index;
                var p = node.Value.Seek(local_index);
                return p != null ? new Sds(node.Value.Get(p)) : null;
            }
            current_index += node_count;
            node = node.Next;
        }
        return null;
    }

    public void Rotate()
    {
        if (_count <= 1) return;

        if (PopTail(out Sds tail))
        {
            PushHead(tail);
        }
    }   

    public IEnumerable<Sds> GetRange(long start, long stop)
    {
        // Simplified range implementation
        long s = start < 0 ? _count + start : start;
        long e = stop < 0 ? _count + stop : stop;

        for (long i = s; i <= e && i < _count; i++)
        {
            yield return Index(i);
        }
    }
}