﻿using System.Text.Json.Serialization;

namespace DeepLCmdPal.DTO
{
    public class TranslationDTO
    {
        [JsonPropertyName("detected_source_language")]
        public string DetectedSourceLanguage { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
