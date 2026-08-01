using Xunit;

namespace TicketingSystem.Api.IntegrationTests;

[CollectionDefinition(CollectionName)]
public sealed class IntegrationTestCollection : ICollectionFixture<Helpers.PostgresTestContainer>
{
    public const string CollectionName = "IntegrationTests";
}
