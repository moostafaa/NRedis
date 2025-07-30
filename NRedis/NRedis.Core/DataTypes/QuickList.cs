/// <summary>
/// Implementation of QuickList, the primary data structure for Lists in modern Redis.
/// It's a linked list where each node is a ListPack.
/// </summary>
public class QuickList
{
    private readonly LinkedList<ListPack> _list = new LinkedList<ListPack>();

    // Configuration would go here (e.g., max size of each listpack node)

    public void AddFirst(Sds value)
    {
        if (_list.First == null)
        {
            _list.AddFirst(new ListPack());
        }
        // In real Redis, logic would decide if the node is full and a new one is needed.
        _list.First.Value.Add(value.ToBytes());
    }

    public void AddLast(Sds value)
    {
        if (_list.Last == null)
        {
            _list.AddLast(new ListPack());
        }
        _list.Last.Value.Add(value.ToBytes());
    }

    // Pop, Index, etc. would navigate this structure.
}