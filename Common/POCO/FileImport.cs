using LinqToDB.Mapping;

namespace Interview.Repository.POCO;

// Normally, these would be fully internal to the repository layer.
// But since this is a quickie project I'm not going to do that.

[Table( "FileImport" )]
public class FileImport : AAuditBase
{
	[Column( Length = 256 ), NotNull]
	public string FilePath { get; set; }

	[Column( Length = 256 ), NotNull]
	public string FileHash { get; set; }

	[Column( Length = 256 ), NotNull]
	public string FileName { get; set; }

	[Column, NotNull]
	public int RecordCount { get; set; }

	[Column( Length = 256 ), NotNull]
	public string FileType { get; set; }
}

