using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Design tokens do SafetyLens. Paleta derivada da sinalização de segurança ISO 3864
    /// (amarelo = atenção, vermelho = risco, azul = EPI obrigatório, verde = condição segura)
    /// sobre neutros frios, para manter contraste AA tanto em tela cheia quanto sobre a câmera.
    /// Medidas em pontos lógicos (iPhone 12 = 390 de largura); a overlay converte para pixels.
    /// </summary>
    public static class SafetyLensTheme
    {
        public const float ReferenceWidth = 390f;

        public static readonly Color Background = Hex(0xF2F4F7);
        public static readonly Color Surface = Hex(0xFFFFFF);
        public static readonly Color SurfaceMuted = Hex(0xE9EDF2);
        public static readonly Color Border = Hex(0xD5DBE3);

        public static readonly Color Ink = Hex(0x0F1B2D);
        public static readonly Color InkSoft = Hex(0x3D4B5C);
        public static readonly Color InkFaint = Hex(0x5B6A7D);
        public static readonly Color OnDark = Hex(0xFFFFFF);

        public static readonly Color Brand = Hex(0xFFC21A);
        public static readonly Color OnBrand = Ink;

        public static readonly Color Danger = Hex(0xC8281C);
        public static readonly Color DangerSoft = Hex(0xFDECEA);
        public static readonly Color Info = Hex(0x1A63D6);
        public static readonly Color InfoSoft = Hex(0xE6EFFD);
        public static readonly Color Success = Hex(0x0F7A55);
        public static readonly Color SuccessSoft = Hex(0xE3F4EC);

        public static readonly Color Scrim = new Color(0.06f, 0.10f, 0.17f, 0.78f);
        public static readonly Color ReticleStroke = new Color(1f, 1f, 1f, 0.92f);

        public static readonly Color HazardAlertActive = Hex(0xE5372A);
        public static readonly Color HazardEpiActive = Hex(0x2B7BF0);
        public static readonly Color HazardDone = Hex(0x1E9E6E);
        public static readonly Color HazardIdle = Hex(0x56606C);
        public static readonly Color ArLabel = Hex(0xFFFFFF);
        public static readonly Color ArLabelShadow = Hex(0x0F1B2D);

        // Placas de advertência (ISO 7010 série W): face amarela, borda e pictograma pretos.
        public static readonly Color SignFace = Brand;
        public static readonly Color SignInk = Hex(0x14161A);
        // Tints multiplicados sobre a cor base do objeto 3D conforme o estado do item.
        public static readonly Color HazardTintActive = Hex(0xFFFFFF);
        public static readonly Color HazardTintIdle = new Color(0.62f, 0.64f, 0.68f, 0.82f);
        // Concluído só escurece levemente; o anel verde comunica o estado (sem pintar a placa de verde).
        public static readonly Color HazardTintDone = new Color(0.86f, 0.86f, 0.86f, 1f);
        public static readonly Color HelmetBase = Brand;

        /// <summary>Dimensões dos objetos de RA em metros (marcador de 20 x 13 cm).</summary>
        public const float ArSignWidth = 0.04f;
        public const float ArSignLift = 0.003f;
        public const float ArHelmetWidth = 0.042f;
        public const float ArRingDiameter = 0.056f;
        public const float ArRingLift = 0.001f;
        public const float ArDoneRingScale = 0.85f;
        public const float ArLabelGap = 0.005f;
        public const float ArLabelScale = 0.0034f;
        public const float ArActiveScale = 1.08f;
        public const float ArIdleScale = 0.88f;
        public const float ArDoneScale = 0.92f;
        public const float ArPulseAmplitude = 0.05f;
        public const float ArPulseSpeed = 2.4f;
        /// <summary>|normal·up| abaixo disso = marcador em pé (tela, parede, máquina).</summary>
        public const float ArVerticalMarkerDot = 0.6f;

        public const float SpaceXs = 4f;
        public const float SpaceSm = 8f;
        public const float SpaceMd = 12f;
        public const float SpaceLg = 16f;
        public const float SpaceXl = 24f;
        public const float Space2Xl = 32f;

        public const float RadiusSm = 10f;
        public const float RadiusMd = 16f;
        public const float RadiusLg = 24f;

        public const float StrokeThin = 1.5f;
        public const float StrokeThick = 2f;

        public const float ControlHeight = 56f;
        public const float TouchMin = 44f;

        public const int FontCaption = 13;
        public const int FontOverline = 12;
        public const int FontBody = 15;
        public const int FontBodyLarge = 17;
        public const int FontTitle = 22;
        public const int FontDisplay = 30;
        public const int FontScore = 40;

        /// <summary>Duração do toast de feedback, em segundos.</summary>
        public const float FeedbackDuration = 2.4f;

        static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f,
            1f);
    }
}
