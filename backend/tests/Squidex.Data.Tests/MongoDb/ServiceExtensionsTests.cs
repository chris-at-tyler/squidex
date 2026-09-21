// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Squidex.MongoDb;

public class ServiceExtensionsTests
{
    [Fact]
    public void Should_not_register_mongodb_as_node_health_check()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["store:mongoDb:configuration"] = "mongodb://localhost",
                ["store:mongoDb:database"] = "Test",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSquidexMongoStore(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = Assert.Single(options.Registrations, x => x.Name == "MongoDB");

        Assert.DoesNotContain("node", registration.Tags);
    }
}
