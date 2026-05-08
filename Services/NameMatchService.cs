namespace OfflinePaymentLinks.API.Services;

public static class NameMatchService
{
    public static decimal GetMatchPercentage(string name1, string name2)
    {
        if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
            return 0;

        var a = Normalize(name1);
        var b = Normalize(name2);

        if (a == b) return 100;

        int distance = LevenshteinDistance(a, b);
        int maxLen = Math.Max(a.Length, b.Length);

        if (maxLen == 0) return 100;

        decimal similarity = (1m - (decimal)distance / maxLen) * 100;
        return Math.Round(Math.Max(0, similarity), 2);
    }

    public static string GetMatchStatus(decimal percentage)
        => percentage >= 60 ? "Approved" : "Rejected";

    private static string Normalize(string name)
        => name.Trim().ToUpperInvariant()
               .Replace(".", "").Replace(",", "")
               .Replace("  ", " ");

    private static int LevenshteinDistance(string s, string t)
    {
        int m = s.Length, n = t.Length;
        int[,] d = new int[m + 1, n + 1];

        for (int i = 0; i <= m; i++) d[i, 0] = i;
        for (int j = 0; j <= n; j++) d[0, j] = j;

        for (int i = 1; i <= m; i++)
            for (int j = 1; j <= n; j++)
            {
                int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        return d[m, n];
    }
}