namespace Analiz.Domain.Entities;

public class SystemStatus
{
    public bool IsHealthy { get; set; }
    public ComponentStatus DatabaseStatus { get; set; }
    public ComponentStatus ModelsStatus { get; set; }
    public ComponentStatus FeatureExtractorStatus { get; set; }
    public DateTime LastChecked { get; set; }
}