namespace Analiz.Domain.Entities;

public class SystemMetrics
{
    public double AverageResponseTime { get; set; }
    public int RequestsPerSecond { get; set; }
    public double ErrorRate { get; set; }
    public Dictionary<string, double> ModelLatencies { get; set; }
    public int ActiveConnections { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
}