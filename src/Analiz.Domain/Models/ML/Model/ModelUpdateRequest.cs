namespace Analiz.Domain.Entities.ML;

public class ModelUpdateRequest
{
    public string Version { get; set; }
    public string Configuration { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}