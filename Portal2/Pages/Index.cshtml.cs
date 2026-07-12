using Interview.DBMigrator.Migration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Markdig;

namespace Portal2.Pages;

public class IndexModel : PageModel
{
	public string HtmlContent { get; private set; } = string.Empty;

	public void OnGet()
	{
		//return RedirectToPage("UploadFilesAndRunReconciliation");
		string resourceName = $"Interview.Portal.ReadMe.md";

		using Stream? stream = typeof( IndexModel ).Assembly.GetManifestResourceStream( resourceName );
		if ( stream is null )
			throw new InvalidOperationException( $"Embedded resource not found: {resourceName}" );

		using StreamReader reader = new StreamReader( stream );
		string md = reader.ReadToEnd();

		HtmlContent = Markdown.ToHtml( md );
	}
}
