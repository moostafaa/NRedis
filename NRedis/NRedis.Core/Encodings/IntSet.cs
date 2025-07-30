/// <summary>
/// Implementation of IntSet, a memory-optimized structure for storing
/// sets of only integers. It's essentially a sorted array of integers.
/// </summary>
public class IntSet
{
    private readonly SortedSet<long> _set = [];

    public bool Add(long value) => _set.Add(value);
    public bool Contains(long value) => _set.Contains(value);
    public bool Remove(long value) => _set.Remove(value);
    public int Count => _set.Count;
    public IEnumerable<long> GetMembers() => _set;
}