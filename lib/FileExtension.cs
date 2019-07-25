
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
        
    }
}
