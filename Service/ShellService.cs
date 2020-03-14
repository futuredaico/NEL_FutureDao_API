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
            if(!File.Exists(exeFileName))
            {
                exeFileName = "cat ../../../Comm/Api.cs | grep case | grep -vw grep";
            }
            var resStr = exeFileName.Bash();
            var resArr = resStr.Split("\n");
            var list = resArr.Select(p =>
            {
                var st = p.IndexOf("\"");
                var ed = p.LastIndexOf("\"");
                var ns = p.Substring(st + 1, ed - st + 1);
                return ns;
            }).Where(p=>p != "testnet" && p != "mainnet").ToList();


            return new JArray { new JObject { { "exeFileName", exeFileName},{ "count", list.Count},{ "list", new JArray { list } } } };
        }
    }
}
