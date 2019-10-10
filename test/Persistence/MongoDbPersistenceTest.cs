using PipServices3.Commons.Config;
using System;
using Xunit;

namespace PipServices3.MongoDb.Persistence
{
    public class MongoDbPersistenceTest
    {
        private static MongoDbDummyPersistence Db { get; } = new MongoDbDummyPersistence();

        private string mongoUri;
        private string mongoHost;
        private string mongoPort;
        private string mongoDatabase;

        public MongoDbPersistenceTest()
        {
            mongoUri = Environment.GetEnvironmentVariable("MONGO_URI");
            mongoHost = Environment.GetEnvironmentVariable("MONGO_HOST") ?? "localhost";
            mongoPort = Environment.GetEnvironmentVariable("MONGO_PORT") ?? "27017";
            mongoDatabase = Environment.GetEnvironmentVariable("MONGO_DB") ?? "test";

            if (mongoUri == null && mongoHost == null)
                return;

            if (Db == null) return;
        }

        [Fact]
        public void TestOpenAsync_Success()
        {
            Db.Configure(ConfigParams.FromTuples(
                "connection.uri", mongoUri,
                "connection.host", mongoHost,
                "connection.port", mongoPort,
                "connection.database", mongoDatabase
            ));

            Db.OpenAsync(null).Wait();

            var actual = Db.IsOpen();

            Assert.True(actual);
        }

        [Fact]
        public void TestOpenAsync_Failure()
        {
            Db.Configure(ConfigParams.FromTuples(
                "connection.uri", mongoUri,
                "connection.host", mongoHost,
                "connection.port", "1234",
                "connection.database", mongoDatabase
            ));

            var ex = Assert.Throws<AggregateException>(() => Db.OpenAsync(null).Wait());
            Assert.Equal("Connection to mongodb failed", ex.InnerException.Message);
        }
    }
}
