using System.ComponentModel.DataAnnotations;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Cihaz bilgileri DTO
/// </summary>
public class DeviceInfoDto
{
    [Required] public string DeviceId { get; set; }

    public string DeviceType { get; set; }

    [Required] public string IpAddress { get; set; }

    public string UserAgent { get; set; }

    public Dictionary<string, string> AdditionalInfo { get; set; } = new();
}