using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;

namespace MongoDb.Bugs
{
    public class DatabaseFixture : IDisposable
    {
        private readonly MongoDbRunner runner;
        public IMongoDatabase Database { get; }
        public IMongoCollection<Post> PostCollection;

        public DatabaseFixture()
        {
            runner = MongoDbRunner.Start();
            var client = new MongoClient(runner.ConnectionString);

            Database = client.GetDatabase("MongoTestDb");
            PostCollection = Database.GetCollection<Post>("posts");

            if (!PostCollection.AsQueryable().Any())
            {
                PostCollection.InsertOne(new Post
                {
                    Id = ObjectId.GenerateNewId(),
                    Title = "post1",
                    Category = new Post.EmbeddedCategory
                    {
                        Id = ObjectId.GenerateNewId(),
                        Name = "category1"
                    },
                    Comments = new[]
                    {
                        new Post.EmbeddedComment
                        {
                            Id = ObjectId.GenerateNewId(),
                            Text ="test"
                        }
                    }
                });

                PostCollection.InsertOne(new Post
                {
                    Id = ObjectId.GenerateNewId(),
                    Title = "post2",
                    Category = new Post.EmbeddedCategory
                    {
                        Id = ObjectId.GenerateNewId(),
                        Name = "category2"
                    },
                });
            }
        }

        public void Dispose()
        {
            runner.Dispose();
        }
    }
}
