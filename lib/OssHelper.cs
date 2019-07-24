using Aliyun.OSS;
using System.IO;
using System.Linq;
using System.Text;

namespace NEL_FutureDao_API
{
    public class OssHelper
    {
        private OssClient client;
        private string bucketName;

        public OssHelper(string endpoint, string accessKeyId, string accessKeySecret, string bucketName)
        {
            client = new OssClient(endpoint, accessKeyId, accessKeySecret);
            if (!client.ListBuckets().Any(p => bucketName == p.Name))
            {
                client.CreateBucket(bucketName);
            }
            this.bucketName = bucketName;
        }

        public string PutObject(string filename, string content)
        {
            byte[] binaryData = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(binaryData);
            client.PutObject(bucketName, filename, stream);
            return "true";
        }
        public string PutObject(string filename, byte[] binaryData)
        {
            var stream = new MemoryStream(binaryData);
            client.PutObject(bucketName, filename, stream);
            return "true";
        }
        public string GetObject(string filename)
        {
            StringBuilder sb = new StringBuilder();
            var result = client.GetObject(bucketName, filename);
            using (var requestStream = result.Content)
            {
                int length = 4 * 1024;
                var buf = new byte[length];
                while (true)
                {
                    length = requestStream.Read(buf, 0, length);
                    if (length == 0) break;
                    sb.Append(Encoding.UTF8.GetString(buf.Take(length).ToArray()));
                }
            }
            return sb.ToString();
        }

        public OssHelper()
        {
        }
        
        public string PutStream(string filename, Stream stream)
        {
            client.PutObject(bucketName, filename, stream);
            return "true";
        }

    }
}
