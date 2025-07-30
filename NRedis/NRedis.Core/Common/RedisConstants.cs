public static class RedisConstants
{
    // Thresholds for switching between encodings
    public const int HashMaxListpackEntries = 512;
    public const int HashMaxListpackValue = 64; // bytes
    public const int SetMaxIntsetEntries = 512;
    public const int ZsetMaxListpackEntries = 128;
    public const int ZsetMaxListpackValue = 64; // bytes
    public const int QuicklistFillFactor = -2; // Default fill factor

    // ListPack insertion constants
    public const int LP_BEFORE = 0;
    public const int LP_AFTER = 1;
    public const int LP_REPLACE = 2;
}