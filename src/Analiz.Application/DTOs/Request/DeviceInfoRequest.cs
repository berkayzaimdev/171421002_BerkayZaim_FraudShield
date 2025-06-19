namespace Analiz.Application.DTOs.Request;

public class DeviceInfoRequest
{
    public string DeviceId { get; set; }
    public string DeviceType { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public Dictionary<string, string> AdditionalInfo { get; set; } = new();
}