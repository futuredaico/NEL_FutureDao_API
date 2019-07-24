using log4net;
using log4net.Config;
using log4net.Repository;
using Newtonsoft.Json;
using System;
using System.IO;

namespace NEL.NNS.lib
{
    public static class LogHelper
    {
        public static string name;

        public static void init()
        {
            ILoggerRepository repository = LogManager.CreateRepository("NELCoreRepository");
            XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));
            name = repository.Name;
        }

        public static ILog GetLogger(Type type)
        {
            return LogManager.GetLogger(name, type);
        }

        public static string logInfoFormat(object inputJson, object outputJson, DateTime start)
        {
            return "\r\n input:\r\n"
                + JsonConvert.SerializeObject(inputJson)
                + "\r\n output \r\n"
                + JsonConvert.SerializeObject(outputJson)
                + "\r\n exetime \r\n"
                + DateTime.Now.Subtract(start).TotalMilliseconds
                + "ms \r\n";
        }
    }
}
