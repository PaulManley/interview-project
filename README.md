# Settlement Project Demo

Hello, thanks for checking this out.

Short Video:  https://www.loom.com/share/e30fec0761ff46cc9351b1726b9d1bdc

## How to Run
These will all start-up docker to run mysql.  There is a persisted volume, but the unit tests will delete all the data from most tests.
The only environmental variables are the data path and mysql password.

### Visual Studio
If you're in Visual Studio 2026 ( any edition ), you should just be able to hit F5 if you have the Interview.Portal set as your startup app.
You can also run the unit tests.

### Command line
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

# Database
Simple mysql database.  I might consider these tables "staging" before you run accounting on them.  IE, these are pretty basic and without much in the way of indices or optimization.  Most data is stored as string.  I was fairly lax on the data import ( you can import bad data ).  

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
It is basic.  I didn't put in a "List Files" or "List Unreconciled Item" or "Reports" page.  But when you import a Transaction Ledger file and a Settlement file we run the Reconciliation.

# Improvements

- I would run reconciliation as a background process
- The reconciliation process is n-squared or worse.  It is very ineffecient.
- This could be cleaned up quite a bit in general.
- We're missing nicer logging ( I didn't even put in the color console, just bland console ).
- Unit tests for patterns can be increased greatly
- Reporting is missing
- No API, this is essentially postbacks internally instead of an JWT I could call APIs with
- No Ajax/HTMX
- I didn't create "Register" functions per project to hide more implementation details
- It would make sense to first call a sproc to do the initial round of matching and then leave "left-over" matching to the more processor intensive Pattern Matching loop.
- I prefer to make custom logger static instead of injecting it and I prefer sending json logs to greylog ( only thing I make global static context since the logger is so common ).
- There's no OTEL, no AsyncContext to have the Identity follow the execution around
- Settlement and Transaction NormalizeWorkflow is basically the same, so they could be based off either a step engine or a common baseclass
- Error messages could be nicer

# AI Usage
I did, off and on.  Sometimes it gets in the way, sometimes not.  I turn off the LSP functions ( no auto-suggest ).
Check out PromptExamples.md


