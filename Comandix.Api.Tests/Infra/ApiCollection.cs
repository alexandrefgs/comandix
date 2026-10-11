namespace Comandix.Api.Tests.Infra;

[CollectionDefinition(Nome)]
public class ApiCollection : ICollectionFixture<ComandixApiFactory>
{
    public const string Nome = "Api";
}
