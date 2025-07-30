namespace NRedis.Core.DataTypes;
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
        public Node Backward { get; set; }
        public Level[] Levels { get; }

        public Node(Sds member, double score, int level)
        {
            Member = member;
            Score = score;
            Levels = new Level[level];
            for (int i = 0; i < level; i++) Levels[i] = new Level();
        }
    }

    public class Level
    {
        public Node Forward { get; set; }
        public uint Span { get; set; }
    }

    private const int MaxLevel = 32;
    private const double Probability = 0.25;

    private readonly Node _header;
    private int _level;
    private readonly Random _random = new Random();
    public int Count { get; private set; }

    public Node First => _header.Levels[0].Forward;
    public Node Last { get; private set; }


    public SkipList()
    {
        _header = new Node(null, 0, MaxLevel);
        _level = 1;
        Last = null;
    }

    private int RandomLevel()
    {
        int level = 1;
        while (_random.NextDouble() < Probability && level < MaxLevel) level++;
        return level;
    }

    public Node Insert(Sds member, double score)
    {
        var update = new Node[MaxLevel];
        var rank = new uint[MaxLevel];
        Node current = _header;

        for (int i = _level - 1; i >= 0; i--)
        {
            rank[i] = (i == (_level - 1)) ? 0 : rank[i + 1];
            while (current.Levels[i].Forward != null &&
                   (current.Levels[i].Forward.Score < score ||
                    (current.Levels[i].Forward.Score == score && current.Levels[i].Forward.Member.CompareTo(member) < 0)))
            {
                rank[i] += current.Levels[i].Span;
                current = current.Levels[i].Forward;
            }
            update[i] = current;
        }

        int newLevel = RandomLevel();
        if (newLevel > _level)
        {
            for (int i = _level; i < newLevel; i++)
            {
                rank[i] = 0;
                update[i] = _header;
                update[i].Levels[i].Span = (uint)Count;
            }
            _level = newLevel;
        }

        var newNode = new Node(member, score, newLevel);
        for (int i = 0; i < newLevel; i++)
        {
            newNode.Levels[i].Forward = update[i].Levels[i].Forward;
            update[i].Levels[i].Forward = newNode;

            newNode.Levels[i].Span = update[i].Levels[i].Span - (rank[0] - rank[i]);
            update[i].Levels[i].Span = (rank[0] - rank[i]) + 1;
        }

        for (int i = newLevel; i < _level; i++)
        {
            update[i].Levels[i].Span++;
        }

        newNode.Backward = (update[0] == _header) ? null : update[0];
        if (newNode.Levels[0].Forward != null)
        {
            newNode.Levels[0].Forward.Backward = newNode;
        }
        else
        {
            Last = newNode;
        }

        Count++;
        return newNode;
    }

    public bool Delete(Sds member, double score)
    {
        var update = new Node[MaxLevel];
        Node current = _header;

        for (int i = _level - 1; i >= 0; i--)
        {
            while (current.Levels[i].Forward != null &&
                   (current.Levels[i].Forward.Score < score ||
                    (current.Levels[i].Forward.Score == score && current.Levels[i].Forward.Member.CompareTo(member) < 0)))
            {
                current = current.Levels[i].Forward;
            }
            update[i] = current;
        }

        current = current.Levels[0].Forward;
        if (current != null && current.Score == score && current.Member.Equals(member))
        {
            DeleteNode(current, update);
            return true;
        }
        return false;
    }

    private void DeleteNode(Node node, Node[] update)
    {
        for (int i = 0; i < _level; i++)
        {
            if (update[i].Levels[i].Forward == node)
            {
                update[i].Levels[i].Span += node.Levels[i].Span - 1;
                update[i].Levels[i].Forward = node.Levels[i].Forward;
            }
            else
            {
                update[i].Levels[i].Span -= 1;
            }
        }

        if (node.Levels[0].Forward != null)
        {
            node.Levels[0].Forward.Backward = node.Backward;
        }
        else
        {
            Last = node.Backward;
        }

        while (_level > 1 && _header.Levels[_level - 1].Forward == null)
        {
            _level--;
        }
        Count--;
    }

    public long GetRank(Sds member, double score)
    {
        long rank = 0;
        Node current = _header;
        for (int i = _level - 1; i >= 0; i--)
        {
            while (current.Levels[i].Forward != null &&
                   (current.Levels[i].Forward.Score < score ||
                    (current.Levels[i].Forward.Score == score && current.Levels[i].Forward.Member.CompareTo(member) <= 0)))
            {
                rank += current.Levels[i].Span;
                current = current.Levels[i].Forward;
            }
            if (current.Member != null && current.Member.Equals(member))
            {
                return rank;
            }
        }
        return 0; // Not found
    }

    public Node GetElementByRank(long rank)
    {
        long traversed = 0;
        Node current = _header;
        for (int i = _level - 1; i >= 0; i--)
        {
            while (current.Levels[i].Forward != null && (traversed + current.Levels[i].Span) <= rank)
            {
                traversed += current.Levels[i].Span;
                current = current.Levels[i].Forward;
            }
            if (traversed == rank)
            {
                return current;
            }
        }
        return null;
    }
}