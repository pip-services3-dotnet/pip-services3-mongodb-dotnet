using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using PipServices.Commons.Data;
using Xunit;

namespace PipServices.MongoDb.Persistence
{
    public class PersistenceFixture
    {
        private readonly Dummy _dummy1 = new Dummy
        {
            Key = "Key 1",
            Content = "Content 1",
            CreateTimeUtc = DateTime.UtcNow,
            InnerDummy = new InnerDummy()
            {
                Description = "Inner Dummy Description"
            },
            DummyType = DummyType.NotDummy,
            InnerDummies = new List<InnerDummy>()
            {
                new InnerDummy
                {
                    Id = "1",
                    Name = "InnerDummy #1"
                },
                new InnerDummy
                {
                    Id = "2",
                    Name = "InnerDummy #2"
                },
                new InnerDummy
                {
                    Id = "3",
                    Name = "InnerDummy #3",
                    InnerInnerDummies = new List<InnerDummy>()
                    {
                        new InnerDummy()
                        {
                            Id = "100",
                            Name = "InnerInner Dummy#1"
                        }
                    }
                }
            }
        };

        private readonly Dummy _dummy2 = new Dummy
        {
            Key = "Key 2",
            Content = "Content 2",
            CreateTimeUtc = DateTime.UtcNow,
            DummyType = DummyType.Dummy
        };

        private readonly Dummy _dummy3 = new Dummy
        {
            Key = "Key 3",
            Content = "Content 3",
            CreateTimeUtc = DateTime.UtcNow,
            InnerDummy = new InnerDummy()
            {
                Description = "Inner Dummy #3 Description"
            },
            DummyType = DummyType.Dummy,
            InnerDummies = new List<InnerDummy>()
            {
                new InnerDummy
                {
                    Id = "3",
                    Name = "InnerDummy #3",
                    InnerInnerDummies = new List<InnerDummy>()
                    {
                        new InnerDummy()
                        {
                            Id = "100",
                            Name = "InnerInner Dummy# 3.1"
                        },
                        new InnerDummy
                        {
                            Id = "200",
                            Name = "InnerInner Dummy #3.2"
                        },
                        new InnerDummy
                        {
                            Id = "300",
                            Name = "InnerInner Dummy #3.3"
                        }
                    }
                }
            }
        };

        private readonly IDummyPersistence _persistence;

        public PersistenceFixture(IDummyPersistence persistence)
        {
            Assert.NotNull(persistence);

            _persistence = persistence;
        }

        public async Task TestCrudOperationsAsync()
        {
            // Create one dummy
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);

            Assert.NotNull(dummy1);
            Assert.NotNull(dummy1.Id);
            Assert.Equal(_dummy1.Key, dummy1.Key);
            Assert.Equal(_dummy1.Content, dummy1.Content);

            // Create another dummy
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            Assert.NotNull(dummy2);
            Assert.NotNull(dummy2.Id);
            Assert.Equal(_dummy2.Key, dummy2.Key);
            Assert.Equal(_dummy2.Content, dummy2.Content);

            //// Get all dummies
            //var dummies = await _get.GetAllAsync(null);
            //Assert.NotNull(dummies);
            //Assert.Equal(2, dummies.Count());

            // Update the dummy
            dummy1.Content = "Updated Content 1";
            var dummy = await _persistence.UpdateAsync(null, dummy1);

            Assert.NotNull(dummy);
            Assert.Equal(dummy1.Id, dummy.Id);
            Assert.Equal(dummy1.Key, dummy.Key);
            Assert.Equal(dummy1.Content, dummy.Content);

            // Delete the dummy
            await _persistence.DeleteByIdAsync(null, dummy1.Id);

            // Try to get deleted dummy
            dummy = await _persistence.GetOneByIdAsync(null, dummy1.Id);
            Assert.Null(dummy);
        }

        public async Task TestMultithreading()
        {
            const int itemNumber = 50;

            var dummies = new List<Dummy>();

            for (var i = 0; i < itemNumber; i++)
            {
                dummies.Add(new Dummy() {Id = i.ToString(), Key = "Key " + i, Content = "Content " + i});
            }

            var count = 0;
            dummies.AsParallel().ForAll(async x =>
            {
                await _persistence.CreateAsync(null, x);
                Interlocked.Increment(ref count);
            });

            while (count < itemNumber)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }

            //var dummiesResponce = await _get.GetAllAsync(null);
            //Assert.NotNull(dummies);
            //Assert.Equal(itemNumber, dummiesResponce.Count());
            //Assert.Equal(itemNumber, dummiesResponce.Total);

            dummies.AsParallel().ForAll(async x =>
            {
                var updatedContent = "Updated Content " + x.Id;

                // Update the dummy
                x.Content = updatedContent;
                var dummy = await _persistence.UpdateAsync(null, x);

                Assert.NotNull(dummy);
                Assert.Equal(x.Id, dummy.Id);
                Assert.Equal(x.Key, dummy.Key);
                Assert.Equal(updatedContent, dummy.Content);
            });

            var taskList = new List<Task>();
            foreach (var dummy in dummies)
            {
                taskList.Add(AssertDelete(dummy));
            }

            Task.WaitAll(taskList.ToArray(), CancellationToken.None);

            //count = 0;
            //dummies.AsParallel().ForAll(async x =>
            //{
            //    // Delete the dummy
            //    await _write.DeleteByIdAsync(null, x.Id);

            //    // Try to get deleted dummy
            //    var dummy = await _get.GetOneByIdAsync(null, x.Id);
            //    Assert.Null(dummy);

            //    Interlocked.Increment(ref count);
            //});

            //while (count < itemNumber)
            //{
            //    await Task.Delay(TimeSpan.FromMilliseconds(10));
            //}

            //dummiesResponce = await _get.GetAllAsync(null);
            //Assert.NotNull(dummies);
            //Assert.Equal(0, dummiesResponce.Count());
            //Assert.Equal(0, dummiesResponce.Total);
        }

        public async Task TestGetByWrongIdAndProjection()
        {
            // arrange
            var dummy = await _persistence.CreateAsync(null, _dummy1);
            var projection = ProjectionParams.FromValues("inner_dummy.description", "content", "key");

            // act
            dynamic result = await _persistence.GetOneByIdAsync(null, "wrong_id", projection);

            // assert
            Assert.Null(result);
        }

        public async Task TestGetByIdAndProjection()
        {
            // arrange
            var dummy = await _persistence.CreateAsync(null, _dummy1);
            var projection = ProjectionParams.FromValues("inner_dummy.description", "content", "key", "create_time_utc", "dummy_type");

            // act
            dynamic result = await _persistence.GetOneByIdAsync(null, dummy.Id, projection);

            // assert
            Assert.NotNull(result);
            Assert.Equal(dummy.Key, result.key);
            Assert.Equal(dummy.Content, result.content);
            Assert.Equal(dummy.InnerDummy.Description, result.inner_dummy.description);
            Assert.Equal(dummy.CreateTimeUtc.ToString(), result.create_time_utc.ToString());
            Assert.Equal(dummy.DummyType.ToString(), result.dummy_type.ToString());
        }

        public async Task TestGetByIdAndProjectionFromArray()
        {
            // arrange
            var dummy = await _persistence.CreateAsync(null, _dummy1);
            var projection = ProjectionParams.FromValues("key", "inner_dummies(name, description)");

            // act
            dynamic result = await _persistence.GetOneByIdAsync(null, dummy.Id, projection);

            // assert
            Assert.NotNull(result);
            Assert.Equal(dummy.Key, result.key);
            Assert.Equal(dummy.InnerDummies[0].Name, result.inner_dummies[0].name);
            Assert.Equal(dummy.InnerDummies[1].Description, result.inner_dummies[1].description);
        }

        public async Task TestGetByIdAndWrongProjection()
        {
            // arrange
            var dummy = await _persistence.CreateAsync(null, _dummy1);
            var projection = ProjectionParams.FromValues("Wrong_Key", "Wrong_Content");

            // act
            dynamic result = await _persistence.GetOneByIdAsync(null, dummy.Id, projection);

            // assert
            Assert.Null(result);
        }

        public async Task TestGetByIdAndNullProjection()
        {
            // arrange
            var dummy = await _persistence.CreateAsync(null, _dummy1);

            // act
            dynamic result = await _persistence.GetOneByIdAsync(null, dummy.Id, null);

            // assert
            Assert.NotNull(result);

            if (result is Dummy)
            {
                Assert.Equal(dummy.Id, (result as Dummy).Id);
                Assert.Equal(dummy.Key, (result as Dummy).Key);
                Assert.Equal(dummy.Content, (result as Dummy).Content);
                Assert.Equal(dummy.InnerDummy.Description, (result as Dummy).InnerDummy.Description);
            }
            else
            {
                Assert.Equal(dummy.Id, result.id);
                Assert.Equal(dummy.Key, result.key);
                Assert.Equal(dummy.Content, result.content);
                Assert.Equal(dummy.InnerDummy.Description, result.inner_dummy.description);
            }
        }

        public async Task TestGetByIdAndIdProjection()
        {
            // arrange
            var dummy = await _persistence.CreateAsync(null, _dummy1);
            var projection = ProjectionParams.FromValues("id");

            // act
            dynamic result = await _persistence.GetOneByIdAsync(null, dummy.Id, projection);

            // assert
            Assert.NotNull(result);
            Assert.Equal(dummy.Id, result.id);
        }

        public async Task TestGetPageByFilter()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            var builder = Builders<Dummy>.Filter;
            var filter = builder.Empty;

            // act
            var result = await _persistence.GetPageByFilterAsync(null, filter);

            // assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Data.Count);
        }

        public async Task TestGetPageByProjection()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            var builder = Builders<Dummy>.Filter;
            var filter = builder.Empty;

            var projection = ProjectionParams.FromValues("inner_dummy.description", "content", "key", "create_time_utc");

            // act
            dynamic result = await _persistence.GetPageByFilterAndProjectionAsync(null, filter, null, null, projection);

            // assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Data.Count);
            Assert.Equal(dummy1.Key, result.Data[0].key);
            Assert.Equal(dummy1.Content, result.Data[0].content);
            Assert.Equal(dummy1.InnerDummy.Description, result.Data[0].inner_dummy.description);
            Assert.Equal(dummy1.CreateTimeUtc.ToString(), result.Data[0].create_time_utc.ToString());
            Assert.Equal(dummy2.Key, result.Data[1].key);
            Assert.Equal(dummy2.Content, result.Data[1].content);
        }

        public async Task TestGetPageByNullProjection()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            var builder = Builders<Dummy>.Filter;
            var filter = builder.Empty;

            // act
            var result = await _persistence.GetPageByFilterAndProjectionAsync(null, filter);

            // assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Data.Count);
        }

        public async Task TestGetPageByWrongProjection()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            var builder = Builders<Dummy>.Filter;
            var filter = builder.Empty;

            var projection = ProjectionParams.FromValues("Wrong_InnerDummy.Description", "Wrong_Content", "Wrong_Key");

            // act
            dynamic result = await _persistence.GetPageByFilterAndProjectionAsync(null, filter, null, null, projection);

            // assert
            Assert.NotNull(result);
            Assert.Empty(result.Data);
        }

        public async Task TestModifyExistingPropertiesBySelectedFields()
        {
            // arrange 
            var dummy = await _persistence.CreateAsync(null, _dummy1);

            var updateMap = new AnyValueMap()
            {
                { "Content", "Modified Content" },
                { "InnerDummy.Description", "Modified InnerDummy Description" }
            };

            // act
            var result = await _persistence.ModifyByIdAsync(null, dummy.Id, ComposeUpdate(updateMap));

            // assert
            Assert.NotNull(result);
            Assert.Equal(dummy.Id, result.Id);
            Assert.Equal("Modified Content", result.Content);
            Assert.Equal("Modified InnerDummy Description", result.InnerDummy.Description);
        }

        public async Task TestModifyExistingPropertiesBySelectedNotChangedFields()
        {
            // arrange 
            var dummy = await _persistence.CreateAsync(null, _dummy1);

            // no changes
            var updateMap = new AnyValueMap()
            {
                { "Content", dummy.Content },
                { "InnerDummy.Description", dummy.InnerDummy.Description }
            };

            // act
            var result = await _persistence.ModifyByIdAsync(null, dummy.Id, ComposeUpdate(updateMap));

            // assert
            Assert.NotNull(result);
            Assert.Equal(dummy.Id, result.Id);
            Assert.Equal(dummy.Content, result.Content);
            Assert.Equal(dummy.InnerDummy.Description, result.InnerDummy.Description);
        }

        public async Task TestModifyNullPropertiesBySelectedFields()
        {
            // arrange 
            var dummy = await _persistence.CreateAsync(null, _dummy2);

            var updateMap = new AnyValueMap()
            {
                { "Content", "Modified Content" },
                { "InnerDummy", new InnerDummy() { Description = "Modified InnerDummy Description" } }
            };

            // act
            var result = await _persistence.ModifyByIdAsync(null, dummy.Id, ComposeUpdate(updateMap));

            // assert
            Assert.NotNull(result);
            Assert.Equal(dummy.Id, result.Id);
            Assert.Null(dummy.InnerDummy);
            Assert.Equal("Modified Content", result.Content);
            Assert.Equal("Modified InnerDummy Description", result.InnerDummy.Description);
        }

        public async Task TestModifyNestedCollectionBySelectedFields()
        {
            // arrange 
            var dummy = await _persistence.CreateAsync(null, _dummy1);

            var builder = Builders<Dummy>.Filter;
            var filter = builder.Empty;

            var updateTuples = new Tuple<string, string, string, string>[]
            {
                Tuple.Create("InnerDummies.Id", "1", "InnerDummies.$.Name", "Modified Name"),
                Tuple.Create("InnerDummies.2.InnerInnerDummies.Id", "100", "InnerDummies.2.InnerInnerDummies.$.Name", "Modified Inner Inner Name"),
            };

            // act 1
            var result = await _persistence.ModifyAsync(null, ComposeUpdateFilter(dummy.Id, updateTuples[0]), ComposeUpdate(updateTuples[0]));

            // assert 1
            Assert.NotNull(result);
            Assert.Equal(dummy.Id, result.Id);
            Assert.Equal("Modified Name", result.InnerDummies[0].Name);

            // act 2
            result = await _persistence.ModifyAsync(null, ComposeUpdateFilter(dummy.Id, updateTuples[1]), ComposeUpdate(updateTuples[1]));

            // assert 2
            Assert.NotNull(result);
            Assert.Equal(dummy.Id, result.Id);
            Assert.Equal("Modified Inner Inner Name", result.InnerDummies[2].InnerInnerDummies[0].Name);
        }

        public async Task TestModifyNestedCollection()
        {
            // arrange 
            var dummy = await _persistence.CreateAsync(null, _dummy1);

            var innerInnerDummies = new List<InnerDummy>()
            {
                new InnerDummy()
                {
                    Id = "Test Inner Id #1",
                    Name = "Test Inner Dummy #1"
                },
                new InnerDummy()
                {
                    Id = "Test Inner Id #2",
                    Name = "Test Inner Dummy #2"
                },
            };

            var updateMap = new AnyValueMap()
            {
                { "inner_dummy.inner_inner_dummies", innerInnerDummies }
            };

            // act - It's important to pass the type of updated object!
            var result = await _persistence.ModifyByIdAsync(null, dummy.Id, ComposeUpdate<List<InnerDummy>>(updateMap));

            // assert 1
            Assert.NotNull(result);
            Assert.Equal(innerInnerDummies[0].Id, result.InnerDummy.InnerInnerDummies[0].Id);
            Assert.Equal(innerInnerDummies[0].Name, result.InnerDummy.InnerInnerDummies[0].Name);
            Assert.Equal(innerInnerDummies[1].Id, result.InnerDummy.InnerInnerDummies[1].Id);
            Assert.Equal(innerInnerDummies[1].Name, result.InnerDummy.InnerInnerDummies[1].Name);
        }

        public async Task TestSearchWithinNestedCollectionByFilter()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            var filterParams = ExtractFilterParams("inner_dummies.name:InnerDummy #2");

            // act
            var result = await _persistence.GetPageByFilterAsync(null, ComposeFilter(filterParams));

            // assert
            Assert.NotNull(result);
            Assert.Single(result.Data);
            Assert.Equal(dummy1.Id, result.Data[0].Id);
        }

        public async Task TestSearchWithinNestedCollectionByFilterAndNullProjection()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            var filterParams = ExtractFilterParams("inner_dummies.name:InnerDummy #2");

            // act
            dynamic result = await _persistence.GetPageByFilterAndProjectionAsync(null, ComposeFilter(filterParams), null, null, null);

            // assert
            Assert.NotNull(result);
            Assert.Single(result.Data);
            Assert.Equal(dummy1.Id, result.Data[0].id);
        }

        public async Task TestSearchWithinDeepNestedCollectionByFilter()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);
            var dummy3 = await _persistence.CreateAsync(null, _dummy3);

            var filterParams = ExtractFilterParams("inner_dummies.inner_inner_dummies.name:InnerInner Dummy#1");

            // act
            var result = await _persistence.GetPageByFilterAsync(null, ComposeFilter(filterParams));

            // assert
            Assert.NotNull(result);
            Assert.Single(result.Data);
            Assert.Equal(dummy1.Id, result.Data[0].Id);
        }

        public async Task TestSearchWithinDeepNestedCollectionByFilterAndNullProjection()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);
            var dummy3 = await _persistence.CreateAsync(null, _dummy3);

            var filterParams = ExtractFilterParams("inner_dummies.inner_inner_dummies.name:InnerInner Dummy #3.3");

            // act
            dynamic result = await _persistence.GetPageByFilterAndProjectionAsync(null, ComposeFilter(filterParams), null, null, null);

            // assert
            Assert.NotNull(result);
            Assert.Single(result.Data);
            Assert.Equal(dummy3.Id, result.Data[0].id);
        }

        public async Task TestGetPageByIdsFilter()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            var filter = FilterParams.FromTuples(
                "ids", $"1234567890,{dummy1.Id}"
            );

            // act
            var result = await _persistence.GetAsync(null, filter, null, null);

            // assert
            Assert.NotNull(result);
            Assert.Single(result.Data);
        }

        public async Task TestGetPageByArrayOfKeysFilter()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);
            var dummy3 = await _persistence.CreateAsync(null, _dummy3);

            var filter = FilterParams.FromTuples(
                "key", $"{dummy1.Key},{dummy2.Key}"
            );

            // act
            var result = await _persistence.GetAsync(null, filter, null, null);

            // assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Data.Count);

            Assert.Equal(dummy1.Key, result.Data[0].Key);
            Assert.Equal(dummy2.Key, result.Data[1].Key);
        }

        public async Task TestGetPageSortedByOneField()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);
            var dummy3 = await _persistence.CreateAsync(null, _dummy3);

            // keys: Key 1, Key 2, Key 3

            var sortParams = new SortParams()
            {
                new SortField("key", false)
            };

            // result -> 3 (Key 3), 2 (Key 2), 1 (Key 1)

            // act
            var result = await _persistence.GetAsync(null, null, null, sortParams);

            // assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Data.Count);

            Assert.Equal(dummy3.Key, result.Data[0].Key);
            Assert.Equal(dummy2.Key, result.Data[1].Key);
            Assert.Equal(dummy1.Key, result.Data[2].Key);
        }

        public async Task TestGetPageSortedByMultipleFields()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);
            var dummy3 = await _persistence.CreateAsync(null, _dummy3);

            // keys: Key 1, Key 2, Key 3
            // dummy_type: not_dummy, dummy, dummy

            var sortParams = new SortParams()
            {
                new SortField("dummy_type", false),
                new SortField("key", false)
            };

            // result -> 1 (not dummy, Key 1), 3 (dummy, Key 3), 2 (dummy, Key 2)

            // act
            var result = await _persistence.GetAsync(null, null, null, sortParams);

            // assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Data.Count);

            Assert.Equal(dummy1.Key, result.Data[0].Key);
            Assert.Equal(dummy3.Key, result.Data[1].Key);
            Assert.Equal(dummy2.Key, result.Data[2].Key);
        }

        public async Task TestGetPageByProjectionAndSortedByOneField()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);
            var dummy3 = await _persistence.CreateAsync(null, _dummy3);

            // keys: Key 1, Key 2, Key 3

            var sortParams = new SortParams()
            {
                new SortField("key", false)
            };

            // result -> 3 (Key 3), 2 (Key 2), 1 (Key 1)

            var projection = ProjectionParams.FromValues("key");

            // act
            dynamic result = await _persistence.GetAsync(null, null, null, sortParams, projection);

            // assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Data.Count);

            Assert.Equal(dummy3.Key, result.Data[0].key);
            Assert.Equal(dummy2.Key, result.Data[1].key);
            Assert.Equal(dummy1.Key, result.Data[2].key);
        }

        public async Task TestGetPageByProjectionAndSortedByMultipleFields()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);
            var dummy3 = await _persistence.CreateAsync(null, _dummy3);

            // keys: Key 1, Key 2, Key 3
            // dummy_type: not_dummy, dummy, dummy

            var sortParams = new SortParams()
            {
                new SortField("dummy_type", false),
                new SortField("key", false)
            };

            // result -> 1 (not dummy, Key 1), 3 (dummy, Key 3), 2 (dummy, Key 2)

            var projection = ProjectionParams.FromValues("key");

            // act
            dynamic result = await _persistence.GetAsync(null, null, null, sortParams, projection);

            // assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Data.Count);

            Assert.Equal(dummy1.Key, result.Data[0].key);
            Assert.Equal(dummy3.Key, result.Data[1].key);
            Assert.Equal(dummy2.Key, result.Data[2].key);
        }

        private async Task AssertDelete(Dummy dummy)
        {
            await _persistence.DeleteByIdAsync(null, dummy.Id);

            // Try to get deleted dummy
            var result = await _persistence.GetOneByIdAsync(null, dummy.Id);
            Assert.Null(result);
        }

        private UpdateDefinition<Dummy> ComposeUpdate(AnyValueMap updateMap)
        {
            return ComposeUpdate<object>(updateMap);
        }

        private UpdateDefinition<Dummy> ComposeUpdate<T>(AnyValueMap updateMap)
            where T : class
        {
            updateMap = updateMap ?? new AnyValueMap();

            var builder = Builders<Dummy>.Update;
            var updateDefinitions = new List<UpdateDefinition<Dummy>>();

            foreach (var key in updateMap.Keys)
            {
                updateDefinitions.Add(builder.Set(key, (T)updateMap[key]));
            }

            return builder.Combine(updateDefinitions);
        }

        private FilterDefinition<Dummy> ComposeUpdateFilter(string id, Tuple<string, string, string, string> updateTuple)
        {
            var builder = Builders<Dummy>.Filter;
            var filter = Builders<Dummy>.Filter.Eq(x => x.Id, id);

            filter &= builder.Eq(updateTuple.Item1, updateTuple.Item2);

            return filter;
        }

        private UpdateDefinition<Dummy> ComposeUpdate(Tuple<string, string, string, string> updateTuple)
        {
            var builder = Builders<Dummy>.Update;
            var updateDefinitions = new List<UpdateDefinition<Dummy>>();

            if (updateTuple == null)
            {
                builder.Combine(updateDefinitions);
            }

            updateDefinitions.Add(builder.Set(updateTuple.Item3, updateTuple.Item4));

            return builder.Combine(updateDefinitions);
        }

        private FilterDefinition<Dummy> ComposeFilter(FilterParams filterParams)
        {
            filterParams = filterParams ?? new FilterParams();

            var builder = Builders<Dummy>.Filter;
            var filter = builder.Empty;
            foreach (var filterKey in filterParams.Keys)
            {
                filter &= builder.Eq(filterKey, filterParams[filterKey]);
            }

            return filter;
        }

        private FilterParams ExtractFilterParams(string query)
        {
            FilterParams filterParams = new FilterParams();

            foreach (var filterParameter in query.Split(','))
            {
                var keyValue = filterParameter.Split(':');

                if (keyValue.Length == 2)
                {
                    filterParams[keyValue[0]] = keyValue[1];
                }
            }

            return filterParams;
        }
    }
}
