using NEL_FutureDao_API.lib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NEL_FutureDao_API.Service
{
    public class ShellService
    {
        public string exeFileName { get; set; }

        public JArray listMethod()
        {
            try
            {
                if (!File.Exists(exeFileName))
                {
                    exeFileName = "cat ../../../Comm/Api.cs | grep case | grep -vw grep";
                }
                var resStr = exeFileName.Bash();
                var resArr = resStr.Split("\n");
                var list = resArr.Where(p => p.Trim().Length > 0).Select(p =>
                {
                    var st = p.IndexOf("\"");
                    var ed = p.LastIndexOf("\"");
                    if (st < 0 || ed < 0) return "";
                    var ns = p.Substring(st + 1, ed - st - 1);
                    return ns;
                }).Where(p => p != "testnet" && p != "mainnet" && p.Trim().Length > 0).ToList();
                return new JArray { new JObject { { "count", list.Count }, { "list", new JArray { list } } } };
            } catch(Exception ex)
            {
                return new JArray { new JObject { { "exeFileName", exeFileName},{ "error", ex.ToString()} } };
            }
            
        }
    }
}
