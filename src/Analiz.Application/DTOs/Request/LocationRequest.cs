namespace Analiz.Application.DTOs.Request;

public class LocationRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Country { get; set; }
    public string City { get; set; }
}