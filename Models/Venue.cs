using System.Text.Json.Serialization;

namespace MauiImageClassifierApp.Models
{
    public class Venue
    {
        [JsonPropertyName("venue_name")]
        public string Name { get; set; }

        [JsonPropertyName("full_address")]
        public string FullAddress { get; set; }

        [JsonPropertyName("business_type")]
        public string BusinessType { get; set; } // Added for dropdown
    }
}
