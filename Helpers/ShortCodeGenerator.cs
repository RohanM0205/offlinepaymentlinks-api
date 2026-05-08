namespace OfflinePaymentLinks.API.Helpers;

public static class ShortCodeGenerator
{
    private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Random Rng = new();

    public static string Generate(int length = 7)
        => new(Enumerable.Repeat(Chars, length).Select(s => s[Rng.Next(s.Length)]).ToArray());
}