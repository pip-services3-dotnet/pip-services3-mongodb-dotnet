using System.Collections.Generic;
using System.Threading.Tasks;
using PipServices.Commons.Config;
using PipServices.Commons.Errors;
using PipServices.Commons.Refer;
using PipServices.Components.Auth;
using PipServices.Components.Connect;

namespace PipServices.MongoDb.Connect
{
    public class MongoDbConnectionResolver: IReferenceable, IConfigurable
    {
        protected ConnectionResolver _connectionResolver = new ConnectionResolver();
        protected CredentialResolver _credentialResolver = new CredentialResolver();

        public void SetReferences(IReferences references)
        {
            _connectionResolver.SetReferences(references);
            _credentialResolver.SetReferences(references);
        }

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

        public async Task<string> ResolveAsync(string correlationId)
        {
            var connections = await _connectionResolver.ResolveAllAsync(correlationId);
            var credential = await _credentialResolver.LookupAsync(correlationId);

            ValidateConnections(correlationId, connections);

            return ComposeUri(connections, credential);
        }

    }
}
