
using NEL.NNS.lib;
using System;

namespace NEL_FutureDao_API.lib
{
    public static class FileExtension
    {
        public static string toTemp(this string fileName)
        {
            return "temp_" + fileName;
        }
        public static string toBak(this string fileName) {
            return "bak_" + fileName;
        }
        public static string toFileName(this string path)
        {
            int index = path.LastIndexOf("/");
            if (index == -1) return path;
            return path.Substring(index+1);
        }
        
        public static string toRandomFileName(this string fileName)
        {
            return string.Format("{0}_{1}_{2}", TimeHelper.GetTimeStamp(), new Random().Next(1000), fileName);
        }
    }
}
