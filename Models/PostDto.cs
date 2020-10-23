using MongoDB.Bson;
using System.Collections.Generic;

namespace MongoDb.Bugs
{
    public class PostDto
    {
        public ObjectId Id { get; set; }
        public string Title { get; set; }
        public CategoryDto Category { get; set; }
        public IEnumerable<CommentDto> Comments { get; set; }

        //[BsonNoId]
        public class CategoryDto
        {
            public ObjectId Id { get; set; }
            public string Name { get; set; }
        }

        //[BsonNoId]
        public class CommentDto
        {
            public ObjectId Id { get; set; }
            public string Text { get; set; }
        }
    }
}
