using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using PipServices3.Commons.Config;
using PipServices3.Commons.Errors;
using PipServices3.Commons.Refer;
using PipServices3.Commons.Run;
using PipServices3.Components.Log;
using PipServices3.MongoDb.Connect;

namespace PipServices3.MongoDb.Persistence
{
    /// <summary>
    /// MongoDB connection using plain driver.
    /// 
    /// By defining a connection and sharing it through multiple persistence components
    /// you can reduce number of used database connections.
    /// 
    /// ### Configuration parameters ###
    /// 
    /// connection(s):
    /// - discovery_key:             (optional) a key to retrieve the connection from <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a>
    /// - host:                      host name or IP address
    /// - port:                      port number (default: 27017)
    /// - uri:                       resource URI or connection string with all parameters in it
    /// 
    /// credential(s):
    /// - store_key:                 (optional) a key to retrieve the credentials from <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/interface_pip_services_1_1_components_1_1_auth_1_1_i_credential_store.html">ICredentialStore</a>
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
    /// - *:logger:*:*:1.0           (optional) <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/interface_pip_services_1_1_components_1_1_log_1_1_i_logger.html">ILogger</a> components to pass log messages
    /// - *:discovery:*:*:1.0        (optional) <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a> services
    /// - *:credential-store:*:*:1.0 (optional) Credential stores to resolve credentials
    /// </summary>
    public class MongoDbConnection : IReferenceable, IReconfigurable, IOpenable
    {
        private ConfigParams _defaultConfig = ConfigParams.FromTuples(
            "options.sessions_supported", true
        );

        /// <summary>
        /// The connection resolver.
        /// </summary>
        protected MongoDbConnectionResolver _connectionResolver = new MongoDbConnectionResolver();

        /// <summary>
        /// The configuration options.
        /// </summary>
        protected ConfigParams _options = new ConfigParams();

        /// <summary>
        /// The MongoDB connection object.
        /// </summary>
        protected MongoClient _connection;

        /// <summary>
        /// The MongoDB database.
        /// </summary>
        protected IMongoDatabase _database;

        /// <summary>
        /// The database name.
        /// </summary>
        protected string _databaseName;

        /// <summary>
        /// The logger.
        /// </summary>
        protected CompositeLogger _logger = new CompositeLogger();


        /// <summary>
        /// Determines if the database supports sessions or not
        /// </summary>
        private bool _areSessionsSupported = true;

        /// <summary>
        /// Creates a new instance of the connection component.
        /// </summary>
        public MongoDbConnection()
        { }

        /// <summary>
        /// Gets MongoDB connection object.
        /// </summary>
        /// <returns>The MongoDB connection object.</returns>
        public MongoClient GetConnection()
        {
            return _connection;
        }

        /// <summary>
        /// Gets the reference to the connected database.
        /// </summary>
        /// <returns>The reference to the connected database.</returns>
        public IMongoDatabase GetDatabase()
        {
            return _database;
        }

        /// <summary>
        /// Gets the name of the connected database.
        /// </summary>
        /// <returns>The name of the connected database.</returns>
        public string GetDatabaseName()
        {
            return _databaseName;
        }

        /// <summary>
        /// Sets references to dependent components.
        /// </summary>
        /// <param name="references">references to locate the component dependencies.</param>
        public void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
            _connectionResolver.SetReferences(references);
        }

        /// <summary>
        /// Configures component by passing configuration parameters.
        /// </summary>
        /// <param name="config">configuration parameters to be set.</param>
        public virtual void Configure(ConfigParams config)
        {
            config = config.SetDefaults(_defaultConfig);

            _connectionResolver.Configure(config);

            _options = _options.Override(config.GetSection("options"));

            _areSessionsSupported = _options.GetAsBooleanWithDefault("sessions_supported", true);
        }

        /// <summary>
        /// Checks if the component is opened.
        /// </summary>
        /// <returns>true if the component has been opened and false otherwise.</returns>
        public virtual bool IsOpen()
        {
            return _connection != null && _database != null;
        }

        /// <summary>
        /// Opens the component.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public async virtual Task OpenAsync(string correlationId)
        {
            var uri = await _connectionResolver.ResolveAsync(correlationId);

            _logger.Trace(correlationId, "Connecting to mongodb...");

            try
            {
                _connection = new MongoClient(uri);
                _databaseName = MongoUrl.Create(uri).DatabaseName;
                _database = _connection.GetDatabase(_databaseName);

                // Check if connection is alive
                if (_areSessionsSupported)
                {
                    await _connection.StartSessionAsync();
                }

                _logger.Debug(correlationId, "Connected to mongodb database {0}", _databaseName);
            }
            catch (Exception ex)
            {
                throw new ConnectionException(correlationId, "ConnectFailed", "Connection to mongodb failed", ex);
            }

            await Task.Delay(0);
        }

        /// <summary>
        /// Closes component and frees used resources.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public async virtual Task CloseAsync(string correlationId)
        {
            // Todo: Properly close the connection
            _connection = null;
            _database = null;
            _databaseName = null;

            await Task.Delay(0);
        }
    }
}
