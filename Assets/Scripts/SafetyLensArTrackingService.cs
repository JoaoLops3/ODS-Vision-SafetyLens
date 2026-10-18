using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Image Tracking apenas: primeiro marcador trava a sessão, graça P1-6 e câmera sob demanda.
    /// Sem plane detection.
    /// </summary>
    public sealed class SafetyLensArTrackingService
    {
        readonly ARSession _arSession;
        readonly MonoBehaviour _host;
        TrackingLossGate _gate;
        ARTrackedImage _trackedImage;
        string _sessionTargetName;
        bool _targetFound;
        bool _targetEverFound;
        bool _targetUnstable;
        string _availabilityMessage;

        /// <summary>Cria o serviço com a sessão AR da cena e o host das corrotinas de câmera.</summary>
        public SafetyLensArTrackingService(ARSession arSession, MonoBehaviour host)
        {
            _arSession = arSession ?? throw new ArgumentNullException(nameof(arSession));
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>True enquanto o marcador da sessão está em Tracking.</summary>
        public bool TargetFound => _targetFound;

        /// <summary>True depois do primeiro reconhecimento (não zera em Lost).</summary>
        public bool TargetEverFound => _targetEverFound;

        /// <summary>True durante a graça Limited (banner amarelo; toque bloqueado).</summary>
        public bool TargetUnstable => _targetUnstable;

        /// <summary>Nome do primeiro Image Target reconhecido. Null até o primeiro lock.</summary>
        public string SessionTargetName => _sessionTargetName;

        /// <summary>Trackable corrente, ou null se removido.</summary>
        public ARTrackedImage TrackedImage => _trackedImage;

        /// <summary>ARSession ativo na hierarquia.</summary>
        public bool IsCameraActive => _arSession != null && _arSession.gameObject.activeSelf;

        /// <summary>Banner de graça Limited. Null fora de Unstable.</summary>
        public string TargetStatusMessage =>
            _targetUnstable ? "Rastreamento instável. Mantenha o marcador na câmera." : null;

        /// <summary>P1-8: device sem AR / sessão que não sobe. Null quando OK.</summary>
        public string AvailabilityMessage => _availabilityMessage;

        /// <summary>Instala o gate de graça (P1-6).</summary>
        public void ConfigureGate(float graceSeconds, int minFrames)
        {
            _gate = new TrackingLossGate(graceSeconds, minFrames);
        }

        /// <summary>Limpa lock, trackable e instabilidade. Não desliga a câmera.</summary>
        public void ResetLock()
        {
            _trackedImage = null;
            _sessionTargetName = null;
            _targetFound = false;
            _targetEverFound = false;
            _targetUnstable = false;
            _availabilityMessage = null;
            _gate?.Reset();
        }

        /// <summary>Zera só o gate e a instabilidade (entrada no quiz / restart parcial).</summary>
        public void ResetGate()
        {
            _gate?.Reset();
            _targetUnstable = false;
        }

        /// <summary>Marca o trackable como nulo quando o AR Foundation remove o id da sessão.</summary>
        public void ClearTrackedImageIf(TrackableId id)
        {
            if (_trackedImage != null && id == _trackedImage.trackableId)
                _trackedImage = null;
        }

        /// <summary>
        /// Processa um trackable adicionado/atualizado. Retorna true se entrou em Tracking
        /// no marcador da sessão (primeiro lock ou reaquisição).
        /// </summary>
        public bool TryHandleImage(ARTrackedImage image, SafetyLensScenario scenario)
        {
            if (image == null || scenario == null) return false;
            var name = image.referenceImage.name;
            if (!scenario.IsKnownTarget(name)) return false;
            if (_sessionTargetName != null && !string.Equals(name, _sessionTargetName, StringComparison.Ordinal))
                return false;

            _trackedImage = image;
            if (image.trackingState != TrackingState.Tracking) return false;

            ApplyFound(image);
            return true;
        }

        /// <summary>Aplica Tracking: lock do nome, found=true. Caller sobe conteúdo e fase.</summary>
        public void ApplyFound(ARTrackedImage image)
        {
            if (image == null) return;
            _trackedImage = image;
            _sessionTargetName ??= image.referenceImage.name;
            _targetFound = true;
        }

        /// <summary>Marca que a sessão já reconheceu o alvo (Scanning → EquipmentBrief).</summary>
        public void MarkEverFound() => _targetEverFound = true;

        /// <summary>Lost: found=false. Caller esconde overlays e zera seleção AR.</summary>
        public void ApplyLost()
        {
            _targetFound = false;
            _targetUnstable = false;
        }

        /// <summary>
        /// Avalia graça P1-6 em EquipmentBrief e RiskInspection. Fora dessas fases reseta o gate.
        /// <paramref name="becameFound"/> / <paramref name="becameLost"/> marcam a transição (não o estado estável).
        /// </summary>
        public TargetVisibility UpdateVisibility(
            AppPhase phase,
            float unscaledTime,
            out bool becameFound,
            out bool becameLost)
        {
            becameFound = false;
            becameLost = false;

            if (phase != AppPhase.RiskInspection && phase != AppPhase.EquipmentBrief)
            {
                _gate?.Reset();
                _targetUnstable = false;
                return TargetVisibility.Tracking;
            }

            var isTracking = _trackedImage != null && _trackedImage.trackingState == TrackingState.Tracking;
            var visibility = _gate != null
                ? _gate.Evaluate(isTracking, unscaledTime)
                : (isTracking ? TargetVisibility.Tracking : TargetVisibility.Lost);

            switch (visibility)
            {
                case TargetVisibility.Tracking:
                    if (!_targetFound && _trackedImage != null)
                    {
                        ApplyFound(_trackedImage);
                        becameFound = true;
                    }
                    _targetUnstable = false;
                    break;
                case TargetVisibility.Unstable:
                    _targetUnstable = true;
                    break;
                case TargetVisibility.Lost:
                    _targetUnstable = false;
                    if (_targetFound)
                    {
                        ApplyLost();
                        becameLost = true;
                    }
                    break;
            }

            return visibility;
        }

        /// <summary>
        /// Liga/desliga o GameObject do ARSession. No iOS o background é recuperado
        /// por corrotina (BeforeOpaques, sem mexer em clearFlags).
        /// </summary>
        public void SetCameraActive(bool active)
        {
            if (_arSession.gameObject.activeSelf != active)
                _arSession.gameObject.SetActive(active);

            if (active)
                _host.StartCoroutine(RecoverArCameraBackground());
        }

        /// <summary>
        /// QR viewer: esconde o feed da câmera e usa fundo sólido do tema.
        /// O marcador WL03 não chama isto — continua com a câmera AR.
        /// </summary>
        public void SetCameraFeedVisible(bool visible)
        {
            var manager = UnityEngine.Object.FindFirstObjectByType<ARCameraManager>(FindObjectsInactive.Include);
            var background = UnityEngine.Object.FindFirstObjectByType<ARCameraBackground>(FindObjectsInactive.Include);
            Camera cam = null;
            if (manager != null)
                cam = manager.GetComponent<Camera>();
            if (cam == null)
                cam = Camera.main;

            if (visible)
            {
                if (manager != null)
                {
                    manager.requestedBackgroundRenderingMode = CameraBackgroundRenderingMode.BeforeOpaques;
                    manager.enabled = true;
                }

                if (background != null)
                    background.enabled = true;

                // AR Foundation assume Depth; não forçar SolidColor no restore.
                if (cam != null)
                    cam.clearFlags = CameraClearFlags.Depth;
            }
            else
            {
                if (background != null)
                    background.enabled = false;
                if (manager != null)
                    manager.enabled = false;

                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = SafetyLensTheme.QrViewerBackdrop;
                }
            }
        }

        /// <summary>Atualiza a mensagem P1-8 a partir de <see cref="ARSession.state"/>.</summary>
        public void RefreshAvailability()
        {
            switch (ARSession.state)
            {
                case ARSessionState.Unsupported:
                    _availabilityMessage = "Este aparelho não suporta Realidade Aumentada.";
                    break;
                case ARSessionState.None:
                    _availabilityMessage =
                        "Não foi possível iniciar a câmera AR. Verifique a permissão da câmera.";
                    break;
                default:
                    _availabilityMessage = null;
                    break;
            }
        }

        IEnumerator RecoverArCameraBackground()
        {
            var manager = UnityEngine.Object.FindFirstObjectByType<ARCameraManager>(FindObjectsInactive.Include);
            var background = UnityEngine.Object.FindFirstObjectByType<ARCameraBackground>(FindObjectsInactive.Include);

            if (manager != null)
            {
                manager.requestedBackgroundRenderingMode = CameraBackgroundRenderingMode.BeforeOpaques;
                manager.enabled = true;
            }

            var started = Time.realtimeSinceStartup;
            while (ARSession.state == ARSessionState.None
                   && Time.realtimeSinceStartup - started < SafetyLensAppConstants.ArCameraStartupTimeoutSeconds)
                yield return null;

            RefreshAvailability();

            if (background != null)
            {
                background.enabled = false;
                yield return null;
                background.enabled = true;
            }
        }
    }
}
