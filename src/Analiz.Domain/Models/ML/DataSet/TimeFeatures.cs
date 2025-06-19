namespace Analiz.Domain.Entities.ML.DataSet;

public class TimeFeatures
{
    public float TimeSin { get; set; }
    public float TimeCos { get; set; }
    public float DayFeature { get; set; } // DayOfWeek yerine
    public float HourFeature { get; set; } // HourOfDay yerine
}