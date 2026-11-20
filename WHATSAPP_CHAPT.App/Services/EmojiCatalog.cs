#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace WHATSAPP_CHAPT.App.Services
{
    public sealed record EmojiEntry(string Name, string Unicode, string Path);

    public sealed class EmojiCatalog
    {
        private readonly List<EmojiEntry> _entries = new();

        public IReadOnlyList<EmojiEntry> Entries => _entries;

        public void Load(string directory)
        {
            _entries.Clear();
            if (!Directory.Exists(directory))
                return;

            foreach (string file in Directory.GetFiles(directory, "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (TryConvertSequence(name, out string? unicode))
                    _entries.Add(new EmojiEntry(name, unicode ?? string.Empty, file));
            }
        }

        public static bool TryConvertSequence(string name, out string? unicode)
        {
            unicode = null;
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var output = new System.Text.StringBuilder();
            foreach (string part in name.Split('-', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int codePoint))
                    return false;
                if (codePoint is < 0 or > 0x10FFFF)
                    return false;
                output.Append(char.ConvertFromUtf32(codePoint));
            }
            unicode = output.ToString();
            return unicode.Length > 0;
        }
    }
}
