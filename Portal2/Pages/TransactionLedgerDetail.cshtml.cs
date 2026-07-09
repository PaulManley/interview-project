using Interview.Common;
using Interview.Repository.POCO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace Portal2.Pages;

public class TransactionLedgerDetailModel : PageModel
{
	private readonly IFileOperationRepository _fileRepository;
	private readonly ILogger<TransactionLedgerDetailModel> _logger;

	[FromQuery]
	public Guid TransactionLedgerId { get; set; }

	public TransactionLedger Transaction { get; set; }
	public string ErrorMessage { get; set; }

	public TransactionLedgerDetailModel(
		IFileOperationRepository fileRepository,
		ILogger<TransactionLedgerDetailModel> logger)
	{
		_fileRepository = fileRepository;
		_logger = logger;
	}

	public async Task OnGetAsync()
	{
		try
		{
			if (TransactionLedgerId == Guid.Empty)
			{
				ErrorMessage = "Invalid Transaction Ledger ID";
				return;
			}

			Transaction = await _fileRepository.LoadTransaction(TransactionLedgerId);

			if (Transaction == null)
			{
				ErrorMessage = "Transaction not found";
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading transaction {TransactionLedgerId}", TransactionLedgerId);
			ErrorMessage = $"Error loading transaction: {ex.Message}";
		}
	}
}
