public static class RedisConstants
{
    // Thresholds for switching between encodings
    public const int HashMaxListpackEntries = 512;
    public const int HashMaxListpackValue = 64; // bytes
    public const int SetMaxIntsetEntries = 512;
    public const int ZsetMaxListpackEntries = 128;
    public const int ZsetMaxListpackValue = 64; // bytes
}