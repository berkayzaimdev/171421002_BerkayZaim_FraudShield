namespace Analiz.Application.Feature;

public class FeatureSet
{
    private readonly Dictionary<string, double> _features;
    private readonly Dictionary<string, string> _metadata;

    private FeatureSet(Dictionary<string, double> features, Dictionary<string, string> metadata)
    {
        _features = features;
        _metadata = metadata;
    }

    public static FeatureSet Create(Dictionary<string, double> features, Dictionary<string, string> metadata)
    {
        return new FeatureSet(features, metadata);
    }

    public float[] ToVector()
    {
        return _features.Values.Select(v => (float)v).ToArray();
    }
}