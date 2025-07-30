using NRedis.Core.Common;
using NRedis.Core.DataTypes;

/// <summary>
/// The core object that wraps all Redis data types. It holds metadata
/// about the type, encoding, and a reference to the actual data structure.
/// </summary>
public class RedisObject
{
    public RedisObjectType Type { get; internal set; }
    public RedisObjectEncoding Encoding { get; internal set; }
    public object Value { get; internal set; }

    // LRU/LFU data for eviction policies would go here
    // private int _lru;

    public RedisObject(RedisObjectType type, RedisObjectEncoding encoding, object value)
    {
        Type = type;
        Encoding = encoding;
        Value = value;
    }

    public static RedisObject CreateString(Sds value)
    {
        // Attempt to encode as an integer first
        if (long.TryParse(value.ToString(), out long longVal))
        {
            return new RedisObject(RedisObjectType.String, RedisObjectEncoding.Int, longVal);
        }
        return new RedisObject(RedisObjectType.String, RedisObjectEncoding.Raw, value);
    }

    public static RedisObject CreateList() => new RedisObject(RedisObjectType.List, RedisObjectEncoding.QuickList, new QuickList());
    public static RedisObject CreateHash() => new RedisObject(RedisObjectType.Hash, RedisObjectEncoding.ListPack, new ListPack());
    public static RedisObject CreateSet() => new RedisObject(RedisObjectType.Set, RedisObjectEncoding.IntSet, new IntSet());
    public static RedisObject CreateZSet() => new RedisObject(RedisObjectType.ZSet, RedisObjectEncoding.ListPack, new ListPack());
}