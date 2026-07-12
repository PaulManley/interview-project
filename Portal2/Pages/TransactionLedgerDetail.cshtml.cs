using Interview.Common;
using Interview.Repository.POCO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Portal2.Pages;

public class TransactionLedgerDetailModel : PageModel
{
	private readonly IFileOperationRepository _fileRepository;
	private readonly ILogger<TransactionLedgerDetailModel> _logger;

	[FromQuery]
	public Guid TransactionLedgerId { get; set; }

	public TransactionLedger Transaction { get; set; }
	public SettlementEntry[] EligibleSettlements { get; set; } = Array.Empty<SettlementEntry>();
	public ForceMatchControlViewModel ForceMatchControl { get; set; }
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
		await LoadPageData();
	}

	public async Task<IActionResult> OnPostForceMatchAsync( Guid transactionLedgerId, Guid settlementEntryId )
	{
		try
		{
			if ( transactionLedgerId == Guid.Empty || settlementEntryId == Guid.Empty )
			{
				ErrorMessage = "Invalid transaction or settlement selection";
				TransactionLedgerId = transactionLedgerId;
				await LoadPageData();
				return Page();
			}

			await _fileRepository.UpdateSettlement( settlementEntryId, transactionLedgerId );
			return RedirectToPage( "TransactionLedgerDetail", new { transactionLedgerId } );
		}
		catch ( Exception ex )
		{
			_logger.LogError( ex, "Error force matching transaction {TransactionLedgerId} to settlement {SettlementEntryId}", transactionLedgerId, settlementEntryId );
			ErrorMessage = $"Error force matching transaction: {ex.Message}";
			TransactionLedgerId = transactionLedgerId;
			await LoadPageData();
			return Page();
		}
	}

	private async Task LoadPageData()
	{
		try
		{
			if ( TransactionLedgerId == Guid.Empty )
			{
				ErrorMessage = "Invalid Transaction Ledger ID";
				return;
			}

			Transaction = await _fileRepository.LoadTransaction( TransactionLedgerId );

			if ( Transaction == null )
			{
				ErrorMessage = "Transaction not found";
				return;
			}

			var unreconciled = await _fileRepository.LoadUnreconciled( null, null );
			EligibleSettlements = unreconciled.Settlements;

			ForceMatchControl = new ForceMatchControlViewModel
			{
				Title = "Force Match",
				EmptyMessage = "No eligible settlement entries available.",
				Handler = "ForceMatch",
				RouteParameterName = "transactionLedgerId",
				RouteParameterValue = TransactionLedgerId,
				SelectionFieldName = "settlementEntryId",
				SubmitButtonText = "Force Match",
				Options = EligibleSettlements
					.Select( x => new ForceMatchOption
					{
						Id = x.Id,
						Label = $"{x.SettlementDate:yyyy-MM-dd} | {x.MerchantRef} | {x.SettledAmountCents}" 
					} )
					.ToArray()
			};
		}
		catch ( Exception ex )
		{
			_logger.LogError( ex, "Error loading transaction {TransactionLedgerId}", TransactionLedgerId );
			ErrorMessage = $"Error loading transaction: {ex.Message}";
		}
	}
}
