using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MongoDB.Bson.Serialization;
using MongoDB.Driver;

using PipServices.Commons.Config;
using PipServices.Commons.Convert;
using PipServices.Commons.Data;
using PipServices.Commons.Reflect;
using PipServices.Data;

namespace PipServices.MongoDb.Persistence
{
    public class IdentifiableMongoDbPersistence<T, K> : MongoDbPersistence<T>, IWriter<T, K>, IGetter<T, K>, ISetter<T>
        where T : IIdentifiable<K>
        where K : class
    {
        protected int _maxPageSize = 100;

        protected const string InternalIdFieldName = "_id";

        public IdentifiableMongoDbPersistence(string collectionName)
            : base(collectionName)
        { }

        public override void Configure(ConfigParams config)
        {
            base.Configure(config);

            _maxPageSize = config.GetAsIntegerWithDefault("options.max_page_size", _maxPageSize);
        }

        public async Task<DataPage<T>> GetPageByFilterAsync(string correlationId, FilterDefinition<T> filterDefinition,
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

            var count = paging.Total ? (long?)await query.CountAsync() : null;
            var items = await query.Skip((int)skip).Limit((int)take).ToListAsync();

            _logger.Trace(correlationId, $"Retrieved {items.Count} from {_collection}");

            return new DataPage<T>()
            {
                Data = items,
                Total = count
            };
        }

        public virtual async Task<DataPage<object>> GetPageByFilterAndProjectionAsync(string correlationId, FilterDefinition<T> filterDefinition,
            PagingParams paging = null, SortDefinition<T> sortDefinition = null, ProjectionParams projection = null)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            var query = _collection.Find(renderedFilter);
            if (sortDefinition != null)
            {
                query = query.Sort(sortDefinition);
            }

            var projectionBuilder = Builders<T>.Projection;
            var projectionDefinition = CreateProjectionDefinition(projection, projectionBuilder);

            paging = paging ?? new PagingParams();
            var skip = paging.GetSkip(0);
            var take = paging.GetTake(_maxPageSize);

            var count = paging.Total ? (long?)await query.CountAsync() : null;
            var items = await query.Project(projectionDefinition).Skip((int)skip).Limit((int)take).ToListAsync();

            var result = new DataPage<object>()
            {
                Data = new List<object>(),
                Total = count
            };

            using (var cursor = await query.Project(projectionDefinition).Skip((int)skip).Limit((int)take).ToCursorAsync())
            {
                while (await cursor.MoveNextAsync())
                {
                    foreach (var doc in cursor.Current)
                    {
                        if (doc.ElementCount != 0)
                        {
                            result.Data.Add(BsonSerializer.Deserialize<object>(doc));
                        }
                    }
                }
            }

            _logger.Trace(correlationId, $"Retrieved {result.Total} from {_collection} with projection fields = '{StringConverter.ToString(projection)}'");

            return result;
        }

        public async Task<List<T>> GetListByFilterAsync(string correlationId, FilterDefinition<T> filterDefinition,
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

        public async Task<List<T>> GetListByIdsAsync(string correlationId, K[] ids)
        {
            
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var builder = Builders<T>.Filter;
            var filterDefinition = builder.In(x => x.Id, ids);
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            var query = _collection.Find(renderedFilter);
            var items = await query.ToListAsync();

            _logger.Trace(correlationId, $"Retrieved {items.Count} from {_collection}");

            return items;
        }


        public async Task<T> GetOneByIdAsync(string correlationId, K id)
        {
            var builder = Builders<T>.Filter;
            var filter = builder.Eq(x => x.Id, id);
            var result = await _collection.Find(filter).FirstOrDefaultAsync();

            if (result == null)
            {
                _logger.Trace(correlationId, "Nothing found from {0} with id = {1}", _collectionName, id);
                return default(T);
            }

            _logger.Trace(correlationId, "Retrieved from {0} with id = {1}", _collectionName, id);

            return result;
        }

        public virtual async Task<object> GetOneByIdAsync(string correlationId, K id, ProjectionParams projection)
        {
            var builder = Builders<T>.Filter;
            var filter = builder.Eq(x => x.Id, id);

            var projectionBuilder = Builders<T>.Projection;
            var projectionDefinition = CreateProjectionDefinition(projection, projectionBuilder);

            var result = await _collection.Find(filter).Project(projectionDefinition).FirstOrDefaultAsync();

            if (result == null)
            {
                _logger.Trace(correlationId, "Nothing found from {0} with id = {1} and projection fields '{2}'", _collectionName, id, StringConverter.ToString(projection));
                return null;
            }

            if (result.ElementCount == 0)
            {
                _logger.Trace(correlationId, "Retrieved from {0} with id = {1}, but projection is not valid '{2}'", _collectionName, id, StringConverter.ToString(projection));
                return null;
            }

            _logger.Trace(correlationId, "Retrieved from {0} with id = {1} and projection fields '{2}'", _collectionName, id, StringConverter.ToString(projection));

            return BsonSerializer.Deserialize<object>(result);
        }

        public async Task<T> GetOneRandomAsync(string correlationId, FilterDefinition<T> filterDefinition)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            var count = (int)_collection.Count(renderedFilter);

            if (count <= 0)
            {
                _logger.Trace(correlationId, "Nothing found for filter {0}", renderedFilter.ToString());
                return default(T);
            }

            var randomIndex = new Random().Next(0, count - 1);

            var result = await _collection.Find(filterDefinition).Skip(randomIndex).FirstOrDefaultAsync();

            _logger.Trace(correlationId, "Retrieved randomly from {0} with id = {1}", _collectionName, result.Id);

            return result;
        }

        public async Task<T> CreateAsync(string correlationId, T item)
        {
            var identifiable = item as IStringIdentifiable;
            if (identifiable != null && item.Id == null)
                ObjectWriter.SetProperty(item, nameof(item.Id), IdGenerator.NextLong());

            await _collection.InsertOneAsync(item, null);

            _logger.Trace(correlationId, "Created in {0} with id = {1}", _collectionName, item.Id);

            return item;
        }

        public async Task<T> SetAsync(string correlationId, T item)
        {
            var identifiable = item as IIdentifiable<K>;
            if (identifiable == null || item.Id == null)
                return default(T);

            var filter = Builders<T>.Filter.Eq(x => x.Id, identifiable.Id);
            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };
            var result = await _collection.FindOneAndReplaceAsync(filter, item, options);

            _logger.Trace(correlationId, "Set in {0} with id = {1}", _collectionName, item.Id);

            return result;
        }

        public async Task<T> UpdateAsync(string correlationId, T item)
        {
            var identifiable = item as IIdentifiable<K>;
            if (identifiable == null || item.Id == null)
                return default(T);

            var filter = Builders<T>.Filter.Eq(x => x.Id, identifiable.Id);
            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false
            };
            var result = await _collection.FindOneAndReplaceAsync(filter, item, options);

            _logger.Trace(correlationId, "Update in {0} with id = {1}", _collectionName, item.Id);

            return result;
        }

        public async Task<T> ModifyAsync(string correlationId, FilterDefinition<T> filterDefinition, UpdateDefinition<T> updateDefinition)
        {
            if (filterDefinition == null || updateDefinition == null)
            {
                return default(T);
            }

            var options = new FindOneAndUpdateOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false
            };

            var result = await _collection.FindOneAndUpdateAsync(filterDefinition, updateDefinition, options);

            _logger.Trace(correlationId, "Modify in {0}", _collectionName);

            return result;
        }

        public async Task<T> ModifyByIdAsync(string correlationId, K id, UpdateDefinition<T> updateDefinition)
        {
            if (id == null || updateDefinition == null)
            {
                return default(T);
            }

            var result = await ModifyAsync(correlationId, Builders<T>.Filter.Eq(x => x.Id, id), updateDefinition);

            _logger.Trace(correlationId, "Modify in {0} with id = {1}", _collectionName, id);

            return result;
        }

        public async Task<T> DeleteByIdAsync(string correlationId, K id)
        {
            var filter = Builders<T>.Filter.Eq(x => x.Id, id);
            var options = new FindOneAndDeleteOptions<T>();
            var result = await _collection.FindOneAndDeleteAsync(filter, options);

            _logger.Trace(correlationId, "Deleted from {0} with id = {1}", _collectionName, id);

            return result;
        }

        public async Task DeleteByFilterAsync(string correlationId, FilterDefinition<T> filterDefinition)
        {
            var result = await _collection.DeleteManyAsync(filterDefinition);

            _logger.Trace(correlationId, $"Deleted {result.DeletedCount} from {_collection}");
        }

        public async Task DeleteByIdsAsync(string correlationId, K[] ids)
        {
            var filterDefinition = Builders<T>.Filter.In(x => x.Id, ids);

            var result = await _collection.DeleteManyAsync(filterDefinition);

            _logger.Trace(correlationId, $"Deleted {result.DeletedCount} from {_collection}");
        }

        #region Overridable Compose Methods

        // !! Hack, Remove !!

        protected virtual FilterDefinition<T> ComposeFilter(FilterParams filterParams)
        {
            filterParams = filterParams ?? new FilterParams();

            var builder = Builders<T>.Filter;
            var filter = builder.Empty;

            foreach (var filterKey in filterParams.Keys)
            {
                if (filterKey.Equals("ids"))
                {
                    filter &= builder.In(s => s.Id, ToArrayOfType<K>(filterParams.GetAsNullableString("ids")));
                    continue;
                }

                filter &= builder.Eq(filterKey, filterParams[filterKey]);
            }

            return filter;
        }

        protected virtual UpdateDefinition<T> ComposeUpdate(AnyValueMap updateMap)
        {
            updateMap = updateMap ?? new AnyValueMap();

            var builder = Builders<T>.Update;
            var updateDefinitions = new List<UpdateDefinition<T>>();

            foreach (var key in updateMap.Keys)
            {
                updateDefinitions.Add(builder.Set(key, updateMap[key]));
            }

            return builder.Combine(updateDefinitions);
        }

        protected virtual ProjectionDefinition<T> CreateProjectionDefinition(ProjectionParams projection, ProjectionDefinitionBuilder<T> projectionBuilder)
        {
            projection = projection ?? new ProjectionParams();

            return projectionBuilder.Combine(projection.Select(field => projectionBuilder.Include(field))).Exclude(InternalIdFieldName);
        }

        #endregion

        #region Helper Methods

        // !! Hack, Remove !!
        protected static TT[] ToArrayOfType<TT>(string value)
        {
            if (value == null)
            {
                return null;
            }

            var items = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries) as TT[];
            return (items != null && items.Length > 0) ? items : null;
        }

        #endregion

    }
}