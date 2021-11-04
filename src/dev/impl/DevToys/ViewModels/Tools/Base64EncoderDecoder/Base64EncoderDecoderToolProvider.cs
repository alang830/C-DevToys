﻿#nullable enable

using DevToys.Api.Core.Injection;
using DevToys.Api.Tools;
using System;
using System.Composition;
using System.Text;
using System.Text.RegularExpressions;

namespace DevToys.ViewModels.Tools.Base64EncoderDecoder
{
    [Export(typeof(IToolProvider))]
    [Name("Base64 Encoder/Decoder")]
    [ProtocolName("base64")]
    [Order(1)]
    internal sealed class Base64EncoderDecoderToolProvider : ToolProviderBase, IToolProvider
    {
        public string DisplayName => LanguageManager.Instance.Base64EncoderDecoder.DisplayName;

        public object IconSource => CreatePathIconFromPath(nameof(Base64EncoderDecoderToolProvider));

        private readonly IMefProvider _mefProvider;

        [ImportingConstructor]
        public Base64EncoderDecoderToolProvider(IMefProvider mefProvider)
        {
            _mefProvider = mefProvider;
        }

        public bool CanBeTreatedByTool(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return false;
            }

            string? trimmedData = data.Trim();
            bool isBase64 = IsBase64DataStrict(trimmedData);

            return isBase64;
        }

        public IToolViewModel CreateTool()
        {
            return _mefProvider.Import<Base64EncoderDecoderToolViewModel>();
        }

        private bool IsBase64DataStrict(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return false;
            }

            if (data.Length % 4 != 0)
            {
                return false;
            }

            if (new Regex(@"[^A-Z0-9+/=]", RegexOptions.IgnoreCase).IsMatch(data))
            {
                return false;
            }

            var equalIndex = data.IndexOf('=');
            var length = data.Length;

            if (!(equalIndex == -1 || equalIndex == length - 1 || (equalIndex == length - 2 && data[length - 1] == '=')))
            {
                return false;
            }

            string? decoded;

            try
            {
                byte[] decodedData = Convert.FromBase64String(data);
                decoded = Encoding.UTF8.GetString(decodedData);
            }
            catch (Exception)
            {
                return false;
            }

            //check for special chars that you know should not be there
            char current;
            for (int i = 0; i < decoded.Length; i++)
            {
                current = decoded[i];
                if (current == 65533)
                {
                    return false;
                }

                if (!(current == 0x9
                    || current == 0xA
                    || current == 0xD
                    || (current >= 0x20 && current <= 0xD7FF)
                    || (current >= 0xE000 && current <= 0xFFFD)
                    || (current >= 0x10000 && current <= 0x10FFFF)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
