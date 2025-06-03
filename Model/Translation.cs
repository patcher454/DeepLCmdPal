using System.Text.Json.Serialization;

namespace DeepLCmdPal.Model
{
    public class Translation
    {
        [JsonPropertyName("detected_source_language")]
        public string DetectedSourceLanguage { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
