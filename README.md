# MongoDb C# Driver Bugs

This repo reproduce some bugs of [MongoDB C# Driver](https://github.com/mongodb/mongo-csharp-driver).

## 1- Using `Contains()` with `false` Check

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