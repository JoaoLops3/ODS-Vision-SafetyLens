namespace Facens.SafetyLens
{
    /// <summary>
    /// Máquina de estados da sessão. Só transições válidas mudam <see cref="Phase"/>.
    /// O IMGUI lê a fase pelo facade; não atribui fase diretamente.
    /// </summary>
    public sealed class SafetyLensSessionFlow
    {
        AppPhase _phaseBeforeHistory = AppPhase.Identification;

        /// <summary>Fase corrente da jornada (identificação → ficha → inspeção → quiz → telemetria? → resultado).</summary>
        public AppPhase Phase { get; private set; } = AppPhase.Identification;

        /// <summary>Volta à tela de ID. Sempre permitido (reinício / boot).</summary>
        public void EnterIdentification()
        {
            Phase = AppPhase.Identification;
            _phaseBeforeHistory = AppPhase.Identification;
        }

        /// <summary>Identification → Scanning após ID válido.</summary>
        public bool TryEnterScanning()
        {
            if (Phase != AppPhase.Identification) return false;
            Phase = AppPhase.Scanning;
            return true;
        }

        /// <summary>Scanning → EquipmentBrief no primeiro tracking.</summary>
        public bool TryEnterEquipmentBrief()
        {
            if (Phase != AppPhase.Scanning) return false;
            Phase = AppPhase.EquipmentBrief;
            return true;
        }

        /// <summary>EquipmentBrief → RiskInspection após Continuar com tracking estável.</summary>
        public bool TryEnterRiskInspection()
        {
            if (Phase != AppPhase.EquipmentBrief) return false;
            Phase = AppPhase.RiskInspection;
            return true;
        }

        /// <summary>RiskInspection → Quiz após o último hazard confirmado.</summary>
        public bool TryEnterQuiz()
        {
            if (Phase != AppPhase.RiskInspection) return false;
            Phase = AppPhase.Quiz;
            return true;
        }

        /// <summary>Quiz → Telemetry quando RF12 está habilitado.</summary>
        public bool TryEnterTelemetry()
        {
            if (Phase != AppPhase.Quiz) return false;
            Phase = AppPhase.Telemetry;
            return true;
        }

        /// <summary>Quiz ou Telemetry → Finished no export.</summary>
        public bool TryEnterFinished()
        {
            if (Phase != AppPhase.Quiz && Phase != AppPhase.Telemetry) return false;
            Phase = AppPhase.Finished;
            return true;
        }

        /// <summary>History só a partir de Identification ou Finished.</summary>
        public bool TryOpenHistory()
        {
            if (Phase != AppPhase.Identification && Phase != AppPhase.Finished) return false;
            _phaseBeforeHistory = Phase;
            Phase = AppPhase.History;
            return true;
        }

        /// <summary>Fecha o histórico restaurando Identification ou Finished.</summary>
        public bool TryCloseHistory()
        {
            if (Phase != AppPhase.History) return false;
            Phase = _phaseBeforeHistory == AppPhase.Finished
                ? AppPhase.Finished
                : AppPhase.Identification;
            return true;
        }
    }
}
