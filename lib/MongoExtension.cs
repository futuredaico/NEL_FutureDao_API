using MongoDB.Bson;
using MongoDB.Driver;
using NEL.NNS.lib;

namespace NEL_FutureDao_API.lib
{
    public static class MongoExtension
    {
        public static long GetDataCountBson(this MongoHelper mh, string mongodbConnStr, string mongodbDatabase, string coll, BsonDocument findBson)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            var txCount = collection.Find(findBson).CountDocuments();

            client = null;

            return txCount;
        }

        public static BsonDecimal128 format(this decimal value)
        {
            return BsonDecimal128.Create(value);
        }

        public static decimal format(this BsonDecimal128 value)
        {
            return value.AsDecimal;
        }
    }
}
