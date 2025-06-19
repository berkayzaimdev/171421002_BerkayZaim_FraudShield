using Analiz.Application.DTOs.Request;
using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces.Test;

/// <summary>
/// Test amaçlı işlem oluşturmak için kullanılan servis arayüzü
/// </summary>
public interface ITransactionTestService
{
    /// <summary>
    /// Belirtilen sayıda test işlemi oluşturur
    /// </summary>
    /// <param name="count">Toplam işlem sayısı</param>
    /// <param name="fraudPercentage">Dolandırıcılık işlemlerinin yüzdesi (0-100 arası)</param>
    /// <returns>Oluşturulan işlemler listesi</returns>
    List<TransactionRequest> GenerateTransactions(int count, double fraudPercentage = 0.2);

    /// <summary>
    /// Kredi kartı veri setinden gerçek değerlerle gerçekçi işlemler oluşturur
    /// </summary>
    /// <param name="count">Oluşturulacak işlem sayısı</param>
    /// <returns>Oluşturulan işlemler</returns>
    Task<List<TransactionRequest>> GenerateTransactionsFromDatasetAsync(int count);
}