#nullable enable

using System;
using System.Drawing;

namespace WHATSAPP_CHAPT.App.Drawing
{
    public enum ThemeMode
    {
        Dark,
        Light,
        System
    }

    /// <summary>
    /// Fournit les couleurs et constantes graphiques de l'application selon le thème sombre et bleu ciel.
    /// </summary>
    public static class Theme
    {
        public static event EventHandler? ThemeChanged;
        public static ThemeMode Mode { get; private set; } = ThemeMode.Dark;
        /// <summary>
        /// Fond principal de l'application (#0B0F14).
        /// </summary>
        public static Color AppBackground { get; private set; } = Color.FromArgb(0x0B, 0x0F, 0x14);

        /// <summary>
        /// Fond des panneaux et de la barre latérale (#111820).
        /// </summary>
        public static Color Panel { get; private set; } = Color.FromArgb(0x11, 0x18, 0x20);

        /// <summary>
        /// Fond des en-têtes (#161E29).
        /// </summary>
        public static Color Header { get; private set; } = Color.FromArgb(0x16, 0x1E, 0x29);

        /// <summary>
        /// Fond au survol (#1A2430).
        /// </summary>
        public static Color Hover { get; private set; } = Color.FromArgb(0x1A, 0x24, 0x30);

        /// <summary>
        /// Fond d'un élément sélectionné (#223041).
        /// </summary>
        public static Color Selected { get; private set; } = Color.FromArgb(0x22, 0x30, 0x41);

        /// <summary>
        /// Couleur des lignes séparatrices (#1F2A37).
        /// </summary>
        public static Color Separator { get; private set; } = Color.FromArgb(0x1F, 0x2A, 0x37);

        /// <summary>
        /// Texte principal (#E8EEF4).
        /// </summary>
        public static Color TextPrimary { get; private set; } = Color.FromArgb(0xE8, 0xEE, 0xF4);

        /// <summary>
        /// Texte secondaire (#8A9BAD).
        /// </summary>
        public static Color TextSecondary { get; private set; } = Color.FromArgb(0x8A, 0x9B, 0xAD);

        /// <summary>
        /// Couleur d'accentuation bleu ciel (#38BDF8).
        /// </summary>
        public static Color Accent { get; private set; } = Color.FromArgb(0x38, 0xBD, 0xF8);

        /// <summary>
        /// Texte sur fond d'accentuation (#04121C).
        /// </summary>
        public static readonly Color AccentText = Color.FromArgb(0x04, 0x12, 0x1C);

        /// <summary>
        /// Bulle de message entrant (#1C2631).
        /// </summary>
        public static Color BubbleIncoming { get; private set; } = Color.FromArgb(0x1C, 0x26, 0x31);

        /// <summary>
        /// Bulle de message sortant (#38BDF8).
        /// </summary>
        public static Color BubbleOutgoing { get; private set; } = Color.FromArgb(0x38, 0xBD, 0xF8);

        /// <summary>
        /// Texte de message sortant (#04121C).
        /// </summary>
        public static Color OutgoingText { get; private set; } = Color.FromArgb(0x04, 0x12, 0x1C);

        /// <summary>
        /// Métadonnées de message sortant : heure et coches (#0B4A68).
        /// </summary>
        public static Color OutgoingMeta { get; private set; } = Color.FromArgb(0x0B, 0x4A, 0x68);

        /// <summary>
        /// Fond d'une citation dans une bulle de message sortant (opacité réduite).
        /// </summary>
        public static Color BubbleOutgoingReplyBackground { get; private set; } = Color.FromArgb(40, 0x0B, 0x4A, 0x68);

        /// <summary>
        /// Coche de message lu (#FFFFFF).
        /// </summary>
        public static Color TickRead { get; private set; } = Color.FromArgb(0xFF, 0xFF, 0xFF);

        /// <summary>
        /// Fond du champ de recherche (#1A2430).
        /// </summary>
        public static Color SearchBackground { get; private set; } = Color.FromArgb(0x1A, 0x24, 0x30);

        /// <summary>
        /// Couleurs d'auteurs pour les discussions de groupe.
        /// </summary>
        public static readonly Color[] AuthorColors =
        {
            Color.FromArgb(0x7D, 0xD3, 0xFC), // #7DD3FC
            Color.FromArgb(0xA7, 0x8B, 0xFA), // #A78BFA
            Color.FromArgb(0xF9, 0xA8, 0xD4), // #F9A8D4
            Color.FromArgb(0xFC, 0xD3, 0x4D), // #FCD34D
            Color.FromArgb(0x86, 0xEF, 0xAC), // #86EFAC
            Color.FromArgb(0xFD, 0xBA, 0x74)  // #FDBA74
        };

        /// <summary>
        /// Palette d'avatars variée et lisible sur fond sombre.
        /// </summary>
        public static readonly Color[] AvatarPalette =
        {
            Color.FromArgb(0x0D, 0x94, 0x88), // Teal
            Color.FromArgb(0x25, 0x63, 0xEB), // Blue
            Color.FromArgb(0x7C, 0x3A, 0xED), // Violet
            Color.FromArgb(0xD9, 0x77, 0x06), // Amber
            Color.FromArgb(0x05, 0x96, 0x69), // Emerald
            Color.FromArgb(0xDB, 0x27, 0x77), // Pink
            Color.FromArgb(0x4F, 0x46, 0xE5), // Indigo
            Color.FromArgb(0xE1, 0x1D, 0x48)  // Rose
        };

        public static void SetAccent(Color color)
        {
            Accent = color;
            BubbleOutgoing = color;
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void SetThemeMode(string mode)
        {
            SetThemeMode(mode.Equals("light", StringComparison.OrdinalIgnoreCase)
                ? ThemeMode.Light
                : mode.Equals("system", StringComparison.OrdinalIgnoreCase)
                    ? ThemeMode.System
                    : ThemeMode.Dark);
        }

        public static void SetThemeMode(ThemeMode mode)
        {
            Mode = mode;
            bool light = mode == ThemeMode.Light
                || (mode == ThemeMode.System && SystemColors.Window.R > 180);

            if (light)
            {
                AppBackground = Color.FromArgb(0xF5, 0xF7, 0xFA);
                Panel = Color.FromArgb(0xFF, 0xFF, 0xFF);
                Header = Color.FromArgb(0xEE, 0xF2, 0xF6);
                Hover = Color.FromArgb(0xE6, 0xEC, 0xF2);
                Selected = Color.FromArgb(0xDC, 0xEB, 0xF7);
                Separator = Color.FromArgb(0xDD, 0xE5, 0xEC);
                TextPrimary = Color.FromArgb(0x0F, 0x1A, 0x24);
                TextSecondary = Color.FromArgb(0x5B, 0x6B, 0x7B);
                SearchBackground = Color.FromArgb(0xF5, 0xF7, 0xFA);
                Accent = Color.FromArgb(0x0E, 0xA5, 0xE9);
                BubbleOutgoing = Color.FromArgb(0xBF, 0xE8, 0xFB);
                BubbleIncoming = Color.White;
                OutgoingText = Color.FromArgb(0x0F, 0x1A, 0x24);
                OutgoingMeta = Color.FromArgb(0x5B, 0x6B, 0x7B);
                BubbleOutgoingReplyBackground = Color.FromArgb(40, 0x0E, 0xA5, 0xE9);
                TickRead = Color.FromArgb(0x0E, 0xA5, 0xE9);
            }
            else
            {
                AppBackground = Color.FromArgb(0x0B, 0x11, 0x19);
                Panel = Color.FromArgb(0x11, 0x19, 0x23);
                Header = Color.FromArgb(0x17, 0x23, 0x30);
                Hover = Color.FromArgb(0x1D, 0x2A, 0x38);
                Selected = Color.FromArgb(0x22, 0x30, 0x41);
                Separator = Color.FromArgb(0x1F, 0x2A, 0x37);
                TextPrimary = Color.FromArgb(0xF4, 0xF7, 0xFA);
                TextSecondary = Color.FromArgb(0x8F, 0xA6, 0xBC);
                SearchBackground = Color.FromArgb(0x1D, 0x2A, 0x38);
                Accent = Color.FromArgb(0x38, 0xBD, 0xF8);
                BubbleOutgoing = Color.FromArgb(0x24, 0xB8, 0xEC);
                BubbleIncoming = Color.FromArgb(0x1D, 0x2A, 0x38);
                OutgoingText = Color.FromArgb(0x04, 0x12, 0x1C);
                OutgoingMeta = Color.FromArgb(0x0B, 0x4A, 0x68);
                BubbleOutgoingReplyBackground = Color.FromArgb(40, 0x0B, 0x4A, 0x68);
                TickRead = Color.White;
            }
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
