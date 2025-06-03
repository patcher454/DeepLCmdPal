using DeepLCmdPal.Enum;
using DeepLCmdPal.Model;

using Microsoft.CommandPalette.Extensions.Toolkit;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DeepLCmdPal.Util
{
    public class SettingsManager : JsonSettingsManager
    {
        private readonly string _historyPath;

        private static readonly string _namespace = "deepl-cmdpal";

        private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

        private static readonly List<ChoiceSetSetting.Choice> _historyChoices =
        [
            new ChoiceSetSetting.Choice(Properties.Resource.history_none, Properties.Resource.history_none),
            new ChoiceSetSetting.Choice(Properties.Resource.history_1, Properties.Resource.history_1),
            new ChoiceSetSetting.Choice(Properties.Resource.history_5, Properties.Resource.history_5),
            new ChoiceSetSetting.Choice(Properties.Resource.history_10, Properties.Resource.history_10),
            new ChoiceSetSetting.Choice(Properties.Resource.history_20, Properties.Resource.history_20),
        ];

        private static readonly List<ChoiceSetSetting.Choice> _targetLangChoices =
            System.Enum.GetValues(typeof(LangCode.Code))
            .Cast<LangCode.Code>()
            .Select(lang => new ChoiceSetSetting.Choice(
                lang.ToString(),
                ((int)lang).ToString())
            ).ToList();

        private readonly ChoiceSetSetting _showHistory = new(
            Namespaced(nameof(ShowHistory)),
            Properties.Resource.plugin_show_history,
            Properties.Resource.plugin_show_history,
            _historyChoices);

        private readonly ChoiceSetSetting _targetLang = new(
            Namespaced(nameof(DefaultTargetLang)),
            Properties.Resource.plugin_default_target_language_code_title,
            Properties.Resource.plugin_default_target_language_code_description,
            _targetLangChoices);

        private readonly TextSetting _apiKey = new(
            Namespaced(nameof(DeepLAPIKey)),
            Properties.Resource.plugin_deepL_api_key,
            Properties.Resource.plugin_deepL_api_key,
            "DeepL-Auth-Key {API KEY}");

        public string ShowHistory => _showHistory.Value ?? string.Empty;

        public string DefaultTargetLang => _targetLang.Value ?? string.Empty;

        public string DeepLAPIKey => _apiKey.Value ?? string.Empty;


        internal static string SettingsJsonPath()
        {
            var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");
            Directory.CreateDirectory(directory);

            // now, the state is just next to the exe
            return Path.Combine(directory, "settings.json");
        }

        internal static string HistoryStateJsonPath()
        {
            var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");
            Directory.CreateDirectory(directory);

            // now, the state is just next to the exe
            return Path.Combine(directory, "deepl_cmdpal_history.json");
        }

        public void SaveHistory(TranslationHistory historyItem)
        {
            if (historyItem == null)
            {
                return;
            }

            try
            {
                List<TranslationHistory> historyItems;

                // Check if the file exists and load existing history
                if (File.Exists(_historyPath))
                {
                    var existingContent = File.ReadAllText(_historyPath);
                    historyItems = JsonSerializer.Deserialize<List<TranslationHistory>>(existingContent) ?? [];
                }
                else
                {
                    historyItems = [];
                }

                // Add the new history item
                historyItems.Add(historyItem);

                historyItems = historyItems.DistinctBy(x => x.TranslatedText).ToList();

                // Determine the maximum number of items to keep based on ShowHistory
                if (int.TryParse(ShowHistory, out var maxHistoryItems) && maxHistoryItems > 0)
                {
                    // Keep only the most recent `maxHistoryItems` items
                    while (historyItems.Count > maxHistoryItems)
                    {
                        historyItems.RemoveAt(0); // Remove the oldest item
                    }
                }

                var historyJson = JsonSerializer.Serialize(historyItems);
                File.WriteAllText(_historyPath, historyJson);
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = ex.ToString() });
            }
        }

        public List<ListItem> LoadHistory()
        {
            try
            {
                if (!File.Exists(_historyPath))
                {
                    return [];
                }

                // Read and deserialize JSON into a list of HistoryItem objects
                var fileContent = File.ReadAllText(_historyPath);
                var historyItems = JsonSerializer.Deserialize<List<TranslationHistory>>(fileContent) ?? [];

                // Convert each HistoryItem to a ListItem
                var listItems = new List<ListItem>();
                foreach (var historyItem in historyItems)
                {
                    try
                    {
                        // Check if historyItem is null
                        if (historyItem == null)
                        {
                            ExtensionHost.LogMessage(new LogMessage() { Message = "Null history item found, skipping." });
                            continue;
                        }

                        // Check if required fields are null
                        if (historyItem.OriginalText == null ||
                            historyItem.TranslatedText == null ||
                            historyItem.OriginalLangCode == null ||
                            historyItem.TargetLangCode == null)
                        {
                            ExtensionHost.LogMessage(new LogMessage() { Message = "History item contains null fields, skipping." });
                            continue;
                        }

                        listItems.Add(new ListItem(new ResultCopyCommand(historyItem, this))
                        {
                            Icon = IconHelpers.FromRelativePath("Assets\\Logo.svg"),
                            Title = historyItem.TranslatedText,
                            Details = new Details()
                            {
                                Body = historyItem.OriginalText
                            },
                            Tags = [new Tag($"{historyItem.OriginalLangCode} -> {historyItem.TargetLangCode}")],
                        });
                    }
                    catch (Exception ex)
                    {
                        ExtensionHost.LogMessage(new LogMessage() { Message = $"Error processing history item: {ex}" });
                    }
                }

                listItems.Reverse();
                return listItems;
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = ex.ToString() });
                return [];
            }
        }

        public SettingsManager()
        {
            FilePath = SettingsJsonPath();
            _historyPath = HistoryStateJsonPath();

            Settings.Add(_showHistory);
            Settings.Add(_targetLang);
            Settings.Add(_apiKey);

            // Load settings from file upon initialization
            LoadSettings();

            Settings.SettingsChanged += (s, a) => SaveSettings();
        }

        private void ClearHistory()
        {
            try
            {
                if (File.Exists(_historyPath))
                {
                    // Delete the history file
                    File.Delete(_historyPath);

                    // Log that the history was successfully cleared
                    ExtensionHost.LogMessage(new LogMessage() { Message = "History cleared successfully." });
                }
                else
                {
                    // Log that there was no history file to delete
                    ExtensionHost.LogMessage(new LogMessage() { Message = "No history file found to clear." });
                }
            }
            catch (Exception ex)
            {
                // Log any exception that occurs
                ExtensionHost.LogMessage(new LogMessage() { Message = $"Failed to clear history: {ex}" });
            }
        }

        public override void SaveSettings()
        {
            base.SaveSettings();
            try
            {
                if (ShowHistory == Properties.Resource.history_none)
                {
                    ClearHistory();
                }
                else if (int.TryParse(ShowHistory, out var maxHistoryItems) && maxHistoryItems > 0)
                {
                    // Trim the history file if there are more items than the new limit
                    if (File.Exists(_historyPath))
                    {
                        var existingContent = File.ReadAllText(_historyPath);
                        var historyItems = JsonSerializer.Deserialize<List<TranslationHistory>>(existingContent) ?? [];

                        historyItems = historyItems.DistinctBy(x => x.TranslatedText).ToList();

                        // Check if trimming is needed
                        if (historyItems.Count > maxHistoryItems)
                        {
                            // Trim the list to keep only the most recent `maxHistoryItems` items
                            historyItems = historyItems.Skip(historyItems.Count - maxHistoryItems).ToList();

                            // Save the trimmed history back to the file
                            var trimmedHistoryJson = JsonSerializer.Serialize(historyItems);
                            File.WriteAllText(_historyPath, trimmedHistoryJson);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = ex.ToString() });
            }
        }
    }
}
