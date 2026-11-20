#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WHATSAPP_CHAPT.App.Services
{
    public sealed class MessageLongTextPolicy
    {
        public int Threshold { get; }
        public bool SplitEnabled { get; }

        public MessageLongTextPolicy(int threshold = 4096, bool splitEnabled = true)
        {
            if (threshold < 500 || threshold > 20000)
                throw new ArgumentOutOfRangeException(nameof(threshold));
            Threshold = threshold;
            SplitEnabled = splitEnabled;
        }

        public IReadOnlyList<string> Split(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (text.Length <= Threshold)
                return new[] { text };

            var chunks = new List<string>();
            var paragraphMatches = Regex.Split(text, @"\r?\n\s*\r?\n");
            foreach (string paragraph in paragraphMatches)
                AddChunk(chunks, paragraph);
            return chunks;
        }

        private void AddChunk(List<string> chunks, string text)
        {
            if (text.Length == 0)
                return;

            if (text.Length <= Threshold)
            {
                chunks.Add(text);
                return;
            }

            var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            var current = new StringBuilder();
            foreach (string sentence in sentences)
            {
                if (current.Length + sentence.Length + 1 <= Threshold)
                {
                    if (current.Length > 0)
                        current.Append(' ');
                    current.Append(sentence);
                }
                else
                {
                    if (current.Length > 0)
                    {
                        chunks.Add(current.ToString());
                        current.Clear();
                    }
                    AddByWords(chunks, current, sentence);
                }
            }
            if (current.Length > 0)
                chunks.Add(current.ToString());
        }

        private void AddByWords(List<string> chunks, StringBuilder current, string text)
        {
            var words = text.Split(' ');
            foreach (string word in words)
            {
                if (current.Length + word.Length + 1 <= Threshold)
                {
                    if (current.Length > 0)
                        current.Append(' ');
                    current.Append(word);
                }
                else
                {
                    if (current.Length > 0)
                    {
                        chunks.Add(current.ToString());
                        current.Clear();
                    }
                    if (word.Length <= Threshold)
                    {
                        current.Append(word);
                    }
                    else
                    {
                        for (int i = 0; i < word.Length; i += Threshold)
                        {
                            int length = Math.Min(Threshold, word.Length - i);
                            if (i + length < word.Length
                                && char.IsHighSurrogate(word[i + length - 1])
                                && char.IsLowSurrogate(word[i + length]))
                                length--;
                            if (length > 0)
                                chunks.Add(word.Substring(i, length));
                        }
                    }
                }
            }
        }
    }
}
