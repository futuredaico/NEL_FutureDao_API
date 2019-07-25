using Aliyun.OSS;
using System.IO;

namespace NEL_FutureDao_API
{
    public class OssHelper
    {
        public string endpoint { get; set; }
        public string accessKeyId { get; set; }
        public string accessKeySecret { get; set; }
        public string bucketName { get; set; }
        public string bucketName_testnet { get; set; }
        public string bucketName_mainnet { get; set; }
        public string ossUrlPrefix { get; set; }

        public string PutObjectTestnet(string fileName, Stream stream)
        {
            getOss().PutStream(bucketName_testnet, fileName, stream);
            return ossUrlPrefix;
        }
        public string PutObjectMainnet(string fileName, Stream stream)
        {
            getOss().PutStream(bucketName_mainnet, fileName, stream);
            return ossUrlPrefix;
        }
        public void CopyObject(string bucketName, string source, string target)
        {
            getOss().CopyObject(bucketName, source, target);
        }

        private OssWraper getOss()
        {
            return new OssWraper(endpoint, accessKeyId, accessKeySecret);
        }
        private class OssWraper
        {
            private OssClient client;

            public OssWraper(string endpoint, string accessKeyId, string accessKeySecret)
            {
                client = new OssClient(endpoint, accessKeyId, accessKeySecret);
            }

            public string PutStream(string bucketName, string filename, Stream stream)
            {
                client.PutObject(bucketName, filename, stream);
                return "true";
            }
            public string CopyObject(string bucketName, string source, string target)
            {
                client.CopyObject(new CopyObjectRequest(bucketName, source, bucketName, target) { NewObjectMetadata = new ObjectMetadata() });
                return "true";
            }
        }
    }
    
}
