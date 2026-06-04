// ComboCalculator.cs — Static helper that resolves orb combo multipliers.
public static class ComboCalculator
{
    // Returns (multiplier, comboName) for the given pair of orb names.
    // The pair is order-independent.
    public static (float multiplier, string comboName) Calculate(string orbA, string orbB)
    {
        // Normalise to lower-case for comparison
        string a = orbA.ToLower();
        string b = orbB.ToLower();

        if (Match(a, b, "dark",    "void"))      return (3.20f, "Shadow Collapse");
        if (Match(a, b, "crystal", "light"))     return (2.75f, "Prismatic Burst");
        if (Match(a, b, "nature",  "fire"))      return (2.25f, "Wildfire Bloom");
        if (Match(a, b, "water",   "lightning")) return (2.15f, "Storm Surge");
        if (Match(a, b, "nature",  "earth"))     return (2.10f, "Terra Growth");

        // Fallback — no special synergy
        return (1.0f, "Basic Attack");
    }

    private static bool Match(string a, string b, string x, string y)
    {
        return (a == x && b == y) || (a == y && b == x);
    }
}
