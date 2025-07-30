/// <summary>
/// Implementation of a Skip List, used for Sorted Sets (ZSets).
/// A Skip List allows for fast search, insertion, and deletion (O(log n) on average).
/// </summary>
public class SkipList
{
    public class Node
    {
        public Sds Member { get; }
        public double Score { get; set; }
        public Node[] Forward { get; }

        public Node(Sds member, double score, int level)
        {
            Member = member;
            Score = score;
            Forward = new Node[level];
        }
    }

    private const int MaxLevel = 32; // Corresponds to 2^32 elements
    private const double Probability = 0.25; // P-value for level generation

    private readonly Node _header;
    private int _level;
    private readonly Random _random = new Random();

    public int Count { get; private set; }

    public SkipList()
    {
        _header = new Node(null, 0, MaxLevel);
        _level = 1;
    }

    private int RandomLevel()
    {
        int level = 1;
        while (_random.NextDouble() < Probability && level < MaxLevel)
        {
            level++;
        }
        return level;
    }

    public Node Insert(Sds member, double score)
    {
        var update = new Node[MaxLevel];
        Node current = _header;

        for (int i = _level - 1; i >= 0; i--)
        {
            while (current.Forward[i] != null &&
                   (current.Forward[i].Score < score ||
                    (current.Forward[i].Score == score && current.Forward[i].Member.CompareTo(member) < 0)))
            {
                current = current.Forward[i];
            }
            update[i] = current;
        }

        current = current.Forward[0];

        // Update score if member already exists
        if (current != null && current.Score == score && current.Member.Equals(member))
        {
            // In Redis, this would be an update operation, but for simplicity, we assume insert is unique
            return current;
        }

        int newLevel = RandomLevel();
        if (newLevel > _level)
        {
            for (int i = _level; i < newLevel; i++)
            {
                update[i] = _header;
            }
            _level = newLevel;
        }

        var newNode = new Node(member, score, newLevel);
        for (int i = 0; i < newLevel; i++)
        {
            newNode.Forward[i] = update[i].Forward[i];
            update[i].Forward[i] = newNode;
        }

        Count++;
        return newNode;
    }

    // Find, Delete, and other operations would follow a similar pattern.
}