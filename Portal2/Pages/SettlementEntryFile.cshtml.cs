using Interview.Common;
using Interview.Repository.POCO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal2.Pages;

public class SettlementEntryFileModel : PageModel
{
	private readonly IFileOperationRepository _fileRepository;
	private readonly ILogger<SettlementEntryFileModel> _logger;

	[FromQuery]
	public Guid FileId { get; set; }

	public SettlementEntry[] SettlementEntries { get; set; } = Array.Empty<SettlementEntry>();
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
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading settlement entries for file {FileId}", FileId);
			ErrorMessage = $"Error loading settlement entries: {ex.Message}";
		}
	}
}
