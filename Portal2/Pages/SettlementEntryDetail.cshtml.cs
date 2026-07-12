using Interview.Common;
using Interview.Repository.POCO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Portal2.Pages;

public class SettlementEntryDetailModel : PageModel
{
	private readonly IFileOperationRepository _fileRepository;
	private readonly ILogger<SettlementEntryDetailModel> _logger;

	[FromQuery]
	public Guid SettlementEntryId { get; set; }

	public SettlementEntry Settlement { get; set; }
	public TransactionLedger[] Transactions { get; set; } = Array.Empty<TransactionLedger>();
	public TransactionLedger[] EligibleTransactions { get; set; } = Array.Empty<TransactionLedger>();
	public SettlementFileStatusStat[] FileStatusStats { get; set; } = Array.Empty<SettlementFileStatusStat>();
	public ForceMatchControlViewModel ForceMatchControl { get; set; }
	public string ErrorMessage { get; set; }

	public SettlementEntryDetailModel(
		IFileOperationRepository fileRepository,
		ILogger<SettlementEntryDetailModel> logger)
	{
		_fileRepository = fileRepository;
		_logger = logger;
	}

	public async Task OnGetAsync()
	{
		await LoadPageData();
	}

	public async Task<IActionResult> OnPostForceMatchAsync( Guid settlementEntryId, Guid transactionLedgerId )
	{
		try
		{
			if ( settlementEntryId == Guid.Empty || transactionLedgerId == Guid.Empty )
			{
				ErrorMessage = "Invalid settlement or transaction selection";
				SettlementEntryId = settlementEntryId;
				await LoadPageData();
				return Page();
			}

			await _fileRepository.UpdateSettlement( settlementEntryId, transactionLedgerId );
			return RedirectToPage( "SettlementEntryDetail", new { settlementEntryId } );
		}
		catch ( Exception ex )
		{
			_logger.LogError( ex, "Error force matching settlement {SettlementEntryId} to transaction {TransactionLedgerId}", settlementEntryId, transactionLedgerId );
			ErrorMessage = $"Error force matching settlement: {ex.Message}";
			SettlementEntryId = settlementEntryId;
			await LoadPageData();
			return Page();
		}
	}

	public async Task<IActionResult> OnPostClearMatchingAsync( Guid settlementEntryId )
	{
		try
		{
			if ( settlementEntryId == Guid.Empty )
			{
				ErrorMessage = "Invalid Settlement Entry ID";
				SettlementEntryId = settlementEntryId;
				await LoadPageData();
				return Page();
			}

			var settlement = await _fileRepository.LoadSettlement( settlementEntryId );
			if ( settlement == null )
			{
				ErrorMessage = "Settlement entry not found";
				SettlementEntryId = settlementEntryId;
				await LoadPageData();
				return Page();
			}

			await _fileRepository.ClearMatched( new[] { settlement } );
			return RedirectToPage( "SettlementEntryDetail", new { settlementEntryId } );
		}
		catch ( Exception ex )
		{
			_logger.LogError( ex, "Error clearing matching for settlement {SettlementEntryId}", settlementEntryId );
			ErrorMessage = $"Error clearing matching: {ex.Message}";
			SettlementEntryId = settlementEntryId;
			await LoadPageData();
			return Page();
		}
	}

	private async Task LoadPageData()
	{
		try
		{
			if ( SettlementEntryId == Guid.Empty )
			{
				ErrorMessage = "Invalid Settlement Entry ID";
				return;
			}

			Settlement = await _fileRepository.LoadSettlement( SettlementEntryId );

			if ( Settlement == null )
			{
				ErrorMessage = "Settlement entry not found";
				return;
			}

			Transactions = await _fileRepository.LoadTransactionBySettlementId( SettlementEntryId );

			var fileSettlements = await _fileRepository.LoadSettlementEntries( fileId: Settlement.FileImportId );
			FileStatusStats = fileSettlements
				.GroupBy( x => x.Status ?? "Unknown" )
				.Select( x => new SettlementFileStatusStat
				{
					Status = x.Key,
					Count = x.Count(),
					AmountCents = x.Sum( y => Math.Abs( y.SettledAmountCents ?? 0 ) )
				} )
				.OrderBy( x => x.Status )
				.ToArray();

			var unreconciled = await _fileRepository.LoadUnreconciled( null, null );
			EligibleTransactions = unreconciled.Transactions;

			ForceMatchControl = new ForceMatchControlViewModel
			{
				Title = "Force Match",
				EmptyMessage = "No eligible transaction ledger items available.",
				Handler = "ForceMatch",
				RouteParameterName = "settlementEntryId",
				RouteParameterValue = SettlementEntryId,
				SelectionFieldName = "transactionLedgerId",
				SubmitButtonText = "Force Match",
				Options = EligibleTransactions
					.Select( x => new ForceMatchOption
					{
						Id = x.Id,
						Label = $"{x.CapturedAt:yyyy-MM-dd HH:mm} | {x.MerchantReferenceNo} | {x.GrossAmount}" 
					} )
					.ToArray()
			};
		}
		catch ( Exception ex )
		{
			_logger.LogError( ex, "Error loading settlement entry {SettlementEntryId}", SettlementEntryId );
			ErrorMessage = $"Error loading settlement entry: {ex.Message}";
		}
	}
}

public class SettlementFileStatusStat
{
	public string Status { get; set; }
	public int Count { get; set; }
	public long AmountCents { get; set; }
}
