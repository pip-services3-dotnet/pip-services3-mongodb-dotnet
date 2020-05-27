using PipServices3.Commons.Refer;
using PipServices3.Components.Build;
using PipServices3.MongoDb.Persistence;

namespace PipServices3.MongoDb.Build
{
    /// <summary>
    /// Creates MongoDB components by their descriptors.
    /// </summary>
    /// See <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/class_pip_services_1_1_components_1_1_build_1_1_factory.html">Factory</a>, 
    /// <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-mongodb-dotnet/master/doc/api/class_pip_services_1_1_mongodb_1_1_persistence_1_1_mongo_db_connection.html">MongoDbConnection</a>
    public class DefaultMongoDbFactory : Factory
    {
        public static Descriptor Descriptor = new Descriptor("pip-services", "factory", "mongodb", "default", "1.0");
        public static Descriptor Descriptor3 = new Descriptor("pip-services3", "factory", "mongodb", "default", "1.0");
        public static Descriptor MongoDbConnection3Descriptor = new Descriptor("pip-services3", "connection", "mongodb", "*", "1.0");
        public static Descriptor MongoDbConnectionDescriptor = new Descriptor("pip-services", "connection", "mongodb", "*", "1.0");

        /// <summary>
        /// Create a new instance of the factory.
        /// </summary>
        public DefaultMongoDbFactory()
        {
            RegisterAsType(MongoDbConnection3Descriptor, typeof(MongoDbConnection));
            RegisterAsType(MongoDbConnectionDescriptor, typeof(MongoDbConnection));
        }
    }
}
