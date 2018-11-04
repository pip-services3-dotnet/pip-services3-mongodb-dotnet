using MongoDB.Driver;

using PipServices3.Commons.Data;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices3.MongoDb.Persistence
{
    public interface IDummyPersistence
    {
        Task<Dummy> ModifyAsync(string correlationId, string id, AnyValueMap updateMap);
        Task<Dummy> DeleteAsync(string correlationId, string id);
        Task<Dummy> GetByIdAsync(string correlationId, string id);
        Task<object> GetByIdAsync(string correlationId, string id, ProjectionParams projection);
        Task<DataPage<Dummy>> GetAsync(string correlationId, FilterParams filter, PagingParams paging, SortParams sort);
        Task<DataPage<object>> GetAsync(string correlationId, FilterParams filter, PagingParams paging, SortParams sort, ProjectionParams projection);
        Task ClearAsync();

        Task<Dummy> CreateAsync(string correlationId, Dummy item);
        Task DeleteByFilterAsync(string correlationId, FilterDefinition<Dummy> filterDefinition);
        Task<Dummy> DeleteByIdAsync(string correlationId, string id);
        Task DeleteByIdsAsync(string correlationId, string[] ids);
        Task<List<Dummy>> GetListByFilterAsync(string correlationId, FilterDefinition<Dummy> filterDefinition, SortDefinition<Dummy> sortDefinition = null);
        Task<List<Dummy>> GetListByIdsAsync(string correlationId, string[] ids);
        Task<Dummy> GetOneByIdAsync(string correlationId, string id);
        Task<object> GetOneByIdAsync(string correlationId, string id, ProjectionParams projection);
        Task<Dummy> GetOneRandomAsync(string correlationId, FilterDefinition<Dummy> filterDefinition);
        Task<DataPage<object>> GetPageByFilterAndProjectionAsync(string correlationId, FilterDefinition<Dummy> filterDefinition, PagingParams paging = null, SortDefinition<Dummy> sortDefinition = null, ProjectionParams projection = null);
        Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterDefinition<Dummy> filterDefinition, PagingParams paging = null, SortDefinition<Dummy> sortDefinition = null);
        Task<Dummy> ModifyAsync(string correlationId, FilterDefinition<Dummy> filterDefinition, UpdateDefinition<Dummy> updateDefinition);
        Task<Dummy> ModifyByIdAsync(string correlationId, string id, UpdateDefinition<Dummy> updateDefinition);
        Task<Dummy> SetAsync(string correlationId, Dummy item);
        Task<Dummy> UpdateAsync(string correlationId, Dummy item);
    }
}

