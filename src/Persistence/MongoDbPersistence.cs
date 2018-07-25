using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using PipServices.Commons.Config;
using PipServices.Commons.Errors;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Components.Log;
using PipServices.MongoDb.Connect;

namespace PipServices.MongoDb.Persistence
{
    public class MongoDbPersistence2<T> : IReferenceable, IReconfigurable, IOpenable, ICleanable
    {
        private ConfigParams _defaultConfig = ConfigParams.FromTuples(
            //"connection.type", "mongodb",
            //"connection.database", "test",
            //"connection.host", "localhost",
            //"connection.port", 27017,

            //"options.poll_size", 4,
            //"options.keep_alive", 1,
            //"options.connect_timeout", 5000,
            //"options.auto_reconnect", true,
            //"options.max_page_size", 100,
            //"options.debug", true
        );

        protected string _collectionName;
        protected MongoDbConnectionResolver _connectionResolver = new MongoDbConnectionResolver();
        protected ConfigParams _options = new ConfigParams();

        protected MongoClient _connection;
        protected IMongoDatabase _database;
        protected IMongoCollection<T> _collection;

        protected CompositeLogger _logger = new CompositeLogger();

        public MongoDbPersistence2(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentNullException(nameof(collectionName));

            _collectionName = collectionName;
        }

        public void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
            _connectionResolver.SetReferences(references);
        }

        public void Configure(ConfigParams config)
        {
            config = config.SetDefaults(_defaultConfig);

            _connectionResolver.Configure(config);

            _collectionName = config.GetAsStringWithDefault("collection", _collectionName);

            _options = _options.Override(config.GetSection("options"));
        }

        public bool IsOpened()
        {
            return _collection != null;
        }

        public async virtual Task OpenAsync(string correlationId)
        {
            var uri = await _connectionResolver.ResolveAsync(correlationId);

            _logger.Trace(correlationId, "Connecting to mongodb");

            try
            {
                _connection = new MongoClient(uri);
                var databaseName = MongoUrl.Create(uri).DatabaseName;
                _database = _connection.GetDatabase(databaseName);
                _collection = _database.GetCollection<T>(_collectionName);

                _logger.Debug(correlationId, "Connected to mongodb database {0}, collection {1}", databaseName, _collectionName);
            }
            catch (Exception ex)
            {
                throw new ConnectionException(correlationId, "ConnectFailed", "Connection to mongodb failed", ex);
            }

            await Task.Delay(0);
        }

        public Task CloseAsync(string correlationId)
        {
            return Task.Delay(0);
        }

        public Task ClearAsync(string correlationId)
        {
            return _database.DropCollectionAsync(_collectionName);
        }
    }
}