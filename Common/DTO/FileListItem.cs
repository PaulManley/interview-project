using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Common.DTO;

public class FileListItem
{
	public Guid FileId { get; set; }
	public String? Status { get; set; }
	public String? FileName { get; set; }
	public int StatusCount { get; set; }
	public int RecordCount { get; set; }
	public string FileType { get; set; }
	public DateTimeOffset Created { get; set; }
}
