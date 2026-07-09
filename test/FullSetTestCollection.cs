using Interview.Repository;

[assembly: CollectionBehavior( DisableTestParallelization = true )]

namespace Interview.Test;

[CollectionDefinition( "FullSetTests", DisableParallelization = true )]
public class FullSetTestCollection : ICollectionFixture<TestStartupFixture>
{
	// Intentionally empty; xUnit uses this class as a marker.
}

public sealed class TestStartupFixture : IAsyncLifetime
{
	private readonly MySqlContainerSetup _mySqlContainerSetup = new();

	public async ValueTask InitializeAsync()
	{
		await _mySqlContainerSetup.InitializeAsync();
		Console.WriteLine( "TestStartupFixture::MySql container started" );
	}

	public async ValueTask DisposeAsync()
	{
		await _mySqlContainerSetup.DisposeAsync();
		Console.WriteLine( "TestStartupFixture::MySql container disposed" );
	}
}

