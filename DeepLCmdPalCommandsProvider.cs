// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using DeepLCmdPal.Util;

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DeepLCmdPal;

public partial class DeepLCmdPalCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly SettingsManager _settingsManager = new();


    public DeepLCmdPalCommandsProvider()
    {
        DisplayName = "DeepLCmdPal";
        Icon = IconHelpers.FromRelativePath("Assets\\Logo.svg");
        Settings = _settingsManager.Settings;

        _commands = [
            new CommandItem(new DeepLCmdPalPage(_settingsManager)){
                Title = DisplayName,
                MoreCommands = 
                [
                    new CommandContextItem(Settings.SettingsPage)
                ]
            }
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
