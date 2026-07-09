using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Interview.Common;

public class FeeSchedule
{
    [JsonPropertyName("interchange")]
    public Dictionary<string, FeeRate> Interchange { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("processor_markup")]
    public FeeRate ProcessorMarkup { get; set; } = new();

    public FeeSchedule(string jsonData)
    {
        if (string.IsNullOrWhiteSpace(jsonData))
        {
            throw new ArgumentException("JSON data is required.", nameof(jsonData));
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var parsed = JsonSerializer.Deserialize<FeeScheduleData>(jsonData, options)
            ?? throw new InvalidOperationException("Failed to deserialize fee schedule JSON.");

        Interchange = parsed.Interchange ?? new Dictionary<string, FeeRate>(StringComparer.OrdinalIgnoreCase);
        if (Interchange.Comparer != StringComparer.OrdinalIgnoreCase)
        {
            Interchange = new Dictionary<string, FeeRate>(Interchange, StringComparer.OrdinalIgnoreCase);
        }

        ProcessorMarkup = parsed.ProcessorMarkup ?? new FeeRate();
    }

	public (int FlatCents, decimal Flat, decimal Percent) GetFlatAndPercentSafe( string name )
    {
        try
        {
            return GetFlatAndPercent( name );
		}
        catch
        {
            /*Intentional*/
        }
        return (0, 0, 0);
    }


	public (int FlatCents, decimal Flat, decimal Percent) GetFlatAndPercent(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (name.Equals("processor_markup", StringComparison.OrdinalIgnoreCase))
        {
            return ((int)(ProcessorMarkup.Flat * 100.0M), ProcessorMarkup.Flat, ProcessorMarkup.Percent);
        }

        if (Interchange.TryGetValue(name, out var feeRate) && feeRate is not null)
        {
            return ((int)(feeRate.Flat * 100.0M), feeRate.Flat, feeRate.Percent);
        }

        throw new KeyNotFoundException($"No fee schedule entry found for '{name}'.");
    }

    private class FeeScheduleData
    {
        [JsonPropertyName("interchange")]
        public Dictionary<string, FeeRate>? Interchange { get; set; }

        [JsonPropertyName("processor_markup")]
        public FeeRate? ProcessorMarkup { get; set; }
    }
}

public class FeeRate
{
    [JsonPropertyName("percent")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Percent { get; set; }

    [JsonPropertyName("flat")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Flat { get; set; }

    [JsonIgnore]
    public int FlatCents => (int)( Flat * 100.0M );
    
}
