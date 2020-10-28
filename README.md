# MongoDb C# Driver Bugs

This repo reproduce some bugs of [MongoDB C# Driver](https://github.com/mongodb/mongo-csharp-driver).

## 1- Using `Contains()` with `false` Check

Checking `Contains()` with `true` has NO error but with `false` ...

```csharp
var selectedIds = new[] { ObjectId.GenerateNewId() };
var result = postCollection
    .AsQueryable()
    .Where(p => selectedIds.Contains(p.Id) == false)
    .ToList();
```

```ini
  Message: 
    System.InvalidOperationException : Contains(value(MongoDB.Bson.ObjectId[])) is not supported.
  Stack Trace: 
    PredicateTranslator.GetFieldExpression(Expression expression)
    PredicateTranslator.TranslateComparison(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
    PredicateTranslator.TranslateComparison(BinaryExpression binaryExpression)
    PredicateTranslator.Translate(Expression node)
    PredicateTranslator.Translate(Expression node, IBsonSerializerRegistry serializerRegistry)
    QueryableTranslator.TranslateWhere(WhereExpression node)
    QueryableTranslator.Translate(Expression node)
    QueryableTranslator.TranslatePipeline(PipelineExpression node)
    QueryableTranslator.Translate(Expression node)
    QueryableTranslator.Translate(Expression node, IBsonSerializerRegistry serializerRegistry, ExpressionTranslationOptions translationOptions)
    MongoQueryProviderImpl`1.Translate(Expression expression)
    MongoQueryProviderImpl`1.Execute(Expression expression)
    MongoQueryableImpl`2.GetEnumerator()
    List`1.ctor(IEnumerable`1 collection)
    Enumerable.ToList[TSource](IEnumerable`1 source)
```

## 2- Using `AutoMapper.ProjectTo()` with MongoQueryable

```csharp
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
```

```ini
  Message: 
    System.FormatException : An error occurred while deserializing the Category property of class MongoDb.Bugs.PostDto: Element 'Id' does not match any field or property of class MongoDb.Bugs.PostDto+CategoryDto.
    ---- System.FormatException : Element 'Id' does not match any field or property of class MongoDb.Bugs.PostDto+CategoryDto.
  Stack Trace: 
    MongoQueryProviderImpl`1.Execute(Expression expression)
    MongoQueryableImpl`2.GetEnumerator()
    List`1.ctor(IEnumerable`1 collection)
    Enumerable.ToList[TSource](IEnumerable`1 source)
    Tests.AutoMapper_ProjectTo() line 41
    ----- Inner Stack Trace -----
    BsonClassMapSerializer`1.DeserializeClass(BsonDeserializationContext context)
    BsonClassMapSerializer`1.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    IBsonSerializerExtensions.Deserialize(IBsonSerializer serializer, BsonDeserializationContext context)
    BsonClassMapSerializer`1.DeserializeMemberValue(BsonDeserializationContext context, BsonMemberMap memberMap)
```

### Custom Projection Without AutoMapper

Equivalent to the previous query that automapper makes:

```csharp
var result = postCollection
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
	})
	.ToList()
```

```ini
  Message: 
    System.FormatException : An error occurred while deserializing the Category property of class MongoDb.Bugs.PostDto: Element 'Id' does not match any field or property of class MongoDb.Bugs.PostDto+CategoryDto.
    ---- System.FormatException : Element 'Id' does not match any field or property of class MongoDb.Bugs.PostDto+CategoryDto.
  Stack Trace: 
    MongoQueryProviderImpl`1.Execute(Expression expression)
    MongoQueryableImpl`2.GetEnumerator()
    List`1.ctor(IEnumerable`1 collection)
    Enumerable.ToList[TSource](IEnumerable`1 source)
    Tests.CustomProjection_WithoutAutoMapper() line 70
    ----- Inner Stack Trace -----
    BsonClassMapSerializer`1.DeserializeClass(BsonDeserializationContext context)
    BsonClassMapSerializer`1.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    IBsonSerializerExtensions.Deserialize(IBsonSerializer serializer, BsonDeserializationContext context)
    BsonClassMapSerializer`1.DeserializeMemberValue(BsonDeserializationContext context, BsonMemberMap memberMap)
```

### Custom Projection Using Json String

Equivalent to the previous query but using json string:

```csharp
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

var result = postCollection
	.Aggregate()
	.Project(project)
	.ToList();
```

```ini
  Message: 
    System.FormatException : An error occurred while deserializing the Category property of class MongoDb.Bugs.PostDto: Element 'Id' does not match any field or property of class MongoDb.Bugs.PostDto+CategoryDto.
    ---- System.FormatException : Element 'Id' does not match any field or property of class MongoDb.Bugs.PostDto+CategoryDto.
  Stack Trace: 
    BsonClassMapSerializer`1.DeserializeMemberValue(BsonDeserializationContext context, BsonMemberMap memberMap)
    BsonClassMapSerializer`1.DeserializeClass(BsonDeserializationContext context)
    BsonClassMapSerializer`1.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    IBsonSerializerExtensions.Deserialize[TValue](IBsonSerializer`1 serializer, BsonDeserializationContext context)
    EnumerableSerializerBase`2.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    IBsonSerializerExtensions.Deserialize[TValue](IBsonSerializer`1 serializer, BsonDeserializationContext context)
    CursorDeserializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    IBsonSerializerExtensions.Deserialize[TValue](IBsonSerializer`1 serializer, BsonDeserializationContext context)
    AggregateResultDeserializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    IBsonSerializerExtensions.Deserialize[TValue](IBsonSerializer`1 serializer, BsonDeserializationContext context)
    CommandUsingCommandMessageWireProtocol`1.ProcessResponse(ConnectionId connectionId, CommandMessage responseMessage)
    CommandUsingCommandMessageWireProtocol`1.Execute(IConnection connection, CancellationToken cancellationToken)
    CommandWireProtocol`1.Execute(IConnection connection, CancellationToken cancellationToken)
    ServerChannel.ExecuteProtocol[TResult](IWireProtocol`1 protocol, ICoreSession session, CancellationToken cancellationToken)
    ServerChannel.Command[TResult](ICoreSession session, ReadPreference readPreference, DatabaseNamespace databaseNamespace, BsonDocument command, IEnumerable`1 commandPayloads, IElementNameValidator commandValidator, BsonDocument additionalOptions, Action`1 postWriteAction, CommandResponseHandling responseHandling, IBsonSerializer`1 resultSerializer, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
    CommandOperationBase`1.ExecuteProtocol(IChannelHandle channel, ICoreSessionHandle session, ReadPreference readPreference, CancellationToken cancellationToken)
    ReadCommandOperation`1.ExecuteAttempt(RetryableReadContext context, Int32 attempt, Nullable`1 transactionNumber, CancellationToken cancellationToken)
    RetryableReadOperationExecutor.Execute[TResult](IRetryableReadOperation`1 operation, RetryableReadContext context, CancellationToken cancellationToken)
    ReadCommandOperation`1.Execute(RetryableReadContext context, CancellationToken cancellationToken)
    AggregateOperation`1.Execute(RetryableReadContext context, CancellationToken cancellationToken)
    AggregateOperation`1.Execute(IReadBinding binding, CancellationToken cancellationToken)
    OperationExecutor.ExecuteReadOperation[TResult](IReadBinding binding, IReadOperation`1 operation, CancellationToken cancellationToken)
    MongoCollectionImpl`1.ExecuteReadOperation[TResult](IClientSessionHandle session, IReadOperation`1 operation, ReadPreference readPreference, CancellationToken cancellationToken)
    MongoCollectionImpl`1.ExecuteReadOperation[TResult](IClientSessionHandle session, IReadOperation`1 operation, CancellationToken cancellationToken)
    MongoCollectionImpl`1.Aggregate[TResult](IClientSessionHandle session, PipelineDefinition`2 pipeline, AggregateOptions options, CancellationToken cancellationToken)
    <>c__DisplayClass19_0`1.<Aggregate>b__0(IClientSessionHandle session)
    MongoCollectionImpl`1.UsingImplicitSession[TResult](Func`2 func, CancellationToken cancellationToken)
    MongoCollectionImpl`1.Aggregate[TResult](PipelineDefinition`2 pipeline, AggregateOptions options, CancellationToken cancellationToken)
    CollectionAggregateFluent`2.ToCursor(CancellationToken cancellationToken)
    IAsyncCursorSourceExtensions.ToList[TDocument](IAsyncCursorSource`1 source, CancellationToken cancellationToken)
    Tests.CustomProjection_UsingJsonString() line 106
    ----- Inner Stack Trace -----
    BsonClassMapSerializer`1.DeserializeClass(BsonDeserializationContext context)
    BsonClassMapSerializer`1.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    IBsonSerializerExtensions.Deserialize(IBsonSerializer serializer, BsonDeserializationContext context)
    BsonClassMapSerializer`1.DeserializeMemberValue(BsonDeserializationContext context, BsonMemberMap memberMap)
```

## 3- Using `Contains()` to Filter Embedded Documents

```csharp
var result = postCollection
    .AsQueryable()
    .Select(p => new
    {
        Id = p.Id,
        Comments = p.Comments.Where(c => c.Text.Contains("test"))
    })
    .ToList();
```

```ini
  Message: 
    System.NotSupportedException : Contains of type System.String is not supported in the expression tree {document}{$c.Text}.Contains("test").
  Stack Trace: 
    AggregateLanguageTranslator.TranslateMethodCall(MethodCallExpression node)
    AggregateLanguageTranslator.TranslateValue(Expression node)
    AggregateLanguageTranslator.TranslateWhere(WhereExpression node)
    AggregateLanguageTranslator.TranslateValue(Expression node)
    AggregateLanguageTranslator.TranslatePipeline(PipelineExpression node)
    AggregateLanguageTranslator.TranslateValue(Expression node)
    AggregateLanguageTranslator.TranslateMapping(ProjectionMapping mapping)
    AggregateLanguageTranslator.TranslateNew(NewExpression node)
    AggregateLanguageTranslator.TranslateValue(Expression node)
    AggregateLanguageTranslator.Translate(Expression node, ExpressionTranslationOptions translationOptions)
    QueryableTranslator.TranslateProjectValue(Expression selector)
    QueryableTranslator.TranslateSelect(SelectExpression node)
    QueryableTranslator.Translate(Expression node)
    QueryableTranslator.TranslatePipeline(PipelineExpression node)
    QueryableTranslator.Translate(Expression node)
    QueryableTranslator.Translate(Expression node, IBsonSerializerRegistry serializerRegistry, ExpressionTranslationOptions translationOptions)
    MongoQueryProviderImpl`1.Translate(Expression expression)
    MongoQueryProviderImpl`1.Execute(Expression expression)
    MongoQueryableImpl`2.GetEnumerator()
    List`1.ctor(IEnumerable`1 collection)
    Enumerable.ToList[TSource](IEnumerable`1 source)
```

## 4- Using `.ToList()` on Embedded Documents

```csharp
var result = postCollection
    .AsQueryable()
    .Select(p => new
    {
        Id = p.Id,
        Comments = p.Comments.ToList()
    })
    .ToList();
```

```ini
  Message: 
    System.NotSupportedException : The result operation MongoDB.Driver.Linq.Expressions.ResultOperators.ListResultOperator is not supported.
  Stack Trace: 
    AggregateLanguageTranslator.TranslatePipeline(PipelineExpression node)
    AggregateLanguageTranslator.TranslateValue(Expression node)
    AggregateLanguageTranslator.TranslateMapping(ProjectionMapping mapping)
    AggregateLanguageTranslator.TranslateNew(NewExpression node)
    AggregateLanguageTranslator.TranslateValue(Expression node)
    AggregateLanguageTranslator.Translate(Expression node, ExpressionTranslationOptions translationOptions)
    QueryableTranslator.TranslateProjectValue(Expression selector)
    QueryableTranslator.TranslateSelect(SelectExpression node)
    QueryableTranslator.Translate(Expression node)
    QueryableTranslator.TranslatePipeline(PipelineExpression node)
    QueryableTranslator.Translate(Expression node)
    QueryableTranslator.Translate(Expression node, IBsonSerializerRegistry serializerRegistry, ExpressionTranslationOptions translationOptions)
    MongoQueryProviderImpl`1.Translate(Expression expression)
    MongoQueryProviderImpl`1.Execute(Expression expression)
    MongoQueryableImpl`2.GetEnumerator()
    List`1.ctor(IEnumerable`1 collection)
    Enumerable.ToList[TSource](IEnumerable`1 source)
```
