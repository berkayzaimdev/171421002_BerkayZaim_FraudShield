namespace Analiz.Domain.Entities.ML.DataSet;

public class DataStatistics
{
    public Dictionary<string, ColumnStatistics> ColumnStatistics { get; } = new();
}

public class ColumnStatistics
{
    public float Mean { get; set; }
    public float StdDev { get; set; }
    public float Min { get; set; }
    public float Max { get; set; }
    public int MissingCount { get; set; }
    public int NonZeroCount { get; set; }
}