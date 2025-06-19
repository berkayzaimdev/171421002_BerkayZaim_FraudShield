namespace Analiz.Domain.Entities.Rule.Context;

/// <summary>
/// GPS lokasyonu
/// </summary>
public class GpsLocation
{
    /// <summary>
    /// Enlem
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Boylam
    /// </summary>
    public double Longitude { get; set; }

    public string Country { get; set; }
    public string City { get; set; }


    /// <summary>
    /// Doğruluk (metre)
    /// </summary>
    public double Accuracy { get; set; }
}