using Interview.Common;
using Interview.DBMigrator;
using Interview.Repository;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Test.Common;


[Collection( "FullSetTests" )]
public class Basics( TestStartupFixture Fixture )
{
	[Fact]
	public async Task LoadFees()
	{
		string path = Interview.Common.Config.FeeSchedulePath;
		string feeSchedule = System.IO.File.ReadAllText( path );
		FeeSchedule fs = new FeeSchedule(feeSchedule);

		Assert.True( fs.ProcessorMarkup.FlatCents == 5 );
		Assert.True( fs.ProcessorMarkup.Percent == 0.003M );

		Assert.True( fs.GetFlatAndPercent( "VISA" ).FlatCents == 10 );
		Assert.True( fs.GetFlatAndPercent( "MASTERCARD" ).FlatCents == 10 );
		Assert.True( fs.GetFlatAndPercent( "AMEX" ).FlatCents == 15 );
		Assert.True( fs.GetFlatAndPercent( "DISCOVER" ).FlatCents == 10 );

		Assert.True( fs.GetFlatAndPercent( "VISA" ).Percent == 0.018M );
		Assert.True( fs.GetFlatAndPercent( "MASTERCARD" ).Percent == 0.019M );
		Assert.True( fs.GetFlatAndPercent( "AMEX" ).Percent == 0.025M );
		Assert.True( fs.GetFlatAndPercent( "DISCOVER" ).Percent == 0.020M );

		Assert.True( fs.GetFlatAndPercentSafe( "Other" ).Percent == 0.0M );
		Assert.True( fs.GetFlatAndPercentSafe( "Other" ).FlatCents == 0 );

		Assert.True( fs.GetFlatAndPercentSafe( Guid.NewGuid().ToBase30String() ).FlatCents == 0 );

		Assert.True( fs.GetFlatAndPercentSafe( "DISCOVER" ).FlatCents == 10 );
		Assert.True( fs.GetFlatAndPercentSafe( "VISA" ).Percent == 0.018M );
	}
}
