using System;
using System.Collections.Generic;

namespace Portal2.Pages;

public class FileListPageModel
{
	public DateTimeOffset? CreatedStart { get; set; }
	public DateTimeOffset? CreatedEnd { get; set; }
	public List<FileListGroup> Files { get; set; } = new();
}

public class FileListGroup
{
	public Guid FileId { get; set; }
	public string FileName { get; set; } = string.Empty;
	public int RecordCount { get; set; }
	public DateTimeOffset Created { get; set; }
	public string FileType { get; set; } = string.Empty;
	public string ViewPage { get; set; } = string.Empty;
	public List<FileListStatusRow> Statuses { get; set; } = new();
}

public class FileListStatusRow
{
	public string? Status { get; set; }
	public int StatusCount { get; set; }
}