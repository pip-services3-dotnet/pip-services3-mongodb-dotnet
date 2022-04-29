using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using PipServices3.Commons.Config;
using PipServices3.Commons.Data;
using System;
using System.Threading.Tasks;

namespace PipServices3.MongoDb.Persistence
{
    /// <summary>
    /// Abstract persistence component that stores data in MongoDB
    /// and implements a number of CRUD operations over data items with partitionKey.
    /// The data items must implement IIdentifiable interface.
    /// 
    /// In basic scenarios child classes shall only override <c>GetPageByFilter()</c>,
    /// <c>GetListByFilter()</c> or <c>DeleteByFilter()</c> operations with specific filter function.
    /// All other operations can be used out of the box.
    /// 
    /// In complex scenarios child classes can implement additional operations by
    /// accessing <c>this._collection</c> and <c>this._model</c> properties.
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
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="K"></typeparam>
    /// <example>
    /// <code>
    /// class MyMongoDbPersistence: PartitionMongoDbPersistence<MyData, string> 
    /// {
    ///     public constructor()
    ///     {
    ///         base("mydata", MyData.class);
    ///     }
    /// 
    ///     private FilterDefinition<MyData> ComposeFilter(FilterParams filter)
    ///     {
    ///         filterParams = filterParams ?? new FilterParams();
    ///         var builder = Builders<BeaconV1>.Filter;
    ///         var filter = builder.Empty;
    ///         String name = filter.getAsNullableString('name');
    ///         if (name != null)
    ///             filter &= builder.Eq(b => b.Name, name);
    ///         return filter;
    ///     }
    ///     
    ///     public GetPageByFilter(String correlationId, FilterParams filter, PagingParams paging)
    ///     {
    ///         base.GetPageByFilter(correlationId, this.ComposeFilter(filter), paging, null, null);
    ///     }
    /// }
    /// 
    /// var persistence = new MyMongoDbPersistence();
    /// persistence.Configure(ConfigParams.fromTuples(
    /// "host", "localhost",
    /// "port", 27017 ));
    /// 
    /// persitence.Open("123");
    /// 
    /// persistence.Create("123", new MyData("1", "ABC"));
    /// var mydata = persistence.GetPageByFilter(
    /// "123",
    /// FilterParams.FromTuples("name", "ABC"),
    /// Console.Out.WriteLine(mydata.Data);          // Result: { id: "1", name: "ABC" }
    /// 
    /// persistence.DeleteById("123", "1");
    /// ...
    /// </code>
    /// </example>
    public abstract class PartitionMongoDbPersistence<T, K> : IdentifiableMongoDbPersistence<T, K>
        where T : IIdentifiable<K>
        where K : class
    {
        protected string _partitionKey;

        public PartitionMongoDbPersistence(string collectionName, string partitionKey)
            : base(collectionName)
        {
            _partitionKey = partitionKey;
        }

        protected abstract string GetPartitionKey(K id);

        public override void Configure(ConfigParams config)
        {
            base.Configure(config);
        }

        /// <summary>
        /// Modify a data item by it's unique id.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="id">an id of the item to be modify</param>
        /// <param name="updateDefinition">(optional) a JSON object to update</param>
        /// <returns>updated item.</returns>
        public override async Task<T> ModifyByIdAsync(string correlationId, K id, UpdateDefinition<T> updateDefinition)
        {
            if (id == null || updateDefinition == null)
            {
                return default(T);
            }

            var key = string.Empty;
            var filter = ComposePartitionFilter(id, out key);

            var result = await ModifyAsync(correlationId, filter, updateDefinition);

            _logger.Trace(correlationId, $"Modified in {_collectionName} with id = {id} and {_partitionKey} = {key}");

            return result;
        }

        /// <summary>
        /// Updates a data item.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="item">an item to be updated.</param>
        /// <returns>updated item.</returns>
        public override async Task<T> UpdateAsync(string correlationId, T item)
        {
            var identifiable = item as IIdentifiable<K>;
            if (identifiable == null || item.Id == null)
            {
                return default(T);
            }

            var key = string.Empty;
            var filter = ComposePartitionFilter(identifiable.Id, out key);

            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false
            };
            var result = await _collection.FindOneAndReplaceAsync(filter, item, options);

            _logger.Trace(correlationId, $"Updated in {_collectionName} with id = {identifiable.Id} and {_partitionKey} = {key}");

            return result;
        }

        /// <summary>
        /// Deleted a data item by it's unique id.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="id">an id of the item to be deleted</param>
        /// <returns>deleted item.</returns>
        public override async Task<T> DeleteByIdAsync(string correlationId, K id)
        {
            var key = string.Empty;
            var filter = ComposePartitionFilter(id, out key);

            var options = new FindOneAndDeleteOptions<T>();
            var result = await _collection.FindOneAndDeleteAsync(filter, options);

            _logger.Trace(correlationId, $"Deleted from {_collectionName} with id = {id} and {_partitionKey} = {key}");

            return result;
        }

        /// <summary>
        /// Sets a data item. If the data item exists it updates it, otherwise it create a new data item.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="item">an item to be set.</param>
        /// <returns>updated item.</returns>
        public override async Task<T> SetAsync(string correlationId, T item)
        {
            var identifiable = item as IIdentifiable<K>;
            if (identifiable == null || item.Id == null)
            {
                return default(T);
            }

            var key = string.Empty;
            var filter = ComposePartitionFilter(identifiable.Id, out key);

            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };
            var result = await _collection.FindOneAndReplaceAsync(filter, item, options);

            _logger.Trace(correlationId, $"Set in {_collectionName} with id = {identifiable.Id} and {_partitionKey} = {key}");

            return result;
        }

        /// <summary>
        /// Gets one first data item by filter.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="filterDefinition">(optional) a filter JSON object</param>
        /// <returns>a data item by id.</returns>
        public async Task<T> GetOneByFilterAsync(string correlationId, FilterDefinition<T> filterDefinition)
        {
            var result = await _collection.Find(filterDefinition).FirstOrDefaultAsync();

            if (result == null)
            {
                _logger.Trace(correlationId, $"GetOneByFilter: nothing found in {_collectionName} with filter.");
                return default(T);
            }

            _logger.Trace(correlationId, $"GetOneByFilter: retrieved first element from {_collectionName} by filter.");

            return result;
        }

        /// <summary>
        /// Gets one first data item by filter.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="filterDefinition">(optional) a filter JSON object</param>
        /// <param name="projection">(optional) projection parameters.</param>
        /// <returns>a data item by id.</returns>
        protected async Task<object> GetOneByFilterAsync(string correlationId, FilterDefinition<T> filterDefinition, ProjectionParams projection)
        {
            var projectionBuilder = Builders<T>.Projection;
            var projectionDefinition = CreateProjectionDefinition(projection, projectionBuilder);

            var result = await _collection.Find(filterDefinition).Project(projectionDefinition).FirstOrDefaultAsync();

            if (result == null || result.ElementCount == 0)
            {
                _logger.Trace(correlationId, $"GetOneByFilter: nothing found in {_collectionName} with filter.");
                return null;
            }

            _logger.Trace(correlationId, $"GetOneByFilter: retrieved first element from {_collectionName} by filter.");

            return BsonSerializer.Deserialize<object>(result);
        }

        protected virtual async Task<U> ExecuteWithRetriesAsync<U>(string correlationId, Func<Task<U>> invokeFunc, int maxRetries = 3)
        {
            for (var retry = 1; retry <= maxRetries; retry++)
            {
                try
                {
                    return await invokeFunc();
                }
                catch (MongoConnectionException mongoConnectionException)
                {
                    _logger.Error(correlationId, $"MongoConnectionException happened on {retry}/{maxRetries} attempt.");

                    if (retry >= maxRetries)
                    {
                        throw mongoConnectionException;
                    }

                    await Task.Delay(1000);
                }
                catch (Exception exception)
                {
                    throw exception;
                }
            }

            return await Task.FromResult(default(U));
        }

        protected virtual FilterDefinition<T> ComposePartitionFilter(K id, out string key)
        {
            key = GetPartitionKey(id);

            var builder = Builders<T>.Filter;
            var filter = builder.Empty;

            filter &= builder.Eq(x => x.Id, id);
            filter &= builder.Eq(_partitionKey, key);
            return filter;
        }
    }
}
