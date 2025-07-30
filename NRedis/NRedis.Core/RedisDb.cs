
using NRedis.Core.Common;
using NRedis.Core.DataTypes;

/// <summary>
/// Represents a single Redis database (e.g., DB 0), holding the main
/// key-value dictionary and logic for encoding conversions.
/// </summary>
public class RedisDb
{
    private readonly Dictionary<Sds, RedisObject> _dict = [];

    public RedisObject Get(Sds key)
    {
        _dict.TryGetValue(key, out RedisObject value);
        return value;
    }

    public void Set(Sds key, RedisObject value)
    {
        _dict[key] = value;
    }

    public void HSet(Sds key, Sds field, Sds value)
    {
        RedisObject robj = Get(key);
        if (robj == null)
        {
            robj = RedisObject.CreateHash();
            Set(key, robj);
        }

        if (robj.Type != RedisObjectType.Hash)
            throw new InvalidOperationException("Operation against a key holding the wrong kind of value");

        if (robj.Encoding == RedisObjectEncoding.ListPack)
        {
            if (((ListPack)robj.Value).Count * 2 >= RedisConstants.HashMaxListpackEntries ||
                field.Length > RedisConstants.HashMaxListpackValue ||
                value.Length > RedisConstants.HashMaxListpackValue)
            {
                ConvertHashEncoding(robj);
            }
        }

        if (robj.Encoding == RedisObjectEncoding.ListPack)
        {
            var lp = (ListPack)robj.Value;
            // In a real implementation, we'd find and replace if field exists.
            // This simplified version just appends key and value.
            lp.Append(field.ToBytes());
            lp.Append(value.ToBytes());
        }
        else if (robj.Encoding == RedisObjectEncoding.HashTable)
        {
            var ht = (Dictionary<Sds, Sds>)robj.Value;
            ht[field] = value;
        }
    }

    private void ConvertHashEncoding(RedisObject robj)
    {
        var lp = (ListPack)robj.Value;
        var ht = new Dictionary<Sds, Sds>();

        // This GetAll() is a stand-in for proper iteration
        byte[] p = lp.First();
        while (p != null)
        {
            var keyBytes = lp.Get(p);
            p = lp.Next(p);
            if (p == null) break;
            var valBytes = lp.Get(p);
            ht.Add(new Sds(keyBytes), new Sds(valBytes));
            p = lp.Next(p);
        }

        robj.Encoding = RedisObjectEncoding.HashTable;
        robj.Value = ht;
        Console.WriteLine("--> Converted Hash from ListPack to HashTable");
    }

    public void SAdd(Sds key, Sds member)
    {
        RedisObject robj = Get(key);
        if (robj == null)
        {
            robj = RedisObject.CreateSet();
            Set(key, robj);
        }

        if (robj.Type != RedisObjectType.Set)
            throw new InvalidOperationException("Operation against a key holding the wrong kind of value");

        if (robj.Encoding == RedisObjectEncoding.IntSet)
        {
            if (!long.TryParse(member.ToString(), out _))
            {
                ConvertSetEncoding(robj);
            }
            else if (((IntSet)robj.Value).Count >= RedisConstants.SetMaxIntsetEntries)
            {
                ConvertSetEncoding(robj);
            }
        }

        if (robj.Encoding == RedisObjectEncoding.IntSet)
        {
            ((IntSet)robj.Value).Add(long.Parse(member.ToString()));
        }
        else if (robj.Encoding == RedisObjectEncoding.HashTable)
        {
            var ht = (Dictionary<Sds, object>)robj.Value;
            ht[member] = null;
        }
    }

    private void ConvertSetEncoding(RedisObject robj)
    {
        var iset = (IntSet)robj.Value;
        var ht = new Dictionary<Sds, object>();
        foreach (var member in iset.GetMembers())
        {
            ht.Add(new Sds(member.ToString()), null);
        }

        robj.Encoding = RedisObjectEncoding.HashTable;
        robj.Value = ht;
        Console.WriteLine("--> Converted Set from IntSet to HashTable");
    }
}

