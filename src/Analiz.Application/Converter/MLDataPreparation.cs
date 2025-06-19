using Analiz.Domain.Entities.ML.DataSet;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Analiz.Application.Converter;

public static class MLDataPreparation
{
    public static IDataView PrepareData(MLContext mlContext, List<CreditCardModelData> data)
    {
        var mlData = data.Select(x => new
        {
            Label = x.Label,
            Time = x.Time,
            Amount = x.Amount,
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
            V28 = x.V28
        }).ToList();

        var schema = SchemaDefinition.Create(typeof(CreditCardMLModel));
        return mlContext.Data.LoadFromEnumerable(mlData, schema);
    }
}