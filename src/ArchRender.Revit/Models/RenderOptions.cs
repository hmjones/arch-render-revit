namespace ArchRender.Revit.Models;

public class RenderOptions
{
    public string RenderType { get; set; } = "exterior";
    public string Season { get; set; } = "Summer";
    public string TimeOfDay { get; set; } = "Noon";
    public string Environment { get; set; } = "Suburban";
    public string AspectRatio { get; set; } = "4:3";
    public string MaterialDetails { get; set; } = "";
    public bool UseUltraModel { get; set; } = false;
}

public class RenderResult
{
    public string ImageUrl { get; set; } = "";
    public string GenerationId { get; set; } = "";
    public int CreditsRemaining { get; set; }
}
