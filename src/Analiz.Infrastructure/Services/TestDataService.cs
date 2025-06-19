using System.Globalization;
using System.Reflection;
using Analiz.Application.Interfaces.Infrastructure;
using Analiz.Domain.Entities.ML.DataSet;
using Microsoft.Extensions.Logging;

namespace Analiz.Infrastructure.Services;

public class TestDataService : ITestDataService
{
    private readonly ILogger<TestDataService> _logger;

    public TestDataService(ILogger<TestDataService> logger)
    {
        _logger = logger;
    }

    public async Task<List<CreditCardModelData>> LoadCreditCardDataAsync()
    {
        try
        {
            // Doğru dosya yolu belirleniyor
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "TestData", "creditcard.csv");

            // Dosyanın var olup olmadığını kontrol et
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Test verisi bulunamadı: {filePath}");

            using var reader = new StreamReader(filePath);
            var data = new List<CreditCardModelData>();

            var headerLine = await reader.ReadLineAsync();
            if (!ValidateHeader(headerLine))
                throw new InvalidDataException("CSV başlık formatı beklendiği gibi değil");

            string line;
            var lineNumber = 1;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    line = line.Replace("\"", "");
                    var values = line.Split(',');

                    if (values.Length != 31)
                    {
                        _logger.LogWarning("Satır {LineNumber}: Beklenen kolon sayısı tutmuyor", lineNumber);
                        continue;
                    }

                    var record = new CreditCardModelData
                    {
                        Time = ParseFloat(values[0]),
                        V1 = ParseFloat(values[1]),
                        V2 = ParseFloat(values[2]),
                        V3 = ParseFloat(values[3]),
                        V4 = ParseFloat(values[4]),
                        V5 = ParseFloat(values[5]),
                        V6 = ParseFloat(values[6]),
                        V7 = ParseFloat(values[7]),
                        V8 = ParseFloat(values[8]),
                        V9 = ParseFloat(values[9]),
                        V10 = ParseFloat(values[10]),
                        V11 = ParseFloat(values[11]),
                        V12 = ParseFloat(values[12]),
                        V13 = ParseFloat(values[13]),
                        V14 = ParseFloat(values[14]),
                        V15 = ParseFloat(values[15]),
                        V16 = ParseFloat(values[16]),
                        V17 = ParseFloat(values[17]),
                        V18 = ParseFloat(values[18]),
                        V19 = ParseFloat(values[19]),
                        V20 = ParseFloat(values[20]),
                        V21 = ParseFloat(values[21]),
                        V22 = ParseFloat(values[22]),
                        V23 = ParseFloat(values[23]),
                        V24 = ParseFloat(values[24]),
                        V25 = ParseFloat(values[25]),
                        V26 = ParseFloat(values[26]),
                        V27 = ParseFloat(values[27]),
                        V28 = ParseFloat(values[28]),
                        Amount = ParseFloat(values[29]),
                        Label = values[30] == "1"
                    };

                    data.Add(record);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Satır {LineNumber} işlenirken hata oluştu", lineNumber);
                    continue;
                }
            }

            _logger.LogInformation("Toplam {Count} kayıt başarıyla yüklendi", data.Count);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Veri yükleme sırasında hata oluştu");
            throw new Exception("Test verisi yüklenirken hata oluştu", ex);
        }
    }


    private bool ValidateHeader(string headerLine)
    {
        if (string.IsNullOrEmpty(headerLine)) return false;

        var headers = headerLine.Replace("\"", "").Split(',');
        var expectedHeaders = new[] { "Time" }
            .Concat(Enumerable.Range(1, 28).Select(i => $"V{i}"))
            .Concat(new[] { "Amount", "Class" });

        return headers.SequenceEqual(expectedHeaders);
    }

    private float ParseFloat(string value)
    {
        if (float.TryParse(value,
                NumberStyles.Float | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var result))
            return result;

        throw new FormatException($"Değer float'a çevrilemedi: {value}");
    }
}