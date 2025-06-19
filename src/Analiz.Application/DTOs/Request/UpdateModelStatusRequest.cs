using System.ComponentModel.DataAnnotations;

namespace Analiz.Application.DTOs.Request;

public class UpdateModelStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
} 