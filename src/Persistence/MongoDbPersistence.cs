using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using PipServices3.Commons.Config;
using PipServices3.Commons.Data;
using PipServices3.Commons.Errors;
using PipServices3.Commons.Refer;
using PipServices3.Commons.Run;
using PipServices3.Components.Log;

namespace PipServices3.MongoDb.Persistence
{
    /// <summary>
    /// Abstract persistence component that stores data in MongoDB
    /// using the official MongoDB driver.
    /// 
    /// This is the most basic persistence component that is only
    /// able to store data items of any type.Specific CRUD operations 
    /// over the data items must be implemented in child classes by 
    /// accessing <c>this._collection</c> or <c>this._model</c> properties.
    /// 
    /// ### Configuration parameters ###
    /// 
    /// - collection:                  (optional) MongoDB collection name
    /// 
    /// connection(s):
    /// - discovery_key:             (optional) a key to retrieve the connection from <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a>
    /// - host:                      host name or IP address
    /// - port:                      port number (default: 27017)
    /// - uri:                       resource URI or connection string with all parameters in it
    /// 
    /// credential(s):
    /// - store_key:                 (optional) a key to retrieve the credentials from <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_auth_1_1_i_credential_store.html">ICredentialStore</a>
    /// - username:                  (optional) user name
    /// - password:                  (optional) user password
    /// 
    /// options:
    /// - max_pool_size:             (optional) maximum connection pool size (default: 2)
    /// - keep_alive:                (optional) enable connection keep alive (default: true)
    /// - connect_timeout:           (optional) connection timeout in milliseconds (default: 5 sec)
    /// - auto_reconnect:            (optional) enable auto reconnection (default: true)
    /// - max_page_size:             (optional) maximum page size (default: 100)
    /// - debug:                     (optional) enable debug output (default: false).
    /// 
    /// ### References ###
    /// 
    /// - *:logger:*:*:1.0           (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_log_1_1_i_logger.html">ILogger</a> components to pass log messages
    /// - *:discovery:*:*:1.0        (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a> services
    /// - *:credential-store:*:*:1.0 (optional) Credential stores to resolve credentials
    /// </summary>
    /// <typeparam name="T">the class type</typeparam>
    /// <example>
    /// <code>
    /// class MyMongoDbPersistence: MongoDbPersistence<MyData> 
    /// {
    ///     public MyMongoDbPersistence()
    ///     {
    ///         base("mydata");
    ///     }
    ///     public MyData getByName(string correlationId, string name)
    ///     {
    ///         var builder = Builders<BeaconV1>.Filter;
    ///         var filter = builder.Eq(x => x.Name, name);
    ///         var result = await _collection.Find(filter).FirstOrDefaultAsync();
    ///         return result;
    ///     }
    ///     public MyData set(String correlatonId, MyData item)
    ///     {
    ///         var filter = Builders<T>.Filter.Eq(x => x.Id, item.Id);
    ///         var options = new FindOneAndReplaceOptions<T>
    ///         {
    ///             ReturnDocument = ReturnDocument.After,
    ///             IsUpsert = true
    ///         };
    ///         var result = await _collection.FindOneAndReplaceAsync(filter, item, options);
    ///         return result;
    ///     }
    /// }
    /// 
    /// var persistence = new MyMongoDbPersistence();
    /// persistence.Configure(ConfigParams.fromTuples(
    /// "host", "localhost",
    /// "port", 27017 ));
    /// 
    /// persitence.Open("123");
    /// var mydata = new MyData("ABC");
    /// persistence.Set("123", mydata);
    /// persistence.GetByName("123", "ABC");
    /// Console.Out.WriteLine(item);                   // Result: { name: "ABC" }
    /// </code>
    /// </example>
    public class MongoDbPersistence<T> : IReferenceable, IUnreferenceable, IReconfigurable, IOpenable, ICleanable
    {
        private ConfigParams _defaultConfig = ConfigParams.FromTuples(
            "options.poll_size", 4,
            "options.keep_alive", 1,
            "options.connect_timeout", 5000,
            "options.auto_reconnect", true,
            "options.max_page_size", 100,
            "options.debug", true
        );

        /// <summary>
        /// The MongoDb connection.
        /// </summary>
        protected MongoDbConnection _connection;

        /// <summary>
        /// The MongoDB colleciton name.
        /// </summary>
        protected string _collectionName;
        
        /// <summary>
        /// The MongoDB client object.
        /// </summary>
        protected MongoClient _client;

        /// <summary>
        /// The MongoDB database.
        /// </summary>
        protected IMongoDatabase _database;

        /// <summary>
        /// The MongoDB colleciton object.
        /// </summary>
        protected IMongoCollection<T> _collection;

        /// <summary>
        /// Maximum page size
        /// </summary>
        protected int _maxPageSize = 100;

        /// <summary>
        /// The dependency resolver.
        /// </summary>
        protected DependencyResolver _dependencyResolver = new DependencyResolver(
            ConfigParams.FromTuples(
                "dependencies.connection", "pip-services:connection:mongodb:*:1.0"
            )
        );

        /// <summary>
        /// The logger.
        /// </summary>
        protected CompositeLogger _logger = new CompositeLogger();

        private ConfigParams _config;
        private IReferences _references;
        private bool _localConnection;
        private bool _opened;

        /// <summary>
        /// Creates a new instance of the persistence component.
        /// </summary>
        /// <param name="collectionName">(optional) a collection name.</param>
        public MongoDbPersistence(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentNullException(nameof(collectionName));

            _collectionName = collectionName;
        }

        /// <summary>
        /// Configures component by passing configuration parameters.
        /// </summary>
        /// <param name="config">configuration parameters to be set.</param>
        public virtual void Configure(ConfigParams config)
        {
            _config = config.SetDefaults(_defaultConfig);
            _dependencyResolver.Configure(_config);

            _collectionName = config.GetAsStringWithDefault("collection", _collectionName);
            _maxPageSize = config.GetAsIntegerWithDefault("options.max_page_size", _maxPageSize);
        }

        /// <summary>
        /// Sets references to dependent components.
        /// </summary>
        /// <param name="references">references to locate the component dependencies.</param>
        public virtual void SetReferences(IReferences references)
        {
            _references = references;

            _logger.SetReferences(references);
            _dependencyResolver.SetReferences(references);

            // Get connection
            _connection = _dependencyResolver.GetOneOptional("connection") as MongoDbConnection;
            _localConnection = _connection == null;

            // Or create a local one
            if (_connection == null)
                _connection = CreateLocalConnection();
        }

        /// <summary>
        /// Unsets (clears) previously set references to dependent components.
        /// </summary>
        public virtual void UnsetReferences()
        {
            _connection = null;
        }

        private MongoDbConnection CreateLocalConnection()
        {
            var connection = new MongoDbConnection();

            if (_config != null)
                connection.Configure(_config);

            if (_references != null)
                connection.SetReferences(_references);

            return connection;
        }

        /// <summary>
        /// Checks if the component is opened.
        /// </summary>
        /// <returns>true if the component has been opened and false otherwise.</returns>
        public virtual bool IsOpen()
        {
            return _opened;
        }

        /// <summary>
        /// Opens the component.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public async virtual Task OpenAsync(string correlationId)
        {
            if (IsOpen()) return;

            if (_connection == null)
            {
                _connection = CreateLocalConnection();
                _localConnection = true;
            }

            if (_localConnection)
                await _connection.OpenAsync(correlationId);

            if (_connection.IsOpen() == false)
                throw new InvalidStateException(correlationId, "CONNECTION_NOT_OPENED", "Database connection is not opened");

            _database = _connection.GetDatabase();
            _collection = _database.GetCollection<T>(_collectionName);
            _opened = true;
        }

        /// <summary>
        /// Closes component and frees used resources.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public virtual async Task CloseAsync(string correlationId)
        {
            if (IsOpen())
            {
                if (_connection == null)
                    throw new InvalidStateException(correlationId, "NO_CONNECTION", "MongoDb connection is missing");

                _opened = false;

                if (_localConnection)
                    await _connection.CloseAsync(correlationId);
            }
        }

        /// <summary>
        /// Clears component state.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public virtual async Task ClearAsync(string correlationId)
        {
            await _database.DropCollectionAsync(_collectionName);
        }

        /// <summary>
        /// Gets a page of data items retrieved by a given filter and sorted according to sort parameters.
        /// 
        /// This method shall be called by a public getPageByFilter method from child
        /// class that receives FilterParams and converts them into a filter function.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="filterDefinition">(optional) a filter JSON object</param>
        /// <param name="paging">(optional) paging parameters</param>
        /// <param name="sortDefinition">(optional) sorting JSON object</param>
        /// <returns>data page of results by filter.</returns>
        public virtual async Task<DataPage<T>> GetPageByFilterAsync(string correlationId, FilterDefinition<T> filterDefinition,
            PagingParams paging = null, SortDefinition<T> sortDefinition = null)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            var query = _collection.Find(renderedFilter);
            if (sortDefinition != null)
                query = query.Sort(sortDefinition);

            paging = paging ?? new PagingParams();
            var skip = paging.GetSkip(0);
            var take = paging.GetTake(_maxPageSize);

            var count = paging.Total ? (long?)await query.CountDocumentsAsync() : null;
            var items = await query.Skip((int)skip).Limit((int)take).ToListAsync();

            _logger.Trace(correlationId, $"Retrieved {items.Count} from {_collection}");

            return new DataPage<T>()
            {
                Data = items,
                Total = count
            };
        }

        /// <summary>
        /// Gets a list of data items retrieved by a given filter and sorted according to sort parameters.
        /// 
        /// This method shall be called by a public getListByFilter method from child
        /// class that receives FilterParams and converts them into a filter function.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="filterDefinition">(optional) a filter JSON object</param>
        /// <param name="sortDefinition">(optional) sorting JSON object</param>
        /// <returns></returns>
        public virtual async Task<List<T>> GetListByFilterAsync(string correlationId, FilterDefinition<T> filterDefinition,
            SortDefinition<T> sortDefinition = null)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            var query = _collection.Find(renderedFilter);
            if (sortDefinition != null)
                query = query.Sort(sortDefinition);

            var items = await query.ToListAsync();

            _logger.Trace(correlationId, $"Retrieved {items.Count} from {_collection}");

            return items;
        }

        /// <summary>
        /// Gets a random item from items that match to a given filter.
        /// 
        /// This method shall be called by a public getOneRandom method from child class
        /// that receives FilterParams and converts them into a filter function.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="filterDefinition">(optional) a filter JSON object</param>
        /// <returns>a random item by filter.</returns>
        public virtual async Task<T> GetOneRandomAsync(string correlationId, FilterDefinition<T> filterDefinition)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            var count = (int)_collection.CountDocuments(renderedFilter);

            if (count <= 0)
            {
                _logger.Trace(correlationId, "Nothing found for filter {0}", renderedFilter.ToString());
                return default(T);
            }

            var randomIndex = new Random().Next(0, count - 1);

            var result = await _collection.Find(filterDefinition).Skip(randomIndex).FirstOrDefaultAsync();

            _logger.Trace(correlationId, "Retrieved randomly from {0}", _collectionName);

            return result;
        }

        /// <summary>
        /// Creates a data item.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="item">an item to be created.</param>
        /// <returns>created item.</returns>
        public virtual async Task<T> CreateAsync(string correlationId, T item)
        {
            await _collection.InsertOneAsync(item, null);

            _logger.Trace(correlationId, "Created in {0} item {1}", _collectionName, item);

            return item;
        }

        /// <summary>
        /// Deletes data items that match to a given filter.
        /// 
        /// This method shall be called by a public deleteByFilter method from child
        /// class that receives FilterParams and converts them into a filter function.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="filterDefinition">(optional) a filter JSON object.</param>
        public virtual async Task DeleteByFilterAsync(string correlationId, FilterDefinition<T> filterDefinition)
        {
            var result = await _collection.DeleteManyAsync(filterDefinition);

            _logger.Trace(correlationId, $"Deleted {result.DeletedCount} from {_collection}");
        }


    }
}
