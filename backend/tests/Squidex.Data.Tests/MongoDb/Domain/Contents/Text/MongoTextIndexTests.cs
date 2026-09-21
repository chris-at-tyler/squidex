// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson;
using MongoDB.Driver;
using Squidex.Domain.Apps.Core.Apps;
using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Domain.Apps.Entities;
using Squidex.Domain.Apps.Entities.Contents.Text;
using Squidex.Events.Mongo;
using Squidex.Infrastructure;
using Squidex.MongoDb.TestHelpers;

namespace Squidex.MongoDb.Domain.Contents.Text;

[Trait("Category", "Dependencies")]
[Collection(MongoFixtureCollection.Name)]
public class MongoTextIndexTests(MongoFixture fixture) : TextIndexerTests
{
    public override bool SupportsQuerySyntax => false;

    public override bool SupportsGeo => true;

    public override async Task<ITextIndex> CreateSutAsync()
    {
        var sut = new MongoTextIndex(fixture.Database, string.Empty, MongoDerivate.MongoDB);

        await sut.InitializeAsync(default);
        return sut;
    }

    [Fact]
    public async Task Should_create_non_sparse_app_schema_index()
    {
        await CreateSutAsync();

        var collection = fixture.Database.GetCollection<MongoTextIndexEntity<List<MongoTextIndexEntityText>>>("TextIndex2");
        using var cursor = await collection.Indexes.ListAsync();
        var indexes = await cursor.ToListAsync();

        var index = Assert.Single(indexes, x => x["name"] == "a_1_s_1");

        Assert.False(index.TryGetValue("sparse", out var sparse) && sparse.ToBoolean());
    }

    [Fact]
    public async Task Should_delete_schema_only_from_requested_app()
    {
        var sut = await CreateSutAsync();
        var collection = fixture.Database.GetCollection<MongoTextIndexEntity<List<MongoTextIndexEntityText>>>("TextIndex2");

        var app1 = new App { Id = DomainId.NewGuid() };
        var app2 = new App { Id = DomainId.NewGuid() };
        var schema = new Schema { Id = DomainId.NewGuid() };

        await collection.InsertManyAsync(
        [
            new MongoTextIndexEntity<List<MongoTextIndexEntityText>>
            {
                Id = ObjectId.GenerateNewId(),
                AppId = app1.Id,
                SchemaId = schema.Id,
                ContentId = DomainId.NewGuid(),
            },
            new MongoTextIndexEntity<List<MongoTextIndexEntityText>>
            {
                Id = ObjectId.GenerateNewId(),
                AppId = app2.Id,
                SchemaId = schema.Id,
                ContentId = DomainId.NewGuid(),
            },
        ]);

        try
        {
            await ((IDeleter)sut).DeleteSchemaAsync(app1, schema, default);

            var remaining = await collection.Find(x => x.SchemaId == schema.Id).ToListAsync();

            var entity = Assert.Single(remaining);
            Assert.Equal(app2.Id, entity.AppId);
        }
        finally
        {
            await collection.DeleteManyAsync(x => x.SchemaId == schema.Id);
        }
    }

    [Fact]
    public async Task Should_retrieve_all_stopwords_for_english_query()
    {
        await CreateTextAsync(Ids1[0], "de", "and und");
        await CreateTextAsync(Ids2[0], "en", "and und");

        await SearchText(expected: Ids1, text: "de:and");
    }

    [Fact]
    public async Task Should_retrieve_all_stopwords_for_german_query()
    {
        await CreateTextAsync(Ids1[0], "de", "and und");
        await CreateTextAsync(Ids2[0], "en", "and und");

        await SearchText(expected: Ids2, text: "en:und");
    }
}
