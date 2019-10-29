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

        private static decimal EthPrecision = decimal.Parse("1000000000000000000");
        public static decimal formatEth(this decimal numberDecimal)
        {
            return numberDecimal / EthPrecision;
        }
        public static string formatEth(this string numberDecimalStr)
        {
            return decimal.Parse(numberDecimalStr).formatEth().ToString();
        }
        private static decimal ratioPrecision = 1000;
        public static string formatRatio(this string ratio)
        {
            return (decimal.Parse(ratio)/ratioPrecision).ToString();
        }
    }
}
