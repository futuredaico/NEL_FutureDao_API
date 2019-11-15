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

        public string PutObject(string bucketName, string fileName, Stream stream)
        {
            getOss().PutStream(bucketName, fileName, stream);
            return ossUrlPrefix.Replace("#", bucketName) + fileName;
        }
        public void CopyObject(string bucketName, string source, string target)
        {
            getOss().CopyObject(bucketName, source, target);
        }
        public bool ExistKey(string bucketName, string filename)
        {
            return getOss().ExistKey(bucketName, filename);
        }
        public void DeleteObject(string bucketName, string filename)
        {
            getOss().DeleteObject(bucketName, filename);
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
            public bool ExistKey(string bucketName, string filename)
            {
                return client.DoesObjectExist(bucketName, filename);
            }
            public string DeleteObject(string bucketName, string filename)
            {
                client.DeleteObject(bucketName, filename);
                return "true";
            }
        }
    }
    
}
