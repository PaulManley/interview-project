using Interview.Common;
using Interview.Common.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Portal2.Pages;

public class FileListModel : PageModel
{
	private readonly IFileOperationRepository _fileRepository;
	private readonly ILogger<FileListModel> _logger;

	[BindProperty(SupportsGet = true)]
	public DateTime? CreatedStart { get; set; }

	[BindProperty(SupportsGet = true)]
	public DateTime? CreatedEnd { get; set; }

	public FileListPageModel ViewModel { get; private set; } = new();
	public string? ErrorMessage { get; private set; }

	public FileListModel(
		IFileOperationRepository fileRepository,
		ILogger<FileListModel> logger)
	{
		_fileRepository = fileRepository;
		_logger = logger;
	}

	public async Task OnGetAsync()
	{
		try
		{
			var items = await _fileRepository.LoadFileSummary( ToStartOffset( CreatedStart ), ToEndOffset( CreatedEnd ) );
			ViewModel = new FileListPageModel
			{
				CreatedStart = CreatedStart,
				CreatedEnd = CreatedEnd,
				Files = items
					.GroupBy( x => new { x.FileId, x.FileType } )
					.Select( g => BuildGroup( g.ToArray() ) )
					.OrderByDescending( x => x.Created )
					.ThenBy( x => x.FileName )
					.ToList()
				};
		}
		catch ( Exception ex )
		{
			_logger.LogError( ex, "Error loading file list" );
			ErrorMessage = $"Error loading file list: {ex.Message}";
		}
	}

	private static FileListGroup BuildGroup( FileListItem[] items )
	{
		var first = items.First();

		return new FileListGroup
		{
			FileId = first.FileId,
			FileName = first.FileName ?? string.Empty,
			RecordCount = first.RecordCount,
			Created = first.Created,
			FileType = first.FileType ?? string.Empty,
			ViewPage = GetViewPage( first.FileType ),
			Statuses = items
				.Select( x => new FileListStatusRow
				{
					Status = x.Status,
					StatusCount = x.StatusCount
				} )
				.OrderBy( x => x.Status )
				.ToList()
		};
	}

	private static string GetViewPage( string? fileType )
	{
		return fileType?.Trim() switch
		{
			"SettlementEntry" => "SettlementEntryFile",
			"TransactionLedger" => "TransactionLedgerFile",
			_ => string.Empty
		};
	}

	private static DateTimeOffset? ToStartOffset( DateTime? date )
	{
		return date.HasValue
			? new DateTimeOffset( date.Value.Date, TimeSpan.Zero )
			: null;
	}

	private static DateTimeOffset? ToEndOffset( DateTime? date )
	{
		return date.HasValue
			? new DateTimeOffset( date.Value.Date.AddDays( 1 ).AddTicks( -1 ), TimeSpan.Zero )
			: null;
	}
}
