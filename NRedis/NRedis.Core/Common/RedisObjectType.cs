
namespace NRedis.Core.Common;

/// <summary>
/// Defines the high-level data types that Redis supports.
/// </summary>
public enum RedisObjectType
{
    /// <summary>
    /// Represents a Redis string data type.
    /// </summary>
    String,
    /// <summary>
    /// Represents a Redis list data type.
    /// </summary>
    List,
    /// <summary>
    /// Represents a Redis hash data type.
    /// </summary>
    Hash,
    /// <summary>
    /// Represents a Redis set data type.
    /// </summary>
    Set,
    /// <summary>
    /// Represents a Redis sorted set (ZSet) data type.
    /// </summary> 
    ZSet// Sorted Set
    // Note: Stream is omitted for brevity as its implementation (Rax Tree) is very complex.
}