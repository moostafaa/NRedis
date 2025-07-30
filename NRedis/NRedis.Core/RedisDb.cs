/// <summary>
/// Represents a single Redis database (e.g., DB 0), holding the main
/// key-value dictionary and logic for encoding conversions.
/// </summary>
public class RedisDb
{
    private readonly Dictionary<Sds, RedisObject> _dict = new();

    public RedisObject Get(Sds key)
    {
        _dict.TryGetValue(key, out RedisObject value);
        return value;
    }

    public void Set(Sds key, RedisObject value)
    {
        _dict[key] = value;
    }

    // --- Hash Commands and Conversions ---
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

        // If it's a listpack, check if we need to convert it to a hash table
        if (robj.Encoding == RedisObjectEncoding.ListPack)
        {
            var lp = (ListPack)robj.Value;
            if (lp.Count * 2 >= RedisConstants.HashMaxListpackEntries ||
                field.Length > RedisConstants.HashMaxListpackValue ||
                value.Length > RedisConstants.HashMaxListpackValue)
            {
                ConvertHashToListPack(robj);
            }
        }

        // Add to the appropriate structure
        if (robj.Encoding == RedisObjectEncoding.ListPack)
        {
            var lp = (ListPack)robj.Value;
            // A real implementation would find and update or append
            lp.Add(field.ToBytes());
            lp.Add(value.ToBytes());
        }
        else if (robj.Encoding == RedisObjectEncoding.HashTable)
        {
            var ht = (Dictionary<Sds, Sds>)robj.Value;
            ht[field] = value;
        }
    }

    private void ConvertHashToListPack(RedisObject robj)
    {
        var lp = (ListPack)robj.Value;
        var ht = new Dictionary<Sds, Sds>();

        var entries = lp.GetAll().ToArray();
        for (int i = 0; i < entries.Length; i += 2)
        {
            ht.Add(new Sds(entries[i]), new Sds(entries[i + 1]));
        }

        robj = new RedisObject(RedisObjectType.Hash, RedisObjectEncoding.HashTable, ht);
        Console.WriteLine("--> Converted Hash from ListPack to HashTable");
    }

    // --- Set Commands and Conversions ---
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
            if (!long.TryParse(member.ToString(), out long intVal))
            {
                // If the new member is not an integer, we must convert the whole set
                ConvertSetToIntSet(robj);
            }
            else
            {
                var iset = (IntSet)robj.Value;
                if (iset.Count >= RedisConstants.SetMaxIntsetEntries)
                {
                    ConvertSetToIntSet(robj);
                }
            }
        }

        if (robj.Encoding == RedisObjectEncoding.IntSet)
        {
            ((IntSet)robj.Value).Add(long.Parse(member.ToString()));
        }
        else if (robj.Encoding == RedisObjectEncoding.HashTable)
        {
            var ht = (Dictionary<Sds, object>)robj.Value;
            ht[member] = null; // In a set hash table, value is irrelevant
        }
    }

    private void ConvertSetToIntSet(RedisObject robj)
    {
        var iset = (IntSet)robj.Value;
        var ht = new Dictionary<Sds, object>();
        foreach (var member in iset.GetMembers())
        {
            ht.Add(new Sds(member.ToString()), null);
        }

        robj = new RedisObject(RedisObjectType.Set, RedisObjectEncoding.HashTable, ht);
        Console.WriteLine("--> Converted Set from IntSet to HashTable");
    }

    // ZSet and List would have similar conversion logic.
}
