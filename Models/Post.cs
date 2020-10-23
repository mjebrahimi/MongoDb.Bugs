using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace MongoDb.Bugs
{
    public class Post
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Title { get; set; }
        public EmbeddedCategory Category { get; set; }
        public IEnumerable<EmbeddedComment> Comments { get; set; }

        public class EmbeddedCategory
        {
            [BsonId]
            public ObjectId Id { get; set; }
            public string Name { get; set; }
        }

        public class EmbeddedComment
        {
            [BsonId]
            public ObjectId Id { get; set; }
            public string Text { get; set; }
        }
    }
}
