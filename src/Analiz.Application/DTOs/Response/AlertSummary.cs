namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Alert özet bilgileri
/// </summary>
public class AlertSummary
{
    public int TotalAlerts { get; set; }
    public int ActiveAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int InvestigatingAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int HighAlerts { get; set; }
    public int MediumAlerts { get; set; }
    public int LowAlerts { get; set; }
    public double AverageResolutionTimeHours { get; set; }
    public DateTime LastAlertTime { get; set; }
} 