
namespace LEFX.JsLibGen
{
    public static class StringUtility
    {
        public static string FirstCharacterToLower(this string s)
        {
            if (string.IsNullOrEmpty(s) || char.IsLower(s, 0))
            {
                return s;
            }

            return char.ToLowerInvariant(s[0]) + s[1..];
        }
    }
}