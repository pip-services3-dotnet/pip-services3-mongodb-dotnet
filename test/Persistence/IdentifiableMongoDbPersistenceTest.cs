using System;

using PipServices.Commons.Config;

using Xunit;

namespace PipServices.MongoDb.Persistence
{
    [Collection("Sequential")]
    public sealed class IdentifiableMongoDbPersistenceTest : IDisposable
    {
        private static MongoDbDummyPersistence Db { get; } = new MongoDbDummyPersistence();
        private static PersistenceFixture Fixture { get; set; }

        public IdentifiableMongoDbPersistenceTest()
        {
            var mongoUri = Environment.GetEnvironmentVariable("MONGO_URI");
            var mongoHost = Environment.GetEnvironmentVariable("MONGO_HOST") ?? "localhost";
            var mongoPort = Environment.GetEnvironmentVariable("MONGO_PORT") ?? "27017";
            var mongoDatabase = Environment.GetEnvironmentVariable("MONGO_DB") ?? "test";

            if (mongoUri == null && mongoHost == null)
                return;

            if (Db == null) return;

            Db.Configure(ConfigParams.FromTuples(
                "connection.uri", mongoUri,
                "connection.host", mongoHost,
                "connection.port", mongoPort,
                "connection.database", mongoDatabase
            ));

            Db.OpenAsync(null).Wait();
            Db.ClearAsync(null).Wait();

            Fixture = new PersistenceFixture(Db);
        }

        [Fact]
        public void TestCrudOperations()
        {
            Fixture?.TestCrudOperationsAsync().Wait();
        }

        [Fact]
        public void TestMultithreading()
        {
            Fixture?.TestMultithreading().Wait();
        }

        [Fact]
        public void It_Should_Not_Get_By_Wrong_Id_And_Projection()
        {
            Fixture?.TestGetByWrongIdAndProjection().Wait();
        }

        [Fact]
        public void It_Should_Get_By_Id_And_Projection()
        {
            Fixture?.TestGetByIdAndProjection().Wait();
        }

        [Fact]
        public void It_Should_Get_By_Id_And_Projection_From_Array()
        {
            Fixture?.TestGetByIdAndProjectionFromArray().Wait();
        }

        [Fact]
        public void It_Should_Get_By_Id_And_Wrong_Projection()
        {
            Fixture?.TestGetByIdAndWrongProjection().Wait();
        }

        [Fact]
        public void It_Should_Get_By_Id_And_Null_Projection()
        {
            Fixture?.TestGetByIdAndNullProjection().Wait();
        }

        [Fact]
        public void It_Should_Get_By_Id_And_Id_Projection()
        {
            Fixture?.TestGetByIdAndIdProjection().Wait();
        }

        [Fact]
        public void It_Should_Get_Page_By_Filter()
        {
            Fixture?.TestGetPageByFilter().Wait();
        }

        [Fact]
        public void It_Should_Get_Page_By_Projection()
        {
            Fixture?.TestGetPageByProjection().Wait();
        }

        [Fact]
        public void It_Should_Get_Page_By_Null_Projection()
        {
            Fixture?.TestGetPageByNullProjection().Wait();
        }

        [Fact]
        public void It_Should_Not_Get_Page_By_Wrong_Projection()
        {
            Fixture?.TestGetPageByWrongProjection().Wait();
        }

        [Fact]
        public void It_Should_Modify_Object_With_Existing_Properties_By_Selected_Fields()
        {
            Fixture?.TestModifyExistingPropertiesBySelectedFields().Wait();
        }

        [Fact]
        public void It_Should_Modify_Object_With_Existing_Properties_By_Selected_Not_Changed_Fields()
        {
            Fixture?.TestModifyExistingPropertiesBySelectedNotChangedFields().Wait();
        }

        [Fact]
        public void It_Should_Modify_Object_With_Null_Properties_By_Selected_Fields()
        {
            Fixture?.TestModifyExistingPropertiesBySelectedFields().Wait();
        }

        [Fact]
        public void It_Should_Modify_Nested_Collection_By_Selected_Fields()
        {
            Fixture?.TestModifyNestedCollectionBySelectedFields().Wait();
        }

        [Fact]
        public void It_Should_Search_Within_Nested_Collection_Filter()
        {
            Fixture?.TestSearchWithinNestedCollectionByFilter().Wait();
        }

        [Fact]
        public void It_Should_Search_Within_Nested_Collection_Filter_By_Null_Projection()
        {
            Fixture?.TestSearchWithinNestedCollectionByFilterAndNullProjection().Wait();
        }

        [Fact]
        public void It_Should_Search_Within_Deep_Nested_Collection_Filter()
        {
            Fixture?.TestSearchWithinDeepNestedCollectionByFilter().Wait();
        }

        [Fact]
        public void It_Should_Search_Within_Deep_Nested_Collection_Filter_By_Null_Projection()
        {
            Fixture?.TestSearchWithinDeepNestedCollectionByFilterAndNullProjection().Wait();
        }

        [Fact]
        public void It_Should_Modify_Nested_Collection()
        {
            Fixture?.TestModifyNestedCollection().Wait();
        }

        [Fact]
        public void It_Should_Get_Page_By_Ids_Filter()
        {
            Fixture?.TestGetPageByIdsFilter().Wait();
        }

        [Fact]
        public void It_Should_Get_Page_By_Array_Of_Keys_Filter()
        {
            Fixture?.TestGetPageByArrayOfKeysFilter().Wait();
        }

        [Fact]
        public void It_Should_Get_Page_Sorted_By_One_Field()
        {
            Fixture?.TestGetPageSortedByOneField().Wait();
        }

        [Fact]
        public void It_Should_Get_Sales_Orders_Sorted_By_Multiple_Fields()
        {
            Fixture?.TestGetPageSortedByMultipleFields().Wait();
        }

        [Fact]
        public void It_Should_Get_Sales_Orders_By_Projection_And_Sorted_By_One_Field()
        {
            Fixture?.TestGetPageByProjectionAndSortedByOneField().Wait();
        }

        [Fact]
        public void It_Should_Get_Sales_Orders_By_Projection_And_Sorted_By_Multiple_Fields()
        {
            Fixture?.TestGetPageByProjectionAndSortedByMultipleFields().Wait();
        }

        public void Dispose()
        {
            Db?.CloseAsync(null).Wait();
        }
    }
}
