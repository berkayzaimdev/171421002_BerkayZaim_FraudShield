namespace Analiz.Domain.Entities;

public class PerformanceMetrics
{
    public Dictionary<string, double> ModelMetrics { get; set; }
    public SystemMetrics SystemMetrics { get; set; }
    public DateTime CollectedAt { get; set; }
    public DateRange Period { get; set; }
}