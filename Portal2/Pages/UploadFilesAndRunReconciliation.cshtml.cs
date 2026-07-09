using Interview.Common;
using Interview.Import.Settlement;
using Interview.Import.Transaction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Portal2.Pages;

public class UploadFilesAndRunReconciliationModel : PageModel
{
	private readonly Interview.Import.Settlement.NormalizeWorkflow _settlementWorkflow;
	private readonly Interview.Import.Transaction.NormalizeWorkflow _transactionWorkflow;
	private readonly IFileOperationRepository _fileRepository;
	private readonly ILogger<UploadFilesAndRunReconciliationModel> _logger;
	private readonly Interview.Import.Reconcile.Reconciliation _reconciler;

	[BindProperty]
	public IFormFile SettlementFile { get; set; }

	[BindProperty]
	public IFormFile TransactionLedgerFile { get; set; }

	public string SettlementFileId { get; set; }
	public int SettlementRowCount { get; set; }

	public string TransactionFileId { get; set; }
	public int TransactionRowCount { get; set; }

	public string ErrorMessage { get; set; }

	public UploadFilesAndRunReconciliationModel
	(
		Interview.Import.Settlement.NormalizeWorkflow settlementWorkflow,
		Interview.Import.Transaction.NormalizeWorkflow transactionWorkflow,
		IFileOperationRepository fileRepository,
		ILogger<UploadFilesAndRunReconciliationModel> logger,
		Interview.Import.Reconcile.Reconciliation reconciler
	)
	{
		_settlementWorkflow = settlementWorkflow;
		_transactionWorkflow = transactionWorkflow;
		_fileRepository = fileRepository;
		_logger = logger;
		_reconciler = reconciler;
	}

	public void OnGet()
	{
	}

	public async Task<IActionResult> OnPostAsync()
	{
		try
		{
			bool settlementFile = false;
			bool tranFile = false;
			// Load fee schedule
			string feeScheduleJson = System.IO.File.ReadAllText(Interview.Common.Config.FeeSchedulePath);
			var feeSchedule = new FeeSchedule(feeScheduleJson);

			// Process Settlement Entry File
			if (SettlementFile != null && SettlementFile.Length > 0)
			{
				using (var stream = SettlementFile.OpenReadStream())
				{
					string uploadPath = "uploads";
					Directory.CreateDirectory(uploadPath);

					var settlementFileId = await _settlementWorkflow.TransactionWorkflow(
						stream,
						DateTimeOffset.UtcNow,
						uploadPath,
						SettlementFile.FileName,
						feeSchedule);

					SettlementFileId = settlementFileId.ToString();

					var fileImport = _fileRepository.Load(uploadPath, SettlementFile.FileName);
					if (fileImport != null)
					{
						SettlementRowCount = fileImport.RecordCount;
						settlementFile = ( SettlementRowCount > 0 );
					}
				}
			}

			// Process Transaction Ledger File
			if (TransactionLedgerFile != null && TransactionLedgerFile.Length > 0)
			{
				using (var stream = TransactionLedgerFile.OpenReadStream())
				{
					string uploadPath = "uploads";
					Directory.CreateDirectory(uploadPath);

					var transactionFileId = await _transactionWorkflow.TransactionWorkflow(
						stream,
						DateTimeOffset.UtcNow,
						uploadPath,
						TransactionLedgerFile.FileName,
						feeSchedule);

					TransactionFileId = transactionFileId.ToString();

					var fileImport = _fileRepository.Load(uploadPath, TransactionLedgerFile.FileName);
					if (fileImport != null)
					{
						TransactionRowCount = fileImport.RecordCount;
						tranFile = ( TransactionRowCount > 0 );
					}
				}
			}

			if ( tranFile && settlementFile )
			{
				var nonReconciledItems = await _fileRepository.LoadUnreconciled();
				await _reconciler.Process( nonReconciledItems.Settlements, nonReconciledItems.Transactions );
			}

			return Page();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing files");
			ErrorMessage = $"Error: {ex.Message}";
			return Page();
		}
	}
}
