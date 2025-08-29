using System.Text.Json.Serialization;

namespace MauiImageClassifierApp.Models
{
    public class SearchRequest
    {

        [JsonPropertyName("category")]
        public string category { get; set; }

        [JsonPropertyName("latitude")]
        public double latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double longitude { get; set; }

        [JsonPropertyName("radius_miles")]
        public double? radius_miles { get; set; } // Nullable

        [JsonPropertyName("return_count")]
        public int return_count { get; set; }
    }
}
