# <img src="https://uploads-ssl.webflow.com/5ea5d3315186cf5ec60c3ee4/5edf1c94ce4c859f2b188094_logo.svg" alt="Pip.Services Logo" width="200"> <br/> MongoDB components for .NET

This module is a part of the [Pip.Services](http://pipservices.org) polyglot microservices toolkit.

The MongoDB module simplifies how we work with Mongo databases and contains everything you need to start working with MongoDB.

The module contains the following packages:
- **Build** - contains a factory for creating MongoDB persistence components.
- **Connect** - instruments for configuring connections to the database. The component receives a set of configuration parameters and uses them to generate all necessary database connection parameters.
- **Persistence** - abstract classes for working with the database that can be used for connecting to collections and performing basic CRUD operations.

<a name="links"></a> Quick links:

* [MongoDB persistence](https://www.pipservices.org/recipies/mongodb-persistence)
* [Data Microservice. Step 3](https://www.pipservices.org/docs/tutorials/data-microservice/persistence) 
* [Data Microservice. Step 6](https://www.pipservices.org/docs/tutorials/data-microservice/container) 
* [Data Microservice. Step 7](https://www.pipservices.org/docs/tutorials/data-microservice/run-and-test)
* [Configuration](https://www.pipservices.org/recipies/configuration)
* [API Reference](https://pip-services3-dotnet.github.io/pip-services3-mongodb-dotnet/)
* [Change Log](CHANGELOG.md)
* [Get Help](https://www.pipservices.org/community/help)
* [Contribute](https://www.pipservices.org/community/contribute)


## Use

Install the dotnet package as
```bash
dotnet add package PipServices3.Mongodb
```

As an example, lets create persistence for the following data object.

```cs
using PipServices3.Commons.Data;

class MyObject : IIdentifiable<string>
{
    public string Id { get; set; }
    public string key;
    public int value;
}

```

The persistence component shall implement the following interface with a basic set of CRUD operations.

```cs
interface IMyPersistance
{
    void GetPageByFilter(string correlationId, FilterParams filter, PagingParams paging);

    void GetOneById(string correlationId, string id);

    void GetOneByKey(string correlationId, string id);

    void Create(string correlationId, MyObject item);

    void Update(string correlationId, MyObject item);

    void DeleteById(string correlationId, string id);
}

```

To implement mongodb persistence component you shall inherit `IdentifiableMongoDbPersistence`. 
Most CRUD operations will come from the base class. You only need to override `GetPageByFilter` method with a custom filter function.
And implement a `GetOneByKey` custom persistence method that doesn't exist in the base class.

```cs
class MyMongoDbPersistence : IdentifiableMongoDbPersistence<MyObject, string>
{
    public MyMongoDbPersistence() : base("myobjects") { }

    override
    protected FilterDefinition<MyObject> ComposeFilter(FilterParams filter)
    {
        filter = filter != null ? filter : new FilterParams();

        var criteria = new BsonArray();

        string id = filter.GetAsNullableString("id");
        if (id != null)
            criteria.Add(new BsonDocument("_id", id));

        string tempIds = filter.GetAsNullableString("ids");
        if (tempIds != null)
        {
            string[] ids = tempIds.Split(",");
            criteria.Add(new BsonDocument(new Dictionary<string, object> { { "$in", ids } }));
        }

        string key = filter.GetAsNullableString("key");
        if (key != null)
            criteria.Add(new BsonDocument("key", key));


        return criteria.Count > 0 ? new BsonDocument("$and", criteria) : null;
    }

    DataPage<MyObject> GetPageByFilter(string correlationId, FilterParams filter, PagingParams paging)
    {
        return base.GetPageByFilterAsync(correlationId, this.ComposeFilter(filter), paging).Result;
    }

    void GetOneByKey(string correlationId, string key)
    {
        var filter = new BsonDocument("key", key);
        var item = this._collection.Find(filter).First<MyObject>();

        if (item == null)
            this._logger.Trace(correlationId, "Nothing found from %s with key = %s", this._collectionName, key);
        else
            this._logger.Trace(correlationId, "Retrieved from %s with key = %s", this._collectionName, key);
    }
}
```

Configuration for your microservice that includes mongodb persistence may look the following way.

```yaml
...
{{#if MONGODB_ENABLED}}
- descriptor: pip-services:connection:mongodb:con1:1.0
  collection: {{MONGO_COLLECTION}}{{#unless MONGO_COLLECTION}}myobjects{{/unless}}
  connection:
    uri: {{{MONGO_SERVICE_URI}}}
    host: {{{MONGO_SERVICE_HOST}}}{{#unless MONGO_SERVICE_HOST}}localhost{{/unless}}
    port: {{MONGO_SERVICE_PORT}}{{#unless MONGO_SERVICE_PORT}}27017{{/unless}}
    database: {{MONGO_DB}}{{#unless MONGO_DB}}app{{/unless}}
  credential:
    username: {{MONGO_USER}}
    password: {{MONGO_PASS}}
    
- descriptor: myservice:persistence:mongodb:default:1.0
  dependencies:
    connection: pip-services:connection:mongodb:con1:1.0
{{/if}}
...
```

## Develop

For development you shall install the following prerequisites:
* Core .NET SDK 3.1+
* Visual Studio Code or another IDE of your choice
* Docker

Restore dependencies:
```bash
dotnet restore src/src.csproj
```

Compile the code:
```bash
dotnet build src/src.csproj
```

Run automated tests:
```bash
dotnet restore test/test.csproj
dotnet test test/test.csproj
```

Generate API documentation:
```bash
./docgen.ps1
```

Before committing changes run dockerized build and test as:
```bash
./build.ps1
./test.ps1
./clear.ps1
```

## Contacts

The .NET version of Pip.Services is created and maintained by:
- **Sergey Seroukhov**
- **Alex Mazur**

