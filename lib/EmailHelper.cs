using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NEL.NNS.lib
{
    public static class EmailHelper
    {
        private static Regex r = new Regex("^\\s*([A-Za-z0-9_-]+(\\.\\w+)*@(\\w+\\.)+\\w{2,5})\\s*$");

        public static bool checkEmail(this string email) {
            return r.IsMatch(email);
        }

        private static Regex fielUrlRegex = new Regex("((https)://|)[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]");
        public static List<string> catchFileUrl(this string content)
        {
            var list = new List<string>();
            var match = fielUrlRegex.Match(content);
            while (match.Success)
            {
                var val = match.Value;
                if (val.StartsWith("https://")) list.Add(val);
                match = match.NextMatch();
            }
            return list;
        }
    }
}
