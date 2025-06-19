using System.Globalization;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML.DataSet;
using Analiz.Domain.ValueObjects;
using Microsoft.ML;

namespace Analiz.Application.Converter;

public static class CreditCardMLConverter
{
    public static IDataView ConvertToMLDataView(MLContext mlContext, List<CreditCardModelData> data)
    {
        var mlData = data.Select(x => new CreditCardMLData
        {
            Time = x.Time,
            V1 = x.V1,
            V2 = x.V2,
            V3 = x.V3,
            V4 = x.V4,
            V5 = x.V5,
            V6 = x.V6,
            V7 = x.V7,
            V8 = x.V8,
            V9 = x.V9,
            V10 = x.V10,
            V11 = x.V11,
            V12 = x.V12,
            V13 = x.V13,
            V14 = x.V14,
            V15 = x.V15,
            V16 = x.V16,
            V17 = x.V17,
            V18 = x.V18,
            V19 = x.V19,
            V20 = x.V20,
            V21 = x.V21,
            V22 = x.V22,
            V23 = x.V23,
            V24 = x.V24,
            V25 = x.V25,
            V26 = x.V26,
            V27 = x.V27,
            V28 = x.V28,
            Amount = x.Amount,
            Label = x.Label
        }).ToList();

        var dataView = mlContext.Data.LoadFromEnumerable(mlData);

        // Feature transformation pipeline
        var pipeline = mlContext.Transforms
            // Time feature transformation
            .CustomMapping(
                (CreditCardMLData input, TimeFeatures output) =>
                {
                    const double daySeconds = 24 * 60 * 60;
                    output.TimeSin = (float)Math.Sin(2 * Math.PI * input.Time / daySeconds);
                    output.TimeCos = (float)Math.Cos(2 * Math.PI * input.Time / daySeconds);
                },
                "TimeFeatureMapping")
            // Amount feature transformation
            .Append(mlContext.Transforms.CustomMapping(
                (CreditCardMLData input, AmountFeatures output) =>
                {
                    output.LogAmount = (float)Math.Log(input.Amount + 1);
                },
                "AmountFeatureMapping"))
            // Combine features
            .Append(mlContext.Transforms.Concatenate("Features",
                new[]
                {
                    "TimeSin", "TimeCos", "LogAmount",
                    "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10",
                    "V11", "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20",
                    "V21", "V22", "V23", "V24", "V25", "V26", "V27", "V28"
                }));

        return pipeline.Fit(dataView).Transform(dataView);
    }


    public static List<TransactionData> ToTransactionDataList(IDataView dataView, MLContext mlContext)
    {
        // ML.NET üzerinden CreditCardMLData tipine dökülecek veri akışı
        var rows = mlContext.Data.CreateEnumerable<CreditCardMLData>(dataView, false);

        return rows.Select(x =>
        {
            // 1) TransactionAdditionalData VO’su oluştur
            var vo = new TransactionAdditionalData();

            // 2) V1–V28 feature’larını VFactors olarak ata
            for (var i = 1; i <= 28; i++)
            {
                var key = $"V{i}";
                // Reflection ile property bilgisi alınıyor
                var prop = typeof(CreditCardMLData).GetProperty(key);
                if (prop != null && prop.GetValue(x) is float f) vo.VFactors[key] = f;
            }

            // 3) “Time” değerini CustomValues’a ekle
            vo.CustomValues["Time"] = x.Time.ToString(CultureInfo.InvariantCulture);

            // 4) TransactionData’yi doldur
            return new TransactionData
            {
                TransactionId = Guid.NewGuid(),
                UserId = Guid.Empty, // Eğer bilgin varsa burada set et
                Amount = Convert.ToDecimal(x.Amount),
                Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)x.Time)
                    .UtcDateTime,
                IsFraudulent = x.Label,
                AdditionalData = vo
            };
        }).ToList();
    }
}