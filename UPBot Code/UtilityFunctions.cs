public static class UtilityFunctions
{
    public static string PluralFormatter(int count, string singular, string plural)
    {
        return count > 1 ? plural : singular;
    }
}