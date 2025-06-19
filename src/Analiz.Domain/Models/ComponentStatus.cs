namespace Analiz.Domain.Entities;

public class ComponentStatus
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; }
    public string Details { get; set; }
    public Dictionary<string, string> Metrics { get; set; }
}