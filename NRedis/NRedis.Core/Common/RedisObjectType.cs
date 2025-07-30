/// <summary>
/// Defines the high-level data types that Redis supports.
/// </summary>
public enum RedisObjectType
{
    String,
    List,
    Hash,
    Set,
    ZSet // Sorted Set
    // Note: Stream is omitted for brevity as its implementation (Rax Tree) is very complex.
}