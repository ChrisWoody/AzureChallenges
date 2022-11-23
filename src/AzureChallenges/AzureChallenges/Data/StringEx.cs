namespace AzureChallenges.Data;

public static class StringEx
{
    public static bool HasValue(this string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
}