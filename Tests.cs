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

            var query = postCollection
                .AsQueryable()
                .Where(p => selectedIds.Contains(p.Id) == false);

            var result = query.ToList();
        }

        [Fact]
        public void CustomProjection_WithAutoMapper()
        {
            var mapperConfiguration = new MapperConfiguration(config =>
            {
                config.CreateMap<Post, PostDto>();
                config.CreateMap<Post.EmbeddedCategory, PostDto.CategoryDto>();
                config.CreateMap<Post.EmbeddedComment, PostDto.CommentDto>();
            });
            var mapper = mapperConfiguration.CreateMapper();

            var query = postCollection
                 .AsQueryable()
                 .ProjectTo<PostDto>(mapper.ConfigurationProvider);

            var result = query
                 .ToList();
        }

        [Fact]
        public void CustomProjection_WithoutAutoMapper()
        {
            var query = postCollection
                .AsQueryable()
                .Select(p => new PostDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Category = p.Category == null ? null : new PostDto.CategoryDto
                    {
                        Id = p.Id,
                        Name = p.Category.Name,
                    },
                    Comments = p.Comments.Select(c => new PostDto.CommentDto
                    {
                        Id = c.Id,
                        Text = c.Text
                    })
                });

            var result = query.ToList();
        }

        [Fact]
        public void CustomProjection_UsingJsonString()
        {
            ProjectionDefinition<Post, PostDto> project = @"
{
	'Category': {
		'$cond': [{
				'$eq': ['$Category', null]
			}, null, {
				'Id': '$Category._id',
				'Name': '$Category.Name'
			}
		]
	},
	'Comments': {
		'$map': {
			'input': '$Comments',
			'as': 'dtoEmbeddedComment',
			'in': {
				'Id': '$$dtoEmbeddedComment._id',
				'Text': '$$dtoEmbeddedComment.Text'
			}
		}
	},
	'Id': '$_id',
	'Title': '$Title',
	'_id': 0
}";

            var query = postCollection
                .Aggregate()
                .Project(project);

            var result = query.ToList();
        }

        [Fact]
        public void Contains_On_EmbeddedDocuments()
        {
            var query = postCollection
                .AsQueryable()
                .Select(p => new
                {
                    Id = p.Id,
                    Comments = p.Comments.Where(c => c.Text.Contains("test"))
                });

            var result = query.ToList();
        }

        [Fact]
        public void ToList_On_EmbeddedDocuments()
        {
            var query = postCollection
                .AsQueryable()
                .Select(p => new
                {
                    Id = p.Id,
                    Comments = p.Comments.ToList()
                });

            var result = query.ToList();
        }
    }
}
