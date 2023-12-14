# <img src="https://uploads-ssl.webflow.com/5ea5d3315186cf5ec60c3ee4/5edf1c94ce4c859f2b188094_logo.svg" alt="Pip.Services Logo" width="200"> <br/> MongoDB components for .NET Changelog

## <a name="3.6.1-3.6.2"></a> 3.6.1-3.6.2 (2023-12-14)

### Breaking Changes
* Updated MongoDB.Driver version

## <a name="3.6.0"></a> 3.6.0 (2022-08-04)

### Breaking Changes
* Migrate to .NET 6.0

## <a name="3.5.1-3.5.2"></a> 3.5.1-3.5.2 (2022-01-21)

### Features
* **persistence** added GetCountByFilterAsync request
* **persistence** added UpdatePartiallyAsync for IdentifiableMongoDbPersistence

## <a name="3.4.0"></a> 3.4.0 (2021-09-01)

### Breaking Changes
* Migrate to .NET 5.0

## <a name="3.4.0"></a> 3.4.0 (2021-06-11) 

### Features
* Updated references as PipServices3.Components have got minor changes

## <a name="3.3.2"></a> 3.3.2 (2021-05-12) 

### Features
* Added PartitionMongoDbPersistence

## <a name="3.3.0"></a> 3.3.0 (2020-07-14) 

### Features
* Moved some CRUD operations from IdentifiableMongoDbPersistence to MongoDbPersistence

## <a name="3.2.1"></a> 3.2.1 (2020-06-26)

### Features
* Implemented support backward compatibility

## <a name="3.2.0"></a> 3.2.0 (2020-05-26)

### Breaking Changes
* Migrated to .NET Core 3.1

## <a name="3.1.0-3.1.1"></a> 3.1.0-3.1.1 (2019-12-13)

### Features
* Added MongoDbConnection

## <a name="3.0.0-3.0.5"></a> 3.0.0-3.0.5 (2019-10-02)

### Bug Fixes
* Fixed typos

### Breaking Changes
* Moved to a separate package

## <a name="1.1.0-1.1.32"></a> 1.1.0-1.1.32 (2018-07-23)

* Moved MockDb persistence to PipServices3.Data

### Features
* **cache** Added Memcached and Redis clients
* **mongodb** Integrated with projections
* **prometheus** Added PrometheusCounters and PrometheusMetrisService

## <a name="1.0.0"></a> 1.0.0 (2018-03-21)

Initial public release

### Features
* **mongodb** Added MongoDbConnectionResolver
* **mongodb** Added MongoDbPersistence
* **mongodb** Added IdentifiableMongoDbPersistence
* **elasticsearch** Added ElasticSearchLogger

### Bug Fixes
No fixes in this version

