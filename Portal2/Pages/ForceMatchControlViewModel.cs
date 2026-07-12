using System;

namespace Portal2.Pages;

public class ForceMatchControlViewModel
{
	public string Title { get; set; }
	public string EmptyMessage { get; set; }
	public string Handler { get; set; }
	public string RouteParameterName { get; set; }
	public Guid RouteParameterValue { get; set; }
	public string SelectionFieldName { get; set; }
	public string SubmitButtonText { get; set; }
	public ForceMatchOption[] Options { get; set; } = Array.Empty<ForceMatchOption>();
}

public class ForceMatchOption
{
	public Guid Id { get; set; }
	public string Label { get; set; }
}
