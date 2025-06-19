using System.ComponentModel.DataAnnotations;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Konum bilgileri DTO
/// </summary>
public class LocationDto
{
    [Required]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double Longitude { get; set; }

    [Required]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be a 2-letter ISO code")]
    public string Country { get; set; }

    [Required] public string City { get; set; }
}