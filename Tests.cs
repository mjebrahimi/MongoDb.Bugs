using AutoMapper;
using AutoMapper.QueryableExtensions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Linq;
using Xunit;

namespace MongoDb.Bugs
{
    public class Tests : IClassFixture<DatabaseFixture>
    {
        private readonly IMongoCollection<Post> postCollection;
        public Tests(DatabaseFixture database)
        {
            postCollection = database.PostCollection;
        }

        [Fact]
        public void Contains_False()
        {
            var selectedIds = new[] { ObjectId.GenerateNewId() };
            var result = postCollection
                .AsQueryable()
                .Where(p => selectedIds.Contains(p.Id) == false)
                .ToList();
        }

        [Fact]
        public void AutoMapper_ProjectTo()
        {
            var mapperConfiguration = new MapperConfiguration(config =>
            {
                config.CreateMap<Post, PostDto>();
                config.CreateMap<Post.EmbeddedCategory, PostDto.CategoryDto>();
                config.CreateMap<Post.EmbeddedComment, PostDto.CommentDto>();
            });
            var mapper = mapperConfiguration.CreateMapper();

            var result = postCollection
                 .AsQueryable()
                 .ProjectTo<PostDto>(mapper.ConfigurationProvider)
                 .ToList();
        }

        [Fact]
        public void Contains_On_EmbeddedDocuments()
        {
            var result = postCollection
                .AsQueryable()
                .Select(p => new
                {
                    Id = p.Id,
                    Comments = p.Comments.Where(c => c.Text.Contains("test"))
                })
                .ToList();
        }

        [Fact]
        public void ToList_On_EmbeddedDocuments()
        {
            var result = postCollection
                .AsQueryable()
                .Select(p => new
                {
                    Id = p.Id,
                    Comments = p.Comments.ToList()
                })
                .ToList();
        }
    }
}
