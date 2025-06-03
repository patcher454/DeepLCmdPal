// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using DeepLCmdPal.Enum;
using DeepLCmdPal.Job;
using DeepLCmdPal.Model;
using DeepLCmdPal.Util;

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepLCmdPal;

internal sealed partial class DeepLCmdPalPage : DynamicListPage, IDisposable
{
    private List<ListItem> _allItems;
    private readonly SettingsManager _settingsManager;
    private static Task<TranslationResult> translationTask = null;

    public DeepLCmdPalPage(SettingsManager settingsManager)
    {
        Icon = IconHelpers.FromRelativePath("Assets\\Logo.svg");
        Title = "DeepLCmdPal";
        Name = "Open";
        _settingsManager = settingsManager;
        _allItems = _settingsManager.LoadHistory();
    }

    public override async void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (newSearch == oldSearch || string.IsNullOrWhiteSpace(newSearch))
        {
            return;
        }

        var (targetCode, text) = InputInterpreter.Parse(newSearch, LangCode.Parse(_settingsManager.DefaultTargetLang));

        if (translationTask == null || translationTask.IsCompleted)
        {
            translationTask = JobHttp.Translation(targetCode, text, _settingsManager.DeepLAPIKey);
        }

        var result = translationTask.GetAwaiter().GetResult();
       
        _allItems = [];
        foreach (var item in result.Translations)
        {
            var translation = new TranslationHistory
            {
                OriginalText = result.OriginalText,
                OriginalLangCode = item.DetectedSourceLanguage,
                TranslatedText = item.Text,
                TargetLangCode = result.TargetLangCode,
            };

            _allItems.Add(new ListItem(new ResultCopyCommand(translation, _settingsManager))
            {
                Icon = IconHelpers.FromRelativePath("Assets\\Logo.svg"),
                Title = translation.TranslatedText,
                Details = new Details()
                {
                    Body = translation.OriginalText
                },
                Tags = [new Tag($"{translation.OriginalLangCode} -> {translation.TargetLangCode}")],
            });
        }

        RaiseItemsChanged(_allItems.Count);
    }

    public override IListItem[] GetItems()
    {
        return [.. _allItems];
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
