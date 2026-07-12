# Settlement Project Demo

Hello, thanks for checking this out.

I worked on this the first day, and then came back to it on Sat/Sun for half days.  This was a 2 day project for me ( or two and a little bit to write docs and do the video ).

This is not a 2-3 hour project.  
This is not a 4-6 hour project.  
This is a full 2 day project.  

Lines of Code:  ~6000

Short Video:  

## Project Overview
You are building a Transaction Ledger and Settlement File matching and error platform.  
Goals:
- Make sure your processes are testable
- Include a unit testing library
- Upload Transaction Ledger files
- Upload Settlement Entry files
- Error Check those files for data validation issues
- Don't allow duplicate file uploads
- Should be persistent across restarts
- Setup the database
- Match Settlements to Transactions
-- There are a lot of requirements here and rules
-- Generally matching exact
-- Match with amount wiggle room
-- Match split settlements
-- Match Refund items
- UI to Upload files, run reconciliation, show details
- UI enough for a person to do research into items
- Import the Fee Schedule on the fly
- Don't hard code secrets
- Don't hardcode services
- Reporting to show amounts/counts per file reconciliation
- Show prompts used for AI if used

On top of that I'm also doing a database migration project.


## How to Run
These will all start-up docker to run mysql.  There is a persisted volume, but the unit tests will delete all the data from most tests.
The only environmental variables are the data path and mysql password.

### Visual Studio
If you're in Visual Studio 2026 ( any edition ), you should just be able to hit F5 if you have the Interview.Portal set as your startup app.
You can also run the unit tests.

### Command line ( Unit Tests )
If you're in windows, the Portal creates and exe you can run.  You can also dotnet run 

If you're at the root ( where the solution file is )
dotnet test Test\Interview.Test.csproj --no-build -c Debug

```csharp
PS C:\Dev\Career\interview-project> dotnet test Test\Interview.Test.csproj --no-build -c Debug
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.9)
[xUnit.net 00:00:00.31]   Discovering: Interview.Test
[xUnit.net 00:00:00.66]   Discovered:  Interview.Test
[xUnit.net 00:00:00.98]   Starting:    Interview.Test
[xUnit.net 00:00:30.43]   Finished:    Interview.Test
  Interview.Test test net10.0 succeeded (31.7s)

Test summary: total: 14, failed: 0, succeeded: 14, skipped: 0, duration: 31.6s
Build succeeded in 32.3s
PS C:\Dev\Career\interview-project>
```

### Command Line ( Platform )
If you're at the root ( where the solution file is )
```csharp
dotnet run --project Portal2/Interview.Portal.csproj --urls "http://0.0.0.0:5000"
```

OR full build and run the DLL
If you're at the root ( where the solution file is )
```csharp
cd Portal2
dotnet build
dotnet publish -c Debug
cd bin\Debug\net10.0\publish
dotnet Interview.Portal.dll
```
NOTE:  You have to CD into the publish directory because we serve all the static styling content out of the "current" directory.

Note:  This runs on port 5000 without SSL
http://localhost:5000

Also it should output what port it's running on since in some conditions .NET will pick a random port.

# Database
Simple mysql database.  I might consider these tables "landing" before you run accounting on them.  IE, these are pretty basic and without much in the way of indices or optimization.  Most data is stored as string.  I was fairly lax on the data import ( you can import bad data ).  

## Idempotency
I hash the file and include that as well as the Path/FileName a being unique indices.  I don't hash the rows though.

## Data Validation
I almost always insert the data, but add an error and error code in the row.  There's another file "goofy1_internal_transactions.csv" that is more broken, and that still imports.

There's also configuration built in on what to do for certain fail situations ( defaults, ignore, mark as errored on validation )

# What's completed, what's not

## Unit Tets
There are unit tests, they all inject the resources downward so you should be able to seperate out different systems if you want to.
I did not add individual unit tests for the Pattern Matching concrete implementations.
I might also move to TUnit later.

## UI
It is basic.  But it's enough to allow an operator to do some basic poking around and finding transactions.  There's no search functionality.
I did end up putting in List Files and Reconcile and a Report at the top of every Settlement Entry File page.

## BIGGEST Change I'd make
I'd probably skip the UI and DB all together and just have Unit testing to get the matching/reconciliation functionality 100%.  That is more attainable in 6 hours.

# Improvements

- This could be cleaned up quite a bit in general.
- Unit tests for patterns can be increased greatly
- Reporting is missing
- No API, this is essentially postbacks internally instead of an JWT I could call APIs with
- No Ajax/HTMX
- There's no OTEL, no AsyncContext to have the Identity follow the execution around
- Settlement and Transaction NormalizeWorkflow is basically the same, so they could be based off either a step engine or a common baseclass
- Error messages could be nicer

# AI Usage
I did, off and on.  Sometimes it gets in the way, sometimes not.  I turn off the LSP functions ( no auto-suggest ).
Check out PromptExamples.md


