using System.Linq;

namespace Winterleaf.SharedServices.Interrogator.Extensions
{
    internal static class Extensions
    {
        public static bool IsNumeric(this string numberString)
        {
            foreach (byte c in numberString.ToArray())
                {
                if ((((c < 48) || (c > 57)) && (c != 46) && (c != 44) && c != '-' && c != '+'))
                    return false;
                }
            return true;
        }
    }
}