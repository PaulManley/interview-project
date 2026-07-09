using Interview.Common;
using Interview.Repository.POCO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal2.Pages;

public class TransactionLedgerFileModel : PageModel
{
	private readonly IFileOperationRepository _fileRepository;
	private readonly ILogger<TransactionLedgerFileModel> _logger;

	[FromQuery]
	public Guid FileId { get; set; }

	public TransactionLedger[] TransactionLedgers { get; set; } = Array.Empty<TransactionLedger>();
	public FileImport FileInfo { get; set; }
	public string ErrorMessage { get; set; }

	public TransactionLedgerFileModel(
		IFileOperationRepository fileRepository,
		ILogger<TransactionLedgerFileModel> logger)
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

			TransactionLedgers = await _fileRepository.LoadTransactions(fileId: FileId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading transaction ledgers for file {FileId}", FileId);
			ErrorMessage = $"Error loading transaction ledgers: {ex.Message}";
		}
	}
}
