using Microsoft.ML.Data;

namespace Analiz.Domain.Entities.ML.DataSet;

public class SplitDataModel
{
    public float Time { get; set; }
    public float Amount { get; set; }
    public bool Label { get; set; }

    // Split için key
    public float SplitKey { get; set; }

    // V1-V28 özellikleri
    public float[] Features { get; set; }

    // Türetilmiş özellikler
    public float TimeSin { get; set; }
    public float TimeCos { get; set; }
    public float DayOfWeek { get; set; }
    public float HourOfDay { get; set; }
    public float AmountLog { get; set; }

    // V1-V28'e kolay erişim için indexer
    public float this[int index]
    {
        get
        {
            if (index < 0 || index >= Features.Length)
                throw new IndexOutOfRangeException();
            return Features[index];
        }
        set
        {
            if (index < 0 || index >= Features.Length)
                throw new IndexOutOfRangeException();
            Features[index] = value;
        }
    }

    // Constructor
    public SplitDataModel()
    {
        Features = new float[28];
    }
}