
using System;
using System.Threading.Tasks;

using MongoDB.Driver;

using PipServices3.Commons.Data;

namespace PipServices3.MongoDb.Persistence
{
    public class MongoDbDummyPersistence : IdentifiableMongoDbPersistence<Dummy, string>, IDummyPersistence
    {
        public MongoDbDummyPersistence()
            : base("dummies")
        {
        }

        public async Task ClearAsync()
        {
            await ClearAsync(null);
        }

        public async Task<Dummy> DeleteAsync(string correlationId, string id)
        {
            return await DeleteByIdAsync(correlationId, id);
        }

        public async Task<DataPage<Dummy>> GetAsync(string correlationId, FilterParams filter, PagingParams paging, SortParams sort)
        {
            return await GetPageByFilterAsync(correlationId, ComposeFilter(filter), paging, ComposeSort(sort));
        }

        public async Task<DataPage<object>> GetAsync(string correlationId, FilterParams filter, PagingParams paging, SortParams sort, ProjectionParams projection)
        {
            return await GetPageByFilterAndProjectionAsync(correlationId, ComposeFilter(filter), paging, ComposeSort(sort), projection);
        }

        public async Task<Dummy> GetByIdAsync(string correlationId, string id)
        {
            return await GetOneByIdAsync(correlationId, id);
        }

        public Task<long> GetCountAsync(string correlationId, FilterParams filterParams)
        {
            return GetCountByFilterAsync(correlationId, ComposeFilter(filterParams));
        }

        public async Task<object> GetByIdAsync(string correlationId, string id, ProjectionParams projection)
        {
            return await GetOneByIdAsync(correlationId, id, projection);
        }

        public async Task<Dummy> ModifyAsync(string correlationId, string id, AnyValueMap updateMap)
        {
            return await ModifyByIdAsync(correlationId, id, ComposeUpdate(updateMap));
        }

        protected override FilterDefinition<Dummy> ComposeFilter(FilterParams filterParams)
        {
            filterParams = filterParams ?? new FilterParams();

            var builder = Builders<Dummy>.Filter;
            var filter = builder.Empty;

            foreach (var filterKey in filterParams.Keys)
            {
                if (filterKey.Equals("ids"))
                {
                    filter &= builder.In(s => s.Id, ToArrayOfType<string>(filterParams.GetAsNullableString("ids")));
                    continue;
                }

                var filterParam = filterParams[filterKey];

                filter &= IsArray(filterParam) ? builder.In(filterKey, ToArrayOfType<string>(filterParam)) :
                    builder.Eq(filterKey, filterParam);
            }

            return filter;
        }

        protected static TT[] ToArrayOfType<TT>(string value)
        {
            if (value == null)
            {
                return null;
            }

            var items = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries) as TT[];
            return (items != null && items.Length > 0) ? items : null;
        }

        protected static bool IsArray(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.Split(',').Length > 1;
        }
    }
}