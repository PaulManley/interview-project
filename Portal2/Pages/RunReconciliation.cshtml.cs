using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace Portal2.Pages;

public class RunReconciliationModel : PageModel
{
	private readonly Interview.Import.Reconcile.Reconciliation _reconciliation;
	private readonly ILogger<RunReconciliationModel> _logger;

	[BindProperty]
	public DateTime? SettlementDateStart { get; set; }

	[BindProperty]
	public DateTime? SettlementDateEnd { get; set; }

	public string? Message { get; private set; }

	public RunReconciliationModel
	(
		Interview.Import.Reconcile.Reconciliation reconciliation,
		ILogger<RunReconciliationModel> logger
	)
	{
		_reconciliation = reconciliation;
		_logger = logger;
	}

	public void OnGet()
	{
	}

	public IActionResult OnPost()
	{
		try
		{
			var task = _reconciliation.Process( ToOffset( SettlementDateStart ), ToOffset( SettlementDateEnd ) );
			task.SafeFireAndForgetStandardException( "Run Reconciliation" );

			Message = "Reconciliation has been kicked off as an asynchronous process and is working in the background.";
			return Page();
		}
		catch ( Exception ex )
		{
			_logger.LogError( ex, "Error starting reconciliation" );
			Message = $"Error starting reconciliation: {ex.Message}";
			return Page();
		}
	}

	private static DateTimeOffset? ToOffset( DateTime? value )
	{
		return value.HasValue
			? new DateTimeOffset( value.Value.Date, TimeSpan.Zero )
			: null;
	}
}
