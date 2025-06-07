using DeepLCmdPal.Enum;

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeepLCmdPal.DTO
{
    public class TranslationResultDTO
    {
        [JsonIgnore]
        public string TargetLangCode { get; set; }

        [JsonPropertyName("translations")]
        public List<TranslationDTO> Translations { get; set; }
    }
}
