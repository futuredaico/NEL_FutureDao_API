using Newtonsoft.Json.Linq;
using System;
using System.Globalization;

namespace NEL.NNS.lib
{
    public static class MongoDecimalHelper
    {
        public static string formatDecimal(this string numberDecimalStr)
        {
            string value = numberDecimalStr;
            if (numberDecimalStr.Contains("$numberDecimal"))
            {
                value = Convert.ToString(JObject.Parse(numberDecimalStr)["$numberDecimal"]);
            }
            if (numberDecimalStr.Contains("_csharpnull"))
            {
                value = "0";
            }
            if (value.Contains("E"))
            {
                value = decimal.Parse(value, NumberStyles.Float).ToString();
            }
            return value;
        }
        public static decimal formatDecimalDouble(this string numberDecimalStr)
        {
            return decimal.Parse(numberDecimalStr.formatDecimal(), NumberStyles.Float);
        }
    }
}
