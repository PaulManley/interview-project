using Interview.Common;
using Interview.Repository.POCO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Portal2.Pages;

public class SettlementEntryFileModel : PageModel
{
	private readonly IFileOperationRepository _fileRepository;
	private readonly ILogger<SettlementEntryFileModel> _logger;

	[FromQuery]
	public Guid FileId { get; set; }

	public SettlementEntry[] SettlementEntries { get; set; } = Array.Empty<SettlementEntry>();
	public SettlementEntryFileStatusStat[] FileStatusStats { get; set; } = Array.Empty<SettlementEntryFileStatusStat>();
	public FileImport FileInfo { get; set; }
	public string ErrorMessage { get; set; }

	public SettlementEntryFileModel(
		IFileOperationRepository fileRepository,
		ILogger<SettlementEntryFileModel> logger)
	{
		_fileRepository = fileRepository;
		_logger = logger;
	}

	public async Task OnGetAsync()
	{
		try
		{
			if (FileId == Guid.Empty)
			{
				ErrorMessage = "Invalid File ID";
				return;
			}

			SettlementEntries = await _fileRepository.LoadSettlementEntries(fileId: FileId);
			FileStatusStats = SettlementEntries
				.GroupBy( x => x.Status ?? "Unknown" )
				.Select( x => new SettlementEntryFileStatusStat
				{
					Status = x.Key,
					Count = x.Count(),
					SettledAmountCents = x.Sum( y => Math.Abs( y.SettledAmountCents ?? 0 ) ),
					InterchangeFeeCents = x.Sum( y => Math.Abs( y.InterchangeFeeCents ?? 0 ) ),
					ProcessorFeeCents = x.Sum( y => Math.Abs( y.ProcessorFeeCents ?? 0 ) )
				} )
				.OrderBy( x => x.Status )
				.ToArray();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading settlement entries for file {FileId}", FileId);
			ErrorMessage = $"Error loading settlement entries: {ex.Message}";
		}
	}
}

public class SettlementEntryFileStatusStat
{
	public string Status { get; set; }
	public int Count { get; set; }
	public long SettledAmountCents { get; set; }
	public long InterchangeFeeCents { get; set; }
	public long ProcessorFeeCents { get; set; }
}
