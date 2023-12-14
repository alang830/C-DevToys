﻿using System.Text;
using DevToys.Tools.Helpers;
using DevToys.Tools.Models;

namespace DevToys.Tools.Tools.Generators.UUID;

[Export(typeof(IGuiTool))]
[Name("UUIDGenerator")]
[ToolDisplayInformation(
    IconFontName = "DevToys-Tools-Icons",
    IconGlyph = '\u0106',
    GroupName = PredefinedCommonToolGroupNames.Generators,
    ResourceManagerAssemblyIdentifier = nameof(DevToysToolsResourceManagerAssemblyIdentifier),
    ResourceManagerBaseName = "DevToys.Tools.Tools.Generators.UUID.UUIDGenerator",
    ShortDisplayTitleResourceName = nameof(UUIDGenerator.ShortDisplayTitle),
    LongDisplayTitleResourceName = nameof(UUIDGenerator.LongDisplayTitle),
    DescriptionResourceName = nameof(UUIDGenerator.Description),
    AccessibleNameResourceName = nameof(UUIDGenerator.AccessibleName))]
internal sealed class UUIDGeneratorGuidTool : IGuiTool
{
    /// <summary>
    /// Whether the generated UUID should have hyphens or not.
    /// </summary>
    private static readonly SettingDefinition<bool> hyphens
        = new(
            name: $"{nameof(UUIDGeneratorGuidTool)}.{nameof(hyphens)}",
            defaultValue: true);

    /// <summary>
    /// Whether the generated UUID should be uppercase or lowercase.
    /// </summary>
    private static readonly SettingDefinition<bool> uppercase
        = new(
            name: $"{nameof(UUIDGeneratorGuidTool)}.{nameof(uppercase)}",
            defaultValue: false);

    /// <summary>
    /// The UUID Version to generate.
    /// </summary>
    private static readonly SettingDefinition<UuidVersion> version
        = new(
            name: $"{nameof(UUIDGeneratorGuidTool)}.{nameof(version)}",
            defaultValue: UuidVersion.Four);

    /// <summary>
    /// How many GUIDs should be generated at once.
    /// </summary>
    private static readonly SettingDefinition<int> uuidToGenerate
        = new(
            name: $"{nameof(UUIDGeneratorGuidTool)}.{nameof(uuidToGenerate)}",
            defaultValue: 1);

    private enum GridColumn
    {
        Stretch
    }

    private enum GridRow
    {
        Settings,
        Results
    }

    private readonly ISettingsProvider _settingsProvider;
    private readonly IUIMultiLineTextInput _outputText = MultilineTextInput();

    [ImportingConstructor]
    public UUIDGeneratorGuidTool(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    public UIToolView View
        => new(
            isScrollable: true,
            Grid()
                .ColumnLargeSpacing()
                .RowLargeSpacing()
                .Rows(
                    (GridRow.Settings, Auto),
                    (GridRow.Results, new UIGridLength(1, UIGridUnitType.Fraction)))
                .Columns(
                    (GridColumn.Stretch, new UIGridLength(1, UIGridUnitType.Fraction)))

            .Cells(
                Cell(
                    GridRow.Settings,
                    GridColumn.Stretch,
                    Stack()
                        .Vertical()
                        .LargeSpacing()
                        .WithChildren(

                            Stack()
                                .Vertical()
                                .WithChildren(

                                    Label().Text(UUIDGenerator.ConfigurationTitle),

                                    Setting()
                                        .Icon("FluentSystemIcons", '\uF4E0')
                                        .Title(UUIDGenerator.Hyphens)
                                        .Handle(
                                            _settingsProvider,
                                            hyphens),

                                    Setting()
                                        .Icon("FluentSystemIcons", '\uF7B2')
                                        .Title(UUIDGenerator.Uppercase)
                                        .Handle(
                                            _settingsProvider,
                                            uppercase),

                                    Setting()
                                        .Icon("FluentSystemIcons", '\uF587')
                                        .Title(UUIDGenerator.VersionTitle)
                                        .Description(UUIDGenerator.VersionDescription)
                                        .Handle(
                                            _settingsProvider,
                                            version,
                                            onOptionSelected: null,
                                            Item(UUIDGenerator.UuidVersionOne, UuidVersion.One),
                                            Item(UUIDGenerator.UuidVersionFour, UuidVersion.Four))),

                            Stack()
                                .Vertical()
                                .WithChildren(

                                    Label().Text(UUIDGenerator.GenerateTitle),
                                    Stack()
                                        .Horizontal()
                                        .SmallSpacing()
                                        .WithChildren(

                                            Button()
                                                .AccentAppearance()
                                                .Text(UUIDGenerator.GenerateButton)
                                                .OnClick(OnGenerateButtonClick),

                                            Label().Style(UILabelStyle.BodyStrong).Text(UUIDGenerator.MultiplySymbol),

                                            NumberInput()
                                                .Minimum(1)
                                                .Maximum(10000)
                                                .Value(_settingsProvider.GetSetting(uuidToGenerate))
                                                .HideCommandBar()
                                                .OnTextChanged(OnNumberOfUuidsToGenerateChanged))))),

                Cell(
                    GridRow.Results,
                    GridColumn.Stretch,

                    _outputText
                        .Title(UUIDGenerator.Output)
                        .ReadOnly())));

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
    }

    private void OnNumberOfUuidsToGenerateChanged(string value)
    {
        if (int.TryParse(value, out int result))
        {
            _settingsProvider.SetSetting(uuidToGenerate, result);
        }
        else
        {
            _settingsProvider.SetSetting(uuidToGenerate, 1);
        }
    }

    private void OnGenerateButtonClick()
    {
        var newGuids = new StringBuilder();

        for (int i = 0; i < _settingsProvider.GetSetting(uuidToGenerate); i++)
        {
            string newUuid
                = UuidHelper.GenerateUuid(
                    _settingsProvider.GetSetting(version),
                    _settingsProvider.GetSetting(hyphens),
                    _settingsProvider.GetSetting(uppercase));

            newGuids.AppendLine(newUuid);
        }

        _outputText.Text(newGuids.ToString());
    }
}
