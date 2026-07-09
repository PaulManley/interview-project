using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Portal2.Pages;

public class IndexModel : PageModel
{
	public IActionResult OnGet()
	{
		return RedirectToPage("UploadFilesAndRunReconciliation");
	}
}
