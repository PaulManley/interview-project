# Prompt 1

Can you create the fluent migrator database setup for me.

First, create the database if it doesn't exist.  Then in the first migration, create a table called "TransactionImport" that has an Id ( Guid ), and FileName ( VARCHAR 256 ) and Path ( VARCHAR 256 ).

Take in a Interview.Repository.ConnectionFactory and you can use that to get the conn string if you need it to create the DB.  Assume SQL Server for right now.

Edit file:  C:\Dev\Interview\PaymentOne\DBMigrator\Migration\v20260708_0001_Start.cs

Create tables for TransactionLedger and SettlementEntry based on these tables:
CREATE TABLE TransactionLedger
(
	Id					CHAR(36) primary key,
	FileImportId		CHAR(36),
	RefTranId			VARCHAR(64), -- internal_txn_id,
	MerchantId			VARCHAR(64), -- merchant_id,
	MerchantReferenceNo	VARCHAR(64), -- merchant_ref,
	CardType			VARCHAR(16), -- card_type,
	CardLast4			VARCHAR(4),	-- card_last4,
	GrossAmount			BIGINT, -- gross_amount,
	Currency			CHAR(3), -- currency,
	TranType			VARCHAR(16), -- type,
	CapturedAt			TIMESTAMP, -- captured_at

	Created				timestamp default(now() )
)

		 
CREATE TABLE SettlementEntry
(
	Id					CHAR(36) primary key,
	FileImportId		CHAR(36),
	NetworkRef			VARCHAR(64),
	MerchantId			VARCHAR(64),
	CardType			VARCHAR(16), -- card_type,
	CardLast4			VARCHAR(4),	-- card_last4,
	SettledAmount		bigint,
	InterchangeFee		BIGINT,
	ProcessorFee		BIGINT,
	Currency			CHAR(3),
	SettlementDate		DATE,
    
	Created				TIMESTAMP
);

Add an index on FileImportId on both of them.
Create a foreign key on FileImportId to the FileImport table on column Id.

Make sure that Created has a default of UTC now.
Make sure that Id has a default of uuid().


# Prompt 2

Write a simple class for the fee schedule.  It should parse the json into the fields.
Fee Schedule JSon is located here:  C:\Dev\Interview\PaymentOne\fee_schedule.json

# Prompt 3

Read files:
- C:\Dev\Interview\PaymentOne\DBMigrator\Migration\v20260708_0001_Start.cs

Create simple POCO classes to write to the database that match the tables.
The files are already created, you have to fill in the properties and attributes.
Update these files if not already complete:
- C:\Dev\Interview\PaymentOne\Repository\POCO\FileImport.cs
- C:\Dev\Interview\PaymentOne\Repository\POCO\SettlementEntry.cs
- C:\Dev\Interview\PaymentOne\Repository\POCO\TransactionLedger.cs

# Prompt 4


Files:
- C:\Dev\Interview\PaymentOne\Repository\FileOperation.cs
- C:\Dev\Interview\PaymentOne\Repository\POCO\FileImport.cs

Update FileOperation class and create some methods.  Use the Conn to access the database.  Create a new connection every time.

- CheckHash - takes a Path, FileName, and Hash and checks if the file is already in the FileImport table.  Checks by Path and File name first, and then also checks by Hash.  Returns true if either of those are true.
- Save - takes a Poco.FileImport and first checks if it exists in the database by Id.  If it doesn't, it inserts it.  If it does exist then it updates the fields.
- Load - Pass in a Path and a Name and retrieve the FileImport.

File: C:\Dev\Interview\PaymentOne\DBMigrator\Migration\v20260708_0001_Start.cs
Put a unique index on FileImport FileName and FilePath.
Put a unqiue index on FileHash.

# Prompt 5

Files:
- C:\Dev\Interview\PaymentOne\Import\Settlement\NormalizeWorkflow.cs
- C:\Dev\Interview\PaymentOne\Import\Transaction\TranDataImporter.cs
- C:\Dev\Interview\PaymentOne\Common\POCO\SettlementEntry.cs
- C:\Dev\Interview\PaymentOne\Common\POCO\TransactionLedger.cs
- C:\Dev\Interview\PaymentOne\Common\POCO\AuditBase.cs
- C:\Dev\Interview\PaymentOne\Import\ImporterBase.cs

Read the NormalizeWorkflow for TransactionLedger, we want to do the same thing for NormalizeWorkflow for SettlementEntry.  Compute the original expected gross amount in cents.

Edit File: C:\Dev\Interview\PaymentOne\Import\Settlement\NormalizeWorkflow.cs

# Prompt 6

In file:  C:\Dev\Interview\PaymentOne\Test\FullSetTestCollection.cs
Get MySql up and running in test container in a fixture.  

Files:
- C:\Dev\Interview\PaymentOne\Repository\MySqlContainer.cs
- C:\Dev\Interview\PaymentOne\Test\FullSetTestCollection.cs
- C:\Dev\Interview\PaymentOne\Portal2\Program.cs

Let's move the TestContainer with MySql into the MySqlContainerSetup class.  The idea is that I want to be able to run this from either the unit tests, or for it to start automatically inside the Interview.Portal.

Update Program to also start the test container at run time and then shut it down once the app is done.

MySqlContainerSetup should do the work so it's shared between the unit testing and the portal.

# Prompt 7

Read files:
- C:\Dev\Interview\PaymentOne\Test\Import\Reconcile.cs
- C:\Dev\Interview\PaymentOne\Import\Reconcile\Reconciliation.cs

I'm getting error:  System.InvalidOperationException: 'Unable to resolve service for type 'Interview.Import.Reconcile.IMatchPatternSettlement[]' while attempting to activate 'Interview.Import.Reconcile.Reconciliation'.'

On this line:  var reconcileProcess = provider.GetRequiredService<Interview.Import.Reconcile.Reconciliation>();

I checked the Register function and after the scan happens there are multiple IMatchPatternSettlement implementations registered.  How do I instantiate this class?



# Prompt 8

On SettlementEntryFile.cshtml make @entry.TransactionLedgerId a link to a new page called Transaction Ledger Detail.  When there isn't a linked Transaction, then in orange display "Not Reconciled".

Create another page called Transaction Ledger Detail.  It takes a TransactionLedger Id as a Guid in the query string. 
Transaction Ledger Detail will use the IFileOperationRepository to call LoadTransaction. It will display that transaction in a detail view on the page.