using Analiz.Domain.Entities.ML.DataSet;

namespace Analiz.Application.Interfaces.Infrastructure;

public interface ITestDataService
{
    Task<List<CreditCardModelData>> LoadCreditCardDataAsync();
}