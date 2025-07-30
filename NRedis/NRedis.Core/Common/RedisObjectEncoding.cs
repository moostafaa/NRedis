namespace NRedis.Core.Common;

/// <summary>
/// Defines the underlying memory encoding for a RedisObject.
/// This is the key to Redis's performance and memory efficiency.
/// </summary>
public enum RedisObjectEncoding
{
    // --- String Encodings ---
    Raw,        // Standard Simple Dynamic String (Sds)
    EmbStr,     // Embedded Sds for short strings (optimization)
    Int,        // 64-bit signed integer (optimization)

    // --- Hash Encodings ---
    HashTable,  // Standard Dictionary<Sds, Sds>
    ListPack,   // Memory-optimized flat byte array for small hashes

    // --- List Encodings ---
    QuickList,  // A linked list of ListPacks (modern standard)

    // --- Set Encodings ---
    IntSet,     // Memory-optimized sorted array for integer-only sets
    // HashTable is also used for Sets with non-integer members

    // --- Sorted Set (ZSet) Encodings ---
    SkipList    // A skip list combined with a hash table for performance
    // ListPack is also used for small ZSets
}