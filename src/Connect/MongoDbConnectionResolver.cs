using System.Collections.Generic;
using System.Threading.Tasks;
using PipServices3.Commons.Config;
using PipServices3.Commons.Errors;
using PipServices3.Commons.Refer;
using PipServices3.Components.Auth;
using PipServices3.Components.Connect;

namespace PipServices3.MongoDb.Connect
{
    /// <summary>
    /// Helper class that resolves MongoDB connection and credential parameters,
    /// validates them and generates a connection URI.
    /// 
    /// It is able to process multiple connections to MongoDB cluster nodes.
    /// 
    /// ### Configuration parameters ###
    /// 
    /// connection(s):
    /// - discovery_key:               (optional) a key to retrieve the connection from <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a>
    /// - host:                        host name or IP address
    /// - port:                        port number (default: 27017)
    /// - database:                    database name
    /// - uri:                         resource URI or connection string with all parameters in it 
    /// 
    /// credential(s):
    /// - store_key:                   (optional) a key to retrieve the credentials from <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_auth_1_1_i_credential_store.html">ICredentialStore</a>
    /// - username:                    user name
    /// - password:                    user password
    /// 
    /// ### References ###
    /// 
    /// - *:discovery:*:*:1.0          (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a> services
    /// - *:credential-store:*:*:1.0   (optional) Credential stores to resolve credentials
    /// </summary>
    public class MongoDbConnectionResolver: IReferenceable, IConfigurable
    {
        /// <summary>
        /// The connections resolver.
        /// </summary>
        protected ConnectionResolver _connectionResolver = new ConnectionResolver();
        /// <summary>
        /// The credentials resolver.
        /// </summary>
        protected CredentialResolver _credentialResolver = new CredentialResolver();

        /// <summary>
        /// Sets references to dependent components.
        /// </summary>
        /// <param name="references">references to locate the component dependencies.</param>
        public void SetReferences(IReferences references)
        {
            _connectionResolver.SetReferences(references);
            _credentialResolver.SetReferences(references);
        }

        /// <summary>
        /// Configures component by passing configuration parameters.
        /// </summary>
        /// <param name="config">configuration parameters to be set.</param>
        public void Configure(ConfigParams config)
        {
            _connectionResolver.Configure(config, false);
            _credentialResolver.Configure(config, false);
        }

        private void ValidateConnection(string correlationId, ConnectionParams connection)
        {
            var uri = connection.Uri;
            if (uri != null) return;

            var host = connection.Host;
            if (host == null)
                throw new ConfigException(correlationId, "NO_HOST", "Connection host is not set");

            var port = connection.Port;
            if (port == 0)
                throw new ConfigException(correlationId, "NO_PORT", "Connection port is not set");

            var database = connection.GetAsNullableString("database");
            if (database == null)
                throw new ConfigException(correlationId, "NO_DATABASE", "Connection database is not set");
        }

        private void ValidateConnections(string correlationId, List<ConnectionParams> connections)
        {
            if (connections == null || connections.Count == 0)
                throw new ConfigException(correlationId, "NO_CONNECTION", "Database connection is not set");

            foreach (var connection in connections)
                ValidateConnection(correlationId, connection);
        }

        private string ComposeUri(List<ConnectionParams> connections, CredentialParams credential)
        {
            // If there is a uri then return it immediately
            foreach (var connection in connections)
            {
                var fullUri = connection.GetAsNullableString("uri");//connection.Uri;
                if (fullUri != null) return fullUri;
            }

            // Define hosts
            var hosts = "";
            foreach (var connection in connections)
            {
                var host = connection.Host;
                var port = connection.Port;

                if (hosts.Length > 0)
                    hosts += ",";
               hosts += host + (port == 0 ? "" : ":" + port);
            }

            // Define database
            var database = "";
            foreach (var connection in connections)
            {
                database = connection.GetAsNullableString("database") ?? database;
            }
            if (database.Length > 0)
                database = "/" + database;

            // Define authentication part
            var auth = "";
            if (credential != null)
            {
                var username = credential.Username;
                if (username != null)
                {
                    var password = credential.Password;
                    if (password != null)
                        auth = username + ":" + password + "@";
                    else
                        auth = username + "@";
                }
            }

            // Define additional parameters parameters
            var options = ConfigParams.MergeConfigs(connections.ToArray()).Override(credential);
            options.Remove("uri");
            options.Remove("host");
            options.Remove("port");
            options.Remove("database");
            options.Remove("username");
            options.Remove("password");
            var parameters = "";
            var keys = options.Keys;
            foreach (var key in keys)
            {
                if (parameters.Length > 0)
                    parameters += "&";

                parameters += key;

                var value = options.GetAsString(key);
                if (value != null)
                    parameters += "=" + value;
            }
            if (parameters.Length > 0)
                parameters = "?" + parameters;

            // Compose uri
            var uri = "mongodb://" + auth + hosts + database + parameters;

            return uri;
        }

        /// <summary>
        /// Resolves MongoDB connection URI from connection and credential parameters.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <returns>resolved URI.</returns>
        public async Task<string> ResolveAsync(string correlationId)
        {
            var connections = await _connectionResolver.ResolveAllAsync(correlationId);
            var credential = await _credentialResolver.LookupAsync(correlationId);

            ValidateConnections(correlationId, connections);

            return ComposeUri(connections, credential);
        }

    }
}
