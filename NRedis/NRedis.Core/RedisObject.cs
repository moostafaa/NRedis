/// <summary>
/// The core object that wraps all Redis data types. It holds metadata
/// about the type, encoding, and a reference to the actual data structure.
/// </summary>
public class RedisObject
{
    public RedisObjectType Type { get; private set; }
    public RedisObjectEncoding Encoding { get; private set; }
    public object Value { get; private set; }

    // LRU/LFU data for eviction policies would go here
    // private int _lru;

    public RedisObject(RedisObjectType type, RedisObjectEncoding encoding, object value)
    {
        Type = type;
        Encoding = encoding;
        Value = value;
    }

    // --- Static factory methods for creating objects ---

    public static RedisObject CreateString(Sds value)
    {
        // Attempt to encode as an integer first
        if (long.TryParse(value.ToString(), out long longVal))
        {
            return new RedisObject(RedisObjectType.String, RedisObjectEncoding.Int, longVal);
        }
        // Could add EmbStr logic here based on length
        return new RedisObject(RedisObjectType.String, RedisObjectEncoding.Raw, value);
    }

    public static RedisObject CreateList()
    {
        return new RedisObject(RedisObjectType.List, RedisObjectEncoding.QuickList, new QuickList());
    }

    public static RedisObject CreateHash()
    {
        return new RedisObject(RedisObjectType.Hash, RedisObjectEncoding.ListPack, new ListPack());
    }

    public static RedisObject CreateSet()
    {
        return new RedisObject(RedisObjectType.Set, RedisObjectEncoding.IntSet, new IntSet());
    }

    public static RedisObject CreateZSet()
    {
        return new RedisObject(RedisObjectType.ZSet, RedisObjectEncoding.ListPack, new ListPack());
    }
}