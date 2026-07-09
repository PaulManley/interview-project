
using Interview.Test;

namespace Test;

[Collection( "FullSetTests" )]
public class UnitTest1( TestStartupFixture Fixture )
{
	[Fact]
	public void Test1()
	{
		Assert.True( 1 == 1, "1 == 1" );
		Assert.False( 1 == 2, "1 == 2" );
		Assert.False( 10000 == 2, "10000 == 2 FAIL" );
	}

}
