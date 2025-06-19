using System.Text.Json;
using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.ValueObjects;

using System.Text.Json.Serialization;
using FraudShield.TransactionAnalysis.Domain.Common;

[JsonConverter(typeof(LocationJsonConverter))]
public class Location : ValueObject
{
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string Country { get; set; }
    public string City { get; set; }
    public bool IsHighRiskRegion { get; set; }

    public Location()
    {
    }

    public static Location Create(
        double latitude,
        double longitude,
        string country,
        string city,
        bool isHighRiskRegion = false)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Invalid latitude value", nameof(latitude));

        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Invalid longitude value", nameof(longitude));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty", nameof(country));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        return new Location
        {
            Latitude = latitude,
            Longitude = longitude,
            Country = country,
            City = city,
            IsHighRiskRegion = isHighRiskRegion
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return Country;
        yield return City;
    }
}

public class LocationJsonConverter : JsonConverter<Location>
{
    public override Location Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected start of object");

        double? latitude = null;
        double? longitude = null;
        string? country = null;
        string? city = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (!latitude.HasValue || !longitude.HasValue || country == null || city == null)
                    throw new JsonException("Missing required location properties");

                return Location.Create(
                    latitude.Value,
                    longitude.Value,
                    country,
                    city
                );
            }

            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected property name");

            var propertyName = reader.GetString()?.ToLower();
            reader.Read();

            switch (propertyName)
            {
                case "latitude":
                    latitude = reader.GetDouble();
                    break;
                case "longitude":
                    longitude = reader.GetDouble();
                    break;
                case "country":
                    country = reader.GetString();
                    break;
                case "city":
                    city = reader.GetString();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Expected end of object");
    }

    public override void Write(Utf8JsonWriter writer, Location value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("latitude", value.Latitude);
        writer.WriteNumber("longitude", value.Longitude);
        writer.WriteString("country", value.Country);
        writer.WriteString("city", value.City);
        writer.WriteEndObject();
    }
}