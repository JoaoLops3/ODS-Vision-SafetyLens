using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Ciclo do experimento SafetyLens (artigo): identificação → image tracking →
    /// overlay de riscos/EPIs → quiz → métricas (tempo/erros) → exportação local.
    /// Base técnica mesclada de WL01 (tracking/toque), WL03 (procedimento/timing) e WL04 (ordem).
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public sealed class SafetyLensSessionController : MonoBehaviour
    {
        readonly Dictionary<string, SafetyLensHazardVisual> _visuals = new();

        public const int MaxParticipantIdLength = 24;
        public const string ExpectedTargetName = "SafetyLensEquipmentTarget";

        ARTrackedImageManager _imageManager;
        ARSession _arSession;
        Camera _arCamera;
        SafetyLensScenario _scenario;
        SafetyLensInspectionProgress _progress;
        GameObject _contentRoot;
        ARTrackedImage _trackedImage;
        TrackingLossGate _gate;
        bool _targetUnstable;
        /// <summary>Primeiro marcador reconhecido na sessão (WL03 ou QR). Os demais são ignorados até reiniciar.</summary>
        string _sessionTargetName;

        // RNF02: FPS/memória medidos só durante a inspeção em RA (a fase que o artigo avalia).
        readonly SafetyLensPerformanceProbe _perf = new SafetyLensPerformanceProbe();
        const int MemorySampleEveryFrames = 30;
        int _perfFrames;

        [Header("Tracking Limited (P1-6)")]
        [SerializeField] float limitedGraceSeconds = 1.5f;
        [SerializeField] int limitedGraceMinFrames = 10;
        float _startedAt = -1f;
        float _finishedAt = -1f;
        /// <summary>RF04: momento da confirmação do último risco (fim da inspeção, início do quiz).</summary>
        float _inspectionEndedAt = -1f;
        DateTime _startedUtc;
        DateTime _finishedUtc;

        public AppPhase Phase { get; private set; } = AppPhase.Identification;
        public string ParticipantId { get; private set; } = string.Empty;
        public bool TargetFound { get; private set; }
        public bool TargetEverFound { get; private set; }
        public bool TargetUnstable => _targetUnstable;
        /// <summary>Aviso persistente em Limited/None dentro do prazo de graça. Null fora de Unstable.</summary>
        public string TargetStatusMessage =>
            _targetUnstable ? "Rastreamento instável. Mantenha o marcador na câmera." : null;
        public bool IsCameraActive => _arSession != null && _arSession.gameObject.activeSelf;
        public bool IsComplete => Phase == AppPhase.Finished;
        public string ScenarioTitle => _scenario?.title ?? "SafetyLens";
        public string StatusMessage { get; private set; } = string.Empty;
        /// <summary>P1-8: sessão AR não sobe (Unsupported / timeout). Null quando OK.</summary>
        public string ArAvailabilityMessage { get; private set; }
        public string IdentificationError { get; private set; } = string.Empty;
        public string FeedbackText { get; private set; } = string.Empty;
        public bool FeedbackIsError { get; private set; }
        public float FeedbackAt { get; private set; } = float.NegativeInfinity;
        public int QuizSelectedIndex { get; private set; } = -1;
        public string ExportSummary { get; private set; } = string.Empty;
        public bool ExportFailed { get; private set; }
        public string ShareStatus { get; private set; } = string.Empty;

        AppPhase _phaseBeforeHistory = AppPhase.Identification;
        public int ErrorCount => _progress?.ErrorCount ?? 0;
        public int HazardCount => _scenario?.hazards?.Length ?? 0;
        public int HazardConfirmedCount => _progress?.HazardConfirmedCount ?? 0;
        public float HazardProgress => HazardCount == 0 ? 0f : HazardConfirmedCount / (float)HazardCount;
        /// <summary>True após toque no objeto AR do item ativo — habilita “Confirmar item”.</summary>
        public bool CurrentHazardArSelected => _progress != null && _progress.CurrentHazardArSelected;
        /// <summary>RF03: confirmar só com marcador visível + seleção por toque no overlay.</summary>
        public bool CanConfirmCurrentHazard =>
            Phase == AppPhase.RiskInspection
            && TargetFound
            && !_targetUnstable
            && CurrentHazard != null
            && CurrentHazardArSelected;
        public SafetyLensHazard CurrentHazard
        {
            get
            {
                if (_progress == null || HazardCount == 0) return null;
                var i = _progress.ActiveHazardIndex;
                return i < 0 || i >= HazardCount ? null : _scenario.hazards[i];
            }
        }
        public int QuizCount => _scenario?.quiz?.Length ?? 0;
        public int ActiveQuizIndex => _progress?.ActiveQuizIndex ?? 0;
        public SafetyLensQuizQuestion CurrentQuiz
        {
            get
            {
                if (_progress == null || QuizCount == 0) return null;
                var i = _progress.ActiveQuizIndex;
                return i < 0 || i >= QuizCount ? null : _scenario.quiz[i];
            }
        }
        public int QuizCorrectCount => _progress?.QuizCorrect ?? 0;
        public float ElapsedSeconds =>
            _startedAt < 0f
                ? 0f
                : Mathf.Max(0f, (_finishedAt >= 0f ? _finishedAt : Time.realtimeSinceStartup) - _startedAt);

        /// <summary>RF04: tempo do reconhecimento do marcador à confirmação do último risco.</summary>
        public float InspectionSeconds =>
            _startedAt < 0f
                ? 0f
                : Mathf.Max(0f, (_inspectionEndedAt >= 0f ? _inspectionEndedAt : Time.realtimeSinceStartup) - _startedAt);

        /// <summary>RF04: tempo entre o fim da inspeção e a última resposta do quiz.</summary>
        public float QuizSeconds =>
            _inspectionEndedAt < 0f
                ? 0f
                : Mathf.Max(0f, (_finishedAt >= 0f ? _finishedAt : Time.realtimeSinceStartup) - _inspectionEndedAt);

        void Awake()
        {
            // RNF02: sem isso o iOS trava em 30 FPS e a média fica colada no mínimo do artigo,
            // impossibilitando provar "≥ 30". Custo assumido: mais bateria/aquecimento.
            Application.targetFrameRate = 60;
            _imageManager = GetComponent<ARTrackedImageManager>();
            _arSession = FindFirstObjectByType<ARSession>(FindObjectsInactive.Include);
            if (_arSession == null)
                throw new InvalidOperationException("ARSession nao encontrado na cena.");
            LoadScenario();
            PrewarmPictograms();
            _gate = new TrackingLossGate(limitedGraceSeconds, limitedGraceMinFrames);
            EnterIdentification();
        }

        /// <summary>Rasteriza as placas em background já no boot; ao achar o marcador elas estão prontas.</summary>
        void PrewarmPictograms()
        {
            var ids = new List<string>();
            foreach (var hazard in _scenario.hazards)
                if (hazard.IsAlert) ids.Add(hazard.ResolvedPictogram);
            SafetyLensPictograms.Prewarm(ids);
        }

        /// <summary>
        /// A câmera só liga quando o participante pede (botão "Abrir câmera") e desliga
        /// fora das etapas de RA. O GO do ARSession começa inativo na cena.
        /// No iOS o ARCameraBackground configura clearFlags sozinho (BeforeOpaques) —
        /// não forçamos SolidColor/AfterOpaques depois, senão o feed vira preto.
        /// </summary>
        void SetCameraActive(bool active)
        {
            if (_arSession.gameObject.activeSelf != active)
                _arSession.gameObject.SetActive(active);

            if (active)
                StartCoroutine(RecoverArCameraBackground());
        }

        IEnumerator RecoverArCameraBackground()
        {
            var manager = FindFirstObjectByType<ARCameraManager>(FindObjectsInactive.Include);
            var background = FindFirstObjectByType<ARCameraBackground>(FindObjectsInactive.Include);

            // BeforeOpaques: blit do feed antes da geometria — placas AlphaTest e capacete
            // não competem com o fullscreen do AfterOpaques.
            if (manager != null)
            {
                manager.requestedBackgroundRenderingMode = CameraBackgroundRenderingMode.BeforeOpaques;
                manager.enabled = true;
            }

            // Espera a sessão sair de None (frame único não basta no cold start iOS).
            const float timeout = 3f;
            var started = Time.realtimeSinceStartup;
            while (ARSession.state == ARSessionState.None
                   && Time.realtimeSinceStartup - started < timeout)
                yield return null;

            RefreshArAvailabilityMessage();

            if (background != null)
            {
                background.enabled = false;
                yield return null;
                background.enabled = true;
            }
            // Não tocar em cam.clearFlags: o ARCameraBackground manda nisso.
        }

        /// <summary>
        /// Distinto de "Marcador perdido": device sem ARKit / sessão que não inicia.
        /// Não muda a fase — evita zerar o experimento por falha de plataforma.
        /// </summary>
        void RefreshArAvailabilityMessage()
        {
            switch (ARSession.state)
            {
                case ARSessionState.Unsupported:
                    ArAvailabilityMessage = "Este aparelho não suporta Realidade Aumentada.";
                    StatusMessage = ArAvailabilityMessage;
                    break;
                case ARSessionState.None:
                    ArAvailabilityMessage =
                        "Não foi possível iniciar a câmera AR. Verifique a permissão da câmera.";
                    StatusMessage = ArAvailabilityMessage;
                    break;
                default:
                    ArAvailabilityMessage = null;
                    break;
            }
        }

        /// <summary>Mantém só caracteres seguros para nome de arquivo e CSV.</summary>
        public static string SanitizeParticipantId(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var sb = new System.Text.StringBuilder(Mathf.Min(raw.Length, MaxParticipantIdLength));
            foreach (var ch in raw)
            {
                if (sb.Length >= MaxParticipantIdLength) break;
                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == ' ')
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        void ShowFeedback(string text, bool isError)
        {
            FeedbackText = text;
            FeedbackIsError = isError;
            FeedbackAt = Time.realtimeSinceStartup;
        }

        void OnEnable()
        {
            _imageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
            // Pose do marcador fecha em onBeforeRender — sincroniza âncoras A/B/C depois do ARKit.
            Application.onBeforeRender += AlignContentToMarker;
        }

        void OnDisable()
        {
            _imageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
            Application.onBeforeRender -= AlignContentToMarker;
        }

        void LoadScenario()
        {
            var asset = Resources.Load<TextAsset>("safetylens_scenario");
            if (asset == null)
                throw new InvalidOperationException("Assets/Resources/safetylens_scenario.json nao foi encontrado.");

            _scenario = JsonUtility.FromJson<SafetyLensScenario>(asset.text);
            var errors = SafetyLensScenarioValidator.Validate(_scenario, ExpectedTargetName);
            if (errors.Count > 0)
                throw new InvalidOperationException("Cenario SafetyLens invalido: " + string.Join(" ", errors));

            _progress = new SafetyLensInspectionProgress(_scenario.hazards.Length);
        }

        public void StartSession(string participantId)
        {
            if (Phase != AppPhase.Identification) return;

            var trimmed = SanitizeParticipantId(participantId).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                IdentificationError = "Digite seu ID para continuar (ex.: P01).";
                return;
            }

            IdentificationError = string.Empty;
            ArAvailabilityMessage = null;
            ParticipantId = trimmed;
            TargetFound = false;
            TargetEverFound = false;
            _trackedImage = null;
            _sessionTargetName = null;
            _targetUnstable = false;
            _gate.Reset();
            QuizSelectedIndex = -1;
            FeedbackAt = float.NegativeInfinity;
            _progress = new SafetyLensInspectionProgress(HazardCount);
            _startedAt = -1f;
            _finishedAt = -1f;
            _inspectionEndedAt = -1f;
            _perf.Reset();
            _perfFrames = 0;
            ExportSummary = string.Empty;
            ExportFailed = false;
            Phase = AppPhase.Scanning;
            StatusMessage = "Enquadre a placa do equipamento ou o QR Code do treinamento.";
            SetCameraActive(true);
            RefreshVisuals();
            Debug.Log($"[SafetyLens] Sessao iniciada para participante {ParticipantId}.");
        }

        void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            if (Phase == AppPhase.Identification) return;

            foreach (var image in args.added) HandleTrackedImage(image);
            foreach (var image in args.updated) HandleTrackedImage(image);
            foreach (var removed in args.removed)
            {
                if (_trackedImage != null && removed.Key == _trackedImage.trackableId)
                    _trackedImage = null;
            }
        }

        void HandleTrackedImage(ARTrackedImage image)
        {
            var name = image.referenceImage.name;
            if (!_scenario.IsKnownTarget(name)) return;
            // Sessão presa ao primeiro marcador reconhecido: com WL03 e QR no quadro ao mesmo
            // tempo, o segundo não rouba o conteúdo (markerName vira variável de controle).
            if (_sessionTargetName != null && !string.Equals(name, _sessionTargetName, StringComparison.Ordinal))
                return;
            _trackedImage = image;
            if (image.trackingState != TrackingState.Tracking) return;
            SetTargetFound(true, image);
        }

        void SetTargetFound(bool found, ARTrackedImage image = null)
        {
            if (found && image != null)
            {
                _trackedImage = image;
                _sessionTargetName ??= image.referenceImage.name;
                // RF04: cronômetro só no primeiro tracking — reaquisições não zeram o tempo.
                if (_startedAt < 0f)
                {
                    _startedAt = Time.realtimeSinceStartup;
                    _startedUtc = DateTime.UtcNow;
                }

                EnsureArContent(image.transform);
                if (_contentRoot != null) _contentRoot.SetActive(true);

                if (Phase == AppPhase.Scanning)
                {
                    Phase = AppPhase.RiskInspection;
                    TargetEverFound = true;
                    ShowFeedback("Marcador reconhecido", false);
                }

                TargetFound = true;
                return;
            }

            TargetFound = false;
            if (_contentRoot != null) _contentRoot.SetActive(false);
            _progress?.ClearArSelection();
            if (Phase == AppPhase.RiskInspection || Phase == AppPhase.Scanning)
                StatusMessage = "Marcador perdido. Aponte a câmera para a placa de novo.";
        }

        void UpdateTargetVisibility()
        {
            if (Phase != AppPhase.RiskInspection)
            {
                _gate.Reset();
                _targetUnstable = false;
                return;
            }

            var isTracking = _trackedImage != null && _trackedImage.trackingState == TrackingState.Tracking;
            switch (_gate.Evaluate(isTracking, Time.unscaledTime))
            {
                case TargetVisibility.Tracking:
                    if (!TargetFound) SetTargetFound(true, _trackedImage);
                    _targetUnstable = false;
                    break;
                case TargetVisibility.Unstable:
                    _targetUnstable = true;
                    break;
                case TargetVisibility.Lost:
                    _targetUnstable = false;
                    if (TargetFound) SetTargetFound(false);
                    break;
            }
        }

        void EnsureArContent(Transform target)
        {
            var created = false;
            if (_contentRoot == null)
            {
                // Filho do marcador: cada hazard usa UV (A/B/C) no espaço local do ARTrackedImage.
                _contentRoot = new GameObject("SafetyLens_ArContent");
                foreach (var hazard in _scenario.hazards)
                    _visuals[hazard.id] = SafetyLensArVisuals.Create(hazard, _contentRoot.transform);
                created = true;
            }

            if (created)
            {
                foreach (var visual in _visuals.Values)
                    SafetyLensArVisuals.ApplyMounting(visual);
            }

            AlignContentToMarker();
            RefreshVisuals();
        }

        Camera ResolveArCamera()
        {
            if (_arCamera != null) return _arCamera;

            var manager = FindFirstObjectByType<ARCameraManager>(FindObjectsInactive.Include);
            if (manager != null)
                _arCamera = manager.GetComponent<Camera>();
            if (_arCamera == null)
                _arCamera = Camera.main;
            return _arCamera;
        }

        /// <summary>
        /// Conteúdo parentalizado no ARTrackedImage: HOT/PINCH/EPI ficam nas letras A/B/C da arte.
        /// Só a orientação billboard aponta para a câmera — a posição é âncora de mundo no marcador.
        /// </summary>
        void AlignContentToMarker()
        {
            if (_contentRoot == null || !TargetFound || _trackedImage == null) return;

            var marker = _trackedImage.transform;
            if (_contentRoot.transform.parent != marker)
                _contentRoot.transform.SetParent(marker, false);

            _contentRoot.transform.localPosition = Vector3.zero;
            _contentRoot.transform.localRotation = Quaternion.identity;
            _contentRoot.transform.localScale = Vector3.one;

            var size = _trackedImage.size;
            if (size.x < 1e-4f || size.y < 1e-4f)
                size = new Vector2(SafetyLensTheme.ArMarkerWidth, SafetyLensTheme.ArMarkerHeight);

            foreach (var hazard in _scenario.hazards)
            {
                if (!_visuals.TryGetValue(hazard.id, out var visual) || visual.Root == null) continue;
                // UV do marcador da sessão: letras A/B/C no WL03, layout próprio no QR.
                var uv = _scenario.ResolveUv(hazard, _sessionTargetName);
                visual.Root.localPosition = hazard.LocalPositionOnMarker(size, uv.x, uv.y);
                if (visual.Zone != null)
                {
                    // Zona no plano da arte, sob a placa: mesmo UV, lift próprio.
                    var zonePos = hazard.LocalPositionOnMarker(size, uv.x, uv.y);
                    zonePos.y = SafetyLensTheme.ArZoneLift;
                    visual.Zone.localPosition = zonePos;
                }
            }

            FaceHazardsTowardCamera();
        }

        /// <summary>
        /// Placas: billboard para a câmera. EPI 3D: fica no espaço do marcador (+Y saindo da arte),
        /// assentado à frente da letra — billboard faria o capacete “subir” na tela.
        /// </summary>
        void FaceHazardsTowardCamera()
        {
            var cam = ResolveArCamera();
            if (cam == null) return;

            var camPos = cam.transform.position;
            var camUp = cam.transform.up;
            foreach (var visual in _visuals.Values)
            {
                if (visual.Root == null) continue;

                if (visual.IsModel)
                {
                    visual.Root.localRotation = Quaternion.identity;
                    continue;
                }

                var toCam = camPos - visual.Root.position;
                if (toCam.sqrMagnitude < 1e-8f) continue;
                visual.Root.rotation = Quaternion.LookRotation(toCam.normalized, camUp);
            }
        }

        void Update()
        {
            if (Phase == AppPhase.RiskInspection)
            {
                _perf.AddFrame(Time.unscaledDeltaTime);
                // Profiler.GetTotalAllocatedMemoryLong tem custo; 1 leitura a cada 30 frames basta para o pico.
                if (++_perfFrames % MemorySampleEveryFrames == 0)
                    _perf.RecordMemoryBytes(Profiler.GetTotalAllocatedMemoryLong());
            }

            UpdateTargetVisibility();
            if ((Phase == AppPhase.Scanning || Phase == AppPhase.RiskInspection) && IsCameraActive
                && (ARSession.state == ARSessionState.Unsupported
                    || ARSession.state == ARSessionState.Ready
                    || ARSession.state == ARSessionState.SessionTracking
                    || ArAvailabilityMessage != null))
                RefreshArAvailabilityMessage();

            if (Phase != AppPhase.RiskInspection || !TargetFound || Input.touchCount != 1) return;

            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Ended || SafetyLensStatusOverlay.IsTouchOverOverlay(touch.position))
                return;

            if (_targetUnstable) return;

            var camera = ResolveArCamera();
            if (camera == null) return;

            var tag = PickHazard(camera, touch.position);
            if (tag == null || CurrentHazard == null) return;

            if (tag.hazardId == CurrentHazard.id)
                SelectCurrentHazardFromAr();
            else
            {
                _progress.ClearArSelection();
                RegisterError($"Selecao incorreta: {tag.hazardId}. Esperado: {CurrentHazard.id}.");
                ShowFeedback($"Não é esse. Procure: {CurrentHazard.label}", true);
            }
        }

        /// <summary>
        /// RF03 — primeiro passo: toque no overlay AR do item ativo. Só depois o botão confirma.
        /// </summary>
        void SelectCurrentHazardFromAr()
        {
            if (Phase != AppPhase.RiskInspection || !TargetFound || CurrentHazard == null) return;
            if (!_progress.SelectCurrentHazardFromAr()) return;

            ShowFeedback($"Selecionado: {CurrentHazard.label}. Confirme abaixo.", false);
            Debug.Log($"[SafetyLens] Hazard selecionado via AR: {CurrentHazard.id}");
        }

        /// <summary>
        /// Raycast exato primeiro; se errar, aceita o item cujo centro projetado está dentro de um
        /// raio mínimo de toque. Os objetos têm poucos centímetros e, de longe, viram poucos pixels —
        /// sem essa folga o cilindro do EPI era praticamente impossível de acertar.
        /// </summary>
        SafetyLensHazardTag PickHazard(Camera camera, Vector2 screenPosition)
        {
            if (Physics.Raycast(camera.ScreenPointToRay(screenPosition), out var hit, 10f))
            {
                var direct = hit.collider.GetComponent<SafetyLensHazardTag>();
                if (direct != null) return direct;
            }

            var pointsToPixels = Mathf.Min(Screen.width, Screen.height) / SafetyLensTheme.ReferenceWidth;
            var minRadius = SafetyLensTheme.TouchMin * pointsToPixels;
            SafetyLensHazardTag best = null;
            var bestDistance = float.MaxValue;

            foreach (var visual in _visuals.Values)
            {
                var renderer = visual.Renderer;
                if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;

                var bounds = renderer.bounds;
                var center = camera.WorldToScreenPoint(bounds.center);
                if (center.z <= 0f) continue;

                var edge = camera.WorldToScreenPoint(bounds.center + camera.transform.right * bounds.extents.magnitude);
                var radius = Mathf.Max(minRadius, Vector2.Distance(center, edge));
                var distance = Vector2.Distance(screenPosition, center);
                if (distance > radius || distance >= bestDistance) continue;

                bestDistance = distance;
                best = visual.Body.GetComponent<SafetyLensHazardTag>();
            }

            return best;
        }

        public void ConfirmCurrentHazard()
        {
            // Sem seleção AR o botão não confirma — evita burlar RF03 / métricas de toque.
            if (!CanConfirmCurrentHazard) return;

            var confirmedLabel = CurrentHazard.label;
            var confirmedId = CurrentHazard.id;
            var inspectionDone = _progress.ConfirmCurrentHazard(HazardCount);
            Debug.Log($"[SafetyLens] Hazard confirmado (AR+botão): {confirmedId}");

            if (inspectionDone)
            {
                // RF04: aqui fecha o tempo de inspeção; o que vier depois é tempo de quiz.
                _inspectionEndedAt = Time.realtimeSinceStartup;
                // Última leitura de memória: inspeções curtas (< 30 frames) não ficariam sem amostra.
                _perf.RecordMemoryBytes(Profiler.GetTotalAllocatedMemoryLong());
                _gate.Reset();
                _targetUnstable = false;
                Phase = AppPhase.Quiz;
                QuizSelectedIndex = -1;
                if (_contentRoot != null) _contentRoot.SetActive(false);
                SetCameraActive(false);
                ShowFeedback("Todos os itens identificados", false);
            }
            else
                ShowFeedback($"{confirmedLabel} identificado", false);

            RefreshVisuals();
        }

        /// <summary>
        /// Registra a resposta e a revela (certa/errada) antes de avançar: o quiz é de fixação,
        /// então o participante precisa ver a alternativa correta. Avanço via <see cref="NextQuiz"/>.
        /// </summary>
        public void AnswerQuiz(int optionIndex)
        {
            if (Phase != AppPhase.Quiz || CurrentQuiz == null || QuizSelectedIndex >= 0) return;
            if (!_progress.TryAnswerQuiz(CurrentQuiz, optionIndex, QuizCount, out var correct, out var isLast))
                return;

            QuizSelectedIndex = optionIndex;
            if (!correct)
            {
                var selected = CurrentQuiz.options[optionIndex];
                StatusMessage = $"Quiz Q{CurrentQuiz.id}: resposta incorreta ({selected}). Erros: {ErrorCount}.";
                Debug.LogWarning($"[SafetyLens] Erro #{ErrorCount}: Quiz Q{CurrentQuiz.id}: resposta incorreta ({selected}).");
            }

            // RF04: o tempo da tarefa termina na última resposta, não no tempo de leitura do resultado.
            if (isLast)
            {
                _finishedAt = Time.realtimeSinceStartup;
                _finishedUtc = DateTime.UtcNow;
            }
        }

        public void NextQuiz()
        {
            if (Phase != AppPhase.Quiz || QuizSelectedIndex < 0) return;

            QuizSelectedIndex = -1;
            if (_progress.NextQuiz(QuizCount))
                FinishSession();
        }

        void FinishSession()
        {
            if (_finishedAt < 0f)
            {
                _finishedAt = Time.realtimeSinceStartup;
                _finishedUtc = DateTime.UtcNow;
            }
            Phase = AppPhase.Finished;
            SetCameraActive(false);

            var record = _progress.BuildRecord(
                ParticipantId,
                _scenario,
                ElapsedSeconds,
                InspectionSeconds,
                QuizSeconds,
                _perf.Summarize(),
                _sessionTargetName ?? _scenario.targetName,
                _startedUtc.ToString("o"),
                _finishedUtc.ToString("o"));

            try
            {
                SafetyLensSessionStore.Export(record);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                ExportFailed = true;
                Debug.LogError($"[SafetyLens] Falha ao salvar sessao: {ex}");
            }
            ExportSummary =
                $"Inspeção {InspectionSeconds:F0}s · Quiz {QuizSeconds:F0}s · Erros {ErrorCount} · Acertos {QuizCorrectCount}/{QuizCount}";
            StatusMessage = "Sessão concluída. Dados salvos neste aparelho.";
            Debug.Log(
                $"[SafetyLens] Sessao concluida em {ElapsedSeconds:F1}s com {ErrorCount} erro(s).\n" +
                $"JSON: {SafetyLensSessionStore.LastJsonPath}\nCSV: {SafetyLensSessionStore.LastCsvPath}");
        }

        void RegisterError(string detail)
        {
            _progress?.RegisterError();
            StatusMessage = $"{detail} Erros: {ErrorCount}.";
            Debug.LogWarning($"[SafetyLens] Erro #{ErrorCount}: {detail}");
        }

        public void RestartSession()
        {
            if (_contentRoot != null)
            {
                Destroy(_contentRoot);
                _contentRoot = null;
            }

            foreach (var visual in _visuals.Values)
                visual.DestroyMaterials();
            _visuals.Clear();
            TargetFound = false;
            _trackedImage = null;
            _targetUnstable = false;
            ArAvailabilityMessage = null;
            _gate.Reset();
            EnterIdentification();
        }

        public void OpenHistory()
        {
            if (Phase != AppPhase.Identification && Phase != AppPhase.Finished) return;
            _phaseBeforeHistory = Phase;
            ShareStatus = string.Empty;
            Phase = AppPhase.History;
        }

        public void CloseHistory()
        {
            if (Phase != AppPhase.History) return;
            ShareStatus = string.Empty;
            Phase = _phaseBeforeHistory == AppPhase.Finished
                ? AppPhase.Finished
                : AppPhase.Identification;
        }

        public void ShareLastCsv()
        {
            if (ExportFailed || !SafetyLensSessionStore.CanShareLastCsv())
            {
                ShareStatus = "Não há CSV para compartilhar.";
                return;
            }

            var ok = SafetyLensNativeShare.ShareFiles(
                $"SafetyLens — {ParticipantId}",
                SafetyLensSessionStore.LastCsvPath);
            ShareStatus = ok ? "Abrindo compartilhamento…" : "Falha ao abrir o compartilhamento.";
        }

        public void ShareSessionCsv(SafetyLensSessionEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.CsvPath) || !File.Exists(entry.CsvPath))
            {
                ShareStatus = "Arquivo desta sessão não encontrado.";
                return;
            }

            var id = entry.Record?.participantId ?? "sessao";
            var ok = SafetyLensNativeShare.ShareFiles($"SafetyLens — {id}", entry.CsvPath);
            ShareStatus = ok ? "Abrindo compartilhamento…" : "Falha ao abrir o compartilhamento.";
        }

        public bool DeleteSession(SafetyLensSessionEntry entry)
        {
            if (entry == null) return false;
            var ok = SafetyLensSessionStore.DeleteSession(entry);
            ShareStatus = ok ? "Sessão apagada." : "Não foi possível apagar a sessão.";
            return ok;
        }

        public void ShareAllSessionsCsv()
        {
            var sessions = SafetyLensSessionStore.ListSessions();
            if (sessions.Count == 0)
            {
                ShareStatus = "Nenhuma sessão salva ainda.";
                return;
            }

            try
            {
                var path = SafetyLensSessionStore.WriteAggregateCsv(sessions);
                var ok = SafetyLensNativeShare.ShareFiles("SafetyLens — todas as sessões", path);
                ShareStatus = ok
                    ? $"CSV com {sessions.Count} sessão(ões)…"
                    : "Falha ao abrir o compartilhamento.";
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
            {
                ShareStatus = "Não foi possível gerar o CSV completo.";
                Debug.LogError($"[SafetyLens] Aggregate CSV: {ex}");
            }
        }

        void EnterIdentification()
        {
            Phase = AppPhase.Identification;
            ParticipantId = string.Empty;
            _progress = new SafetyLensInspectionProgress(HazardCount);
            _startedAt = -1f;
            _finishedAt = -1f;
            _inspectionEndedAt = -1f;
            _perf.Reset();
            _perfFrames = 0;
            ExportSummary = string.Empty;
            ExportFailed = false;
            ShareStatus = string.Empty;
            QuizSelectedIndex = -1;
            TargetFound = false;
            TargetEverFound = false;
            _sessionTargetName = null;
            IdentificationError = string.Empty;
            FeedbackAt = float.NegativeInfinity;
            StatusMessage = string.Empty;
            SetCameraActive(false);
        }

        void RefreshVisuals()
        {
            if (_scenario == null || _visuals.Count == 0 || _progress == null) return;

            for (var i = 0; i < _scenario.hazards.Length; i++)
            {
                var hazard = _scenario.hazards[i];
                if (!_visuals.TryGetValue(hazard.id, out var visual)) continue;

                var done = _progress.IsHazardDone(i);
                var active = Phase == AppPhase.RiskInspection && !done && i == _progress.ActiveHazardIndex;

                // O objeto mantém sua cor real (placa amarela, capacete amarelo); o estado entra como tint:
                // ativo = cor cheia + anel, pendente = acinzentado, concluído = esverdeado.
                Color tint;
                float scale;
                if (done) { tint = SafetyLensTheme.HazardTintDone; scale = SafetyLensTheme.ArDoneScale; }
                else if (active) { tint = SafetyLensTheme.HazardTintActive; scale = SafetyLensTheme.ArActiveScale; }
                else { tint = SafetyLensTheme.HazardTintIdle; scale = SafetyLensTheme.ArIdleScale; }

                if (visual.Material != null)
                {
                    // Cutout/Standard usam o alpha da textura ou são opacos — tint só no RGB.
                    tint.a = 1f;
                    visual.Material.color = hazard.IsEpi ? SafetyLensTheme.HelmetBase * tint : tint;
                }

                visual.Root.localScale = Vector3.one * scale;
                if (visual.ZoneMaterial != null)
                {
                    // Zona não muda de tamanho (orçamento da letra); o estado entra só na opacidade.
                    var zoneColor = SafetyLensTheme.HazardZone;
                    zoneColor.a = done
                        ? SafetyLensTheme.ArZoneDoneAlpha
                        : active ? SafetyLensTheme.ArZoneActiveAlpha : SafetyLensTheme.ArZoneIdleAlpha;
                    visual.ZoneMaterial.color = zoneColor;
                }
                if (visual.Ring != null)
                {
                    // Anel pulsante colorido = item da vez; anel verde parado = já identificado.
                    visual.Ring.gameObject.SetActive(active || done);
                    visual.RingMaterial.color = done ? SafetyLensTheme.HazardDone : visual.ActiveRingColor;
                    visual.Ring.localScale = Vector3.one * (visual.RingDiameter *
                                                            (done ? SafetyLensTheme.ArDoneRingScale : 1f));
                }
                if (visual.Label != null) visual.Label.SetActive(active);
            }
        }

        /// <summary>Pulso do anel e da zona do item ativo. Âncoras A/B/C: <see cref="AlignContentToMarker"/> em onBeforeRender.</summary>
        void LateUpdate()
        {
            if (Phase != AppPhase.RiskInspection || _visuals.Count == 0) return;

            var current = CurrentHazard;
            if (current == null || !_visuals.TryGetValue(current.id, out var visual)) return;

            if (visual.Ring != null && visual.Ring.gameObject.activeSelf)
            {
                var pulse = SafetyLensAccessibility.ActiveRingPulseScale(Time.realtimeSinceStartup);
                visual.Ring.localScale = Vector3.one * (visual.RingDiameter * pulse);
            }

            if (visual.ZoneMaterial != null)
            {
                // Pulso de opacidade (fica constante com reduce motion).
                var zoneColor = SafetyLensTheme.HazardZone;
                zoneColor.a = SafetyLensAccessibility.ZonePulseAlpha(
                    SafetyLensTheme.ArZoneActiveAlpha, Time.realtimeSinceStartup);
                visual.ZoneMaterial.color = zoneColor;
            }
        }

        void OnDestroy()
        {
            foreach (var visual in _visuals.Values)
                visual.DestroyMaterials();
        }
    }
}
