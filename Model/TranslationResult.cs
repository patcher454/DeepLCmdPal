using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeepLCmdPal.Model
{
    public class TranslationResult
    {
        [JsonIgnore]
        public string TargetLangCode { get; set; }

        [JsonIgnore]
        public string OriginalText { get; set; }

        [JsonPropertyName("translations")]
        public List<Translation> Translations { get; set; }
    }
}
