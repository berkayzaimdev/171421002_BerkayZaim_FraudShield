using Analiz.Domain.Entities.ML.DataSet;
using Microsoft.ML;

namespace Analiz.Application.Extensions;

public static class CalculationExtensions
{
    public static double CalculateRSquared(
        this IEnumerable<dynamic> points,
        double slope,
        double intercept)
    {
        var pointsList = points.Select(p => new
        {
            X = Convert.ToDouble(p.X),
            Y = Convert.ToDouble(p.Y)
        }).ToList();

        var yMean = pointsList.Average(p => p.Y);
        var totalSS = pointsList.Sum(p => Math.Pow(p.Y - yMean, 2));
        var residualSS = pointsList.Sum(p => Math.Pow(p.Y - (slope * p.X + intercept), 2));

        return 1 - residualSS / totalSS;
    }

    public static double CompareAmounts(decimal amount1, double amount2)
    {
        return Convert.ToDouble(amount1).CompareTo(amount2);
    }

    public static IDataView StratifiedSample(
        this IDataView data,
        MLContext mlContext,
        double fraction)
    {
        return mlContext.Data.LoadFromEnumerable(
            mlContext.Data.CreateEnumerable<CreditCardModelData>(data, false)
                .GroupBy(x => x.Label)
                .SelectMany(g => g.OrderBy(x => Guid.NewGuid()).Take((int)(g.Count() * fraction)))
        );
    }
}