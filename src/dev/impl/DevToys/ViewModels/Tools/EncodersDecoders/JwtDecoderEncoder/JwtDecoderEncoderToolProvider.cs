﻿#nullable enable

using System.Composition;
using DevToys.Shared.Api.Core;
using DevToys.Api.Tools;
using DevToys.Helpers;
using DevToys.Core.Threading;
using Windows.UI.Xaml.Controls;

namespace DevToys.ViewModels.Tools.JwtDecoderEncoder
{
    [Export(typeof(IToolProvider))]
    [Name("Jwt Decoder / Encoder")]
    [Parent(EncodersDecodersGroupToolProvider.InternalName)]
    [ProtocolName("jwt")]
    [Order(3)]
    [NotScrollable]
    internal sealed class JwtDecoderEncoderToolProvider : IToolProvider
    {
        private readonly IMefProvider _mefProvider;

        public string MenuDisplayName => LanguageManager.Instance.JwtDecoderEncoder.DisplayName;

        public string? SearchDisplayName => LanguageManager.Instance.JwtDecoderEncoder.DisplayName;

        public string? Description => LanguageManager.Instance.JwtDecoderEncoder.Description;

        public string AccessibleName => LanguageManager.Instance.JwtDecoderEncoder.AccessibleName;

        public string? SearchKeywords => LanguageManager.Instance.JwtDecoderEncoder.SearchKeywords;

        public string IconGlyph => "\u0110";

        [ImportingConstructor]
        public JwtDecoderEncoderToolProvider(IMefProvider mefProvider)
        {
            _mefProvider = mefProvider;
        }

        public bool CanBeTreatedByTool(string data)
        {
            return JwtHelper.IsValid(data?.Trim());
        }

        public IToolViewModel CreateTool()
        {
            return _mefProvider.Import<JwtDecoderEncoderToolViewModel>();
        }
    }
}
