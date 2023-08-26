﻿using System.Text.Json;
using DevToys.Tools.Helpers;
using DevToys.Tools.Models;
using Microsoft.Extensions.Logging;

namespace DevToys.Tools.Tools.Converters.JsonYaml;

[Export(typeof(IGuiTool))]
[Name("JsonYamlConverter")]
[ToolDisplayInformation(
    IconFontName = "FluentSystemIcons",
    IconGlyph = '\uE2A2',
    GroupName = "Converters",
    ResourceManagerAssemblyIdentifier = nameof(DevToysToolsResourceManagerAssemblyIdentifier),
    ResourceManagerBaseName = "DevToys.Tools.Tools.Converters.JsonYaml.JsonYamlConverter",
    ShortDisplayTitleResourceName = nameof(JsonYamlConverter.ShortDisplayTitle),
    LongDisplayTitleResourceName = nameof(JsonYamlConverter.ShortDisplayTitle),
    DescriptionResourceName = nameof(JsonYamlConverter.Description),
    AccessibleNameResourceName = nameof(JsonYamlConverter.AccessibleName))]
[AcceptedDataTypeName(PredefinedCommonDataTypeNames.Json)]
[AcceptedDataTypeName(PredefinedCommonDataTypeNames.Yaml)]
internal sealed partial class JsonYamlConverterGuiTool : IGuiTool, IDisposable
{
    private static string JsonLanguage => "json";
    private static string YamlLanguage => "yaml";

    /// <summary>
    /// Whether the tool should convert Json to Yaml or Yaml to Json.
    /// </summary>
    private static readonly SettingDefinition<Conversion> conversionMode
        = new(name: $"{nameof(JsonYamlConverterGuiTool)}.{nameof(conversionMode)}", defaultValue: Conversion.JsonToYaml);

    /// <summary>
    /// Which indentation the tool need to use.
    /// </summary>
    private static readonly SettingDefinition<Indentation> indentationMode
        = new(name: $"{nameof(JsonYamlConverterGuiTool)}.{nameof(indentationMode)}", defaultValue: Indentation.TwoSpaces);

    private enum GridColumn
    {
        Content
    }

    private enum GridRow
    {
        Header,
        Content,
        Footer
    }

    private readonly ILogger _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IUIMultiLineTextInput _inputTextArea = MultilineTextInput("json-to-yaml-input-textarea");
    private readonly IUIMultiLineTextInput _outputTextArea = MultilineTextInput("json-to-yaml-output-textarea");

    private CancellationTokenSource? _cancellationTokenSource;

    [ImportingConstructor]
    public JsonYamlConverterGuiTool(ISettingsProvider settingsProvider)
    {
        _logger = this.Log();
        _settingsProvider = settingsProvider;

        switch (_settingsProvider.GetSetting(conversionMode))
        {
            case Conversion.JsonToYaml:
                SetJsonToYamlConversion();
                break;
            case Conversion.YamlToJson:
                SetYamlToJsonConversion();
                break;
            default:
                throw new NotSupportedException();
        }
    }

    internal Task? WorkTask { get; private set; }

    public UIToolView View
        => new(
            isScrollable: true,
            Grid()
                .ColumnLargeSpacing()
                .RowLargeSpacing()
                .Rows(
                    (GridRow.Header, Auto),
                    (GridRow.Content, new UIGridLength(1, UIGridUnitType.Fraction))
                )
                .Columns(
                    (GridColumn.Content, new UIGridLength(1, UIGridUnitType.Fraction))
                )
            .Cells(
                Cell(
                    GridRow.Header,
                    GridColumn.Content,
                    Stack().Vertical().WithChildren(
                        Setting()
                        .Icon("FluentSystemIcons", '\uF18D')
                        .Title(JsonYamlConverter.ConversionTitle)
                        .Description(JsonYamlConverter.ConversionDescription)
                        .Handle(
                            _settingsProvider,
                            conversionMode,
                            OnConversionModeChanged,
                            Item(JsonYamlConverter.JsonToYaml, Conversion.JsonToYaml),
                            Item(JsonYamlConverter.YamlToJson, Conversion.YamlToJson)
                        ),
                        Setting()
                        .Icon("FluentSystemIcons", '\uF6F8')
                        .Title(JsonYamlConverter.Indentation)
                        .Handle(
                            _settingsProvider,
                            indentationMode,
                            OnIndentationModelChanged,
                            Item(JsonYamlConverter.TwoSpaces, Indentation.TwoSpaces),
                            Item(JsonYamlConverter.FourSpaces, Indentation.FourSpaces)
                        )
                    )
                ),
                Cell(
                    GridRow.Content,
                    GridColumn.Content,
                    SplitGrid()
                        .Vertical()
                        .WithLeftPaneChild(
                            _inputTextArea
                                .Title(JsonYamlConverter.Input)
                                .OnTextChanged(OnInputTextChanged))
                        .WithRightPaneChild(
                            _outputTextArea
                                .Title(JsonYamlConverter.Output)
                                .ReadOnly()
                                .Extendable())
                )
            )
        );

    // Smart detection handler.
    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        if (dataTypeName == PredefinedCommonDataTypeNames.Json &&
            parsedData is string jsonStrongTypedParsedData)
        {
            _inputTextArea.Language(JsonLanguage);
            _outputTextArea.Language(YamlLanguage);
            _settingsProvider.SetSetting(conversionMode, Conversion.JsonToYaml);
            _inputTextArea.Text(jsonStrongTypedParsedData);
        }

        if (dataTypeName == PredefinedCommonDataTypeNames.Yaml &&
            parsedData is string yamlStrongTypedParsedData)
        {
            _inputTextArea.Language(YamlLanguage);
            _outputTextArea.Language(JsonLanguage);
            _settingsProvider.SetSetting(conversionMode, Conversion.YamlToJson);
            _inputTextArea.Text(yamlStrongTypedParsedData);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }

    private void OnConversionModeChanged(Conversion conversionMode)
    {
        switch (conversionMode)
        {
            case Conversion.JsonToYaml:
                SetJsonToYamlConversion();
                break;
            case Conversion.YamlToJson:
                SetYamlToJsonConversion();
                break;
            default:
                throw new NotSupportedException();
        }
        _settingsProvider.SetSetting(JsonYamlConverterGuiTool.conversionMode, conversionMode);
        _inputTextArea.Text(_outputTextArea.Text);
    }

    private void OnIndentationModelChanged(Indentation indentationMode)
    {
        _settingsProvider.SetSetting(JsonYamlConverterGuiTool.indentationMode, indentationMode);
        _inputTextArea.Text(_outputTextArea.Text);
    }

    private void OnInputTextChanged(string text)
    {
        StartConvert(text);
    }

    private void StartConvert(string text)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        WorkTask = ConvertAsync(text, _cancellationTokenSource.Token);
    }

    private async Task ConvertAsync(string input, CancellationToken cancellationToken)
    {
        await TaskSchedulerAwaiter.SwitchOffMainThreadAsync(cancellationToken);

        string? conversionResult;
        Indentation indentation = _settingsProvider.GetSetting(indentationMode);
        if (_settingsProvider.GetSetting(conversionMode) is Conversion.JsonToYaml)
        {
            conversionResult = YamlHelper.ConvertFromJson(input, indentation, _logger, cancellationToken);
            if (string.IsNullOrEmpty(conversionResult))
            {
                cancellationToken.ThrowIfCancellationRequested();
                _outputTextArea.Text(JsonYamlConverter.InvalidJson);
                return;
            }
        }
        else
        {
            conversionResult = JsonHelper.ConvertFromYaml(input, indentation, _logger, cancellationToken);
            if (string.IsNullOrEmpty(conversionResult))
            {
                cancellationToken.ThrowIfCancellationRequested();
                _outputTextArea.Text(JsonYamlConverter.InvalidYaml);
                return;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        _outputTextArea.Text(conversionResult);
    }

    private void SetJsonToYamlConversion()
    {
        _inputTextArea
            .Language(JsonLanguage);
        _outputTextArea
            .Language(YamlLanguage);
    }

    private void SetYamlToJsonConversion()
    {
        _inputTextArea
            .Language(YamlLanguage);
        _outputTextArea
            .Language(JsonLanguage);
    }
}
