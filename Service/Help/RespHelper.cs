
using NEL_FutureDao_API.Service.State;
using Newtonsoft.Json.Linq;

namespace NEL_FutureDao_API.Service.Help
{
    public class RespHelper
    {
        public static JObject defaultData = new JObject();
        public static JArray getErrorRes(string code)
        {
            return new JArray { new JObject { { "resultCode", code }, { "data", defaultData } } };
        }

        public static JArray getRes(JToken res = null)
        {
            return new JArray { new JObject { { "resultCode", DaoReturnCode.success }, { "data", res ?? defaultData } } };
        }
        public static bool checkResCode(JArray res)
        {
            return res[0]["resultCode"].ToString() == DaoReturnCode.success;
        }

    }
}
