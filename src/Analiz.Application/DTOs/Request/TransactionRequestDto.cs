using System.ComponentModel.DataAnnotations;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// İşlem isteği DTO
/// </summary>
public class TransactionRequestDto
{
    [Required] public Guid UserId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required] public string MerchantId { get; set; }

    [Required] public string Type { get; set; } // TransactionType enum değeri

    [Required] public LocationDto Location { get; set; }

    [Required] public DeviceInfoDto DeviceInfo { get; set; }

    public AdditionalDataDto AdditionalData { get; set; } = new();
}