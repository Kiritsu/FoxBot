namespace Fox.Extensions
{
    public static class IntegerExtensions
    {
        public static string Plural(this int nbr, bool upper = false)
        {
            return nbr > 1 ? upper ? "S" : "s" : "";
        }
    }
}
