#nullable enable

using System;
using System.IO;

namespace WHATSAPP_CHAPT.App.Services
{
    public sealed class MediaPolicy
    {
        public long MaximumSizeBytes { get; }

        public MediaPolicy(long maximumSizeBytes = 100 * 1024 * 1024)
        {
            MaximumSizeBytes = maximumSizeBytes;
        }

        public static string CleanFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "fichier";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalid, '_');
            foreach (char invalid in Path.GetInvalidPathChars())
                fileName = fileName.Replace(invalid, '_');
            fileName = fileName.Trim();
            return fileName.Length == 0 ? "fichier" : fileName;
        }

        public static bool RequiresExecutableWarning(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension is ".exe" or ".bat" or ".cmd" or ".ps1" or ".vbs"
                or ".js" or ".lnk" or ".msi" or ".scr" or ".com";
        }

        public static string Classify(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp")
                return "image";
            if (extension is ".mp4" or ".avi" or ".mov" or ".mkv" or ".webm")
                return "video";
            if (extension is ".wav" or ".mp3" or ".m4a" or ".ogg" or ".flac")
                return "audio";
            if (extension is ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx"
                or ".ppt" or ".pptx" or ".txt" or ".zip")
                return "document";
            return "document";
        }
    }
}
