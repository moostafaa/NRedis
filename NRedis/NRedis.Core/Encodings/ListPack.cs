/// <summary>
/// A simplified simulation of ListPack. A ListPack is a continuous block of memory
/// storing a sequence of entries. It's used for small Hashes, Lists, and ZSets.
/// This C# version uses a List<byte[]> to simulate the flat structure.
/// </summary>
public class ListPack
{
    // In a real C implementation, this would be a single byte array (unsigned char*).
    // We simulate it with a list of byte arrays for simplicity in C#.
    private readonly List<byte[]> _entries = new List<byte[]>();

    public int Count => _entries.Count;
    public int TotalBytes => _entries.Sum(e => e.Length);

    public void Add(byte[] entry) => _entries.Add(entry);
    public byte[] Get(int index) => _entries[index];
    public IEnumerable<byte[]> GetAll() => _entries;

    // In a real listpack, you'd have methods to insert, delete, and find entries
    // by navigating the byte structure, which is very complex.
}