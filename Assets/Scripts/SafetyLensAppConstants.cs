namespace Facens.SafetyLens
{
    /// <summary>
    /// Constantes de produto e runtime. Evita strings e números mágicos espalhados
    /// pelo controller, loader e serviços de RA.
    /// </summary>
    public static class SafetyLensAppConstants
    {
        /// <summary>Limite do ID do participante (RF06 / sanitização).</summary>
        public const int MaxParticipantIdLength = 24;

        /// <summary>Nome do Image Target principal na Reference Image Library (placa WL03).</summary>
        public const string ExpectedTargetName = "SafetyLensEquipmentTarget";

        /// <summary>Nome do Image Target alternativo (QR, 12 cm).</summary>
        public const string QrTargetName = "SafetyLensQrTarget";

        /// <summary>Nome do TextAsset em Resources (sem extensão).</summary>
        public const string ScenarioResourceName = "safetylens_scenario";

        /// <summary>Caminho de projeto do JSON, usado em mensagens de erro.</summary>
        public const string ScenarioAssetPath = "Assets/Resources/safetylens_scenario.json";

        /// <summary>RNF02: iOS sem isso trava em 30 FPS e a média cola no mínimo do artigo.</summary>
        public const int TargetFrameRate = 60;

        /// <summary>Intervalo de amostragem de memória durante a inspeção (RNF02).</summary>
        public const int MemorySampleEveryFrames = 30;

        /// <summary>P1-6: graça em segundos antes de declarar marcador perdido.</summary>
        public const float TrackingGraceSeconds = 1.5f;

        /// <summary>P1-6: frames mínimos em Limited/None junto com a graça temporal.</summary>
        public const int TrackingGraceMinFrames = 10;

        /// <summary>Espera máxima pelo ARSession sair de None no cold start iOS.</summary>
        public const float ArCameraStartupTimeoutSeconds = 3f;

        /// <summary>Alcance do raycast de toque RF03.</summary>
        public const float HazardRaycastMaxDistance = 10f;

        /// <summary>Nome do GameObject raiz dos overlays 3D filhos do marcador.</summary>
        public const string ArContentRootName = "SafetyLens_ArContent";

        /// <summary>Fallback do painel RF12 quando o JSON omite o nome.</summary>
        public const string DefaultEquipmentName = "Equipamento";
    }
}
