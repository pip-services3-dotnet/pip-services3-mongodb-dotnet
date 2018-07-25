using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PipServices.MongoDb.Persistence
{
    [DataContract]
    public class InnerDummy
    {
        [BsonElement("id")]
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [BsonElement("name")]
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [BsonElement("description")]
        [DataMember(Name = "description")]
        public string Description { get; set; }

        [BsonElement("inner_inner_dummies")]
        [DataMember(Name = "inner_inner_dummies")]
        public List<InnerDummy> InnerInnerDummies { get; set; } = new List<InnerDummy>();
    }
}