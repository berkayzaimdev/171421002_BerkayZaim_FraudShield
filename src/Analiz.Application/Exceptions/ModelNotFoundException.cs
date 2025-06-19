namespace Analiz.Application.Exceptions;

public class ModelNotFoundException : Exception
{
    public string ModelName { get; }
    public string Version { get; }

    public ModelNotFoundException(string modelName, string version = null)
        : base($"Model {modelName} {(version != null ? $"version {version} " : "")}not found")
    {
        ModelName = modelName;
        Version = version;
    }
}