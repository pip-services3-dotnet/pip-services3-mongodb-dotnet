using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using PipServices3.Commons.Data;

namespace PipServices3.MongoDb.Persistence
{
    public enum DummyType : int
    {
        Dummy = 0,
        NotDummy
    }

    [DataContract]
    [BsonNoId]
    [BsonIgnoreExtraElements]
    public class Dummy : IStringIdentifiable
    {
        [BsonElement("id")]
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [BsonElement("key")]
        [DataMember(Name = "key")]
        public string Key { get; set; }

        [BsonElement("content")]
        [DataMember(Name = "content")]
        public string Content { get; set; }

        [BsonElement("create_time_utc")]
        [DataMember(Name = "create_time_utc")]
        public DateTime CreateTimeUtc { get; set; }

        [BsonElement("inner_dummy")]
        [DataMember(Name = "inner_dummy")]
        public InnerDummy InnerDummy { get; set; }

        [BsonRepresentation(BsonType.String)]
        [BsonElement("dummy_type")]
        [DataMember(Name = "dummy_type")]
        public DummyType DummyType { get; set; }

        [BsonElement("inner_dummies")]
        [DataMember(Name = "inner_dummies")]
        public List<InnerDummy> InnerDummies { get; set; } = new List<InnerDummy>();
    }
}
