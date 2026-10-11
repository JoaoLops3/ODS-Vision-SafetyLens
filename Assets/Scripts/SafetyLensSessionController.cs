using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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
        readonly List<string> _quizAnswers = new();

        public const int MaxParticipantIdLength = 24;

        ARTrackedImageManager _imageManager;
        ARSession _arSession;
        SafetyLensScenario _scenario;
        GameObject _contentRoot;
        ARTrackedImage _trackedImage;
        bool[] _hazardDone;
        int _activeHazardIndex;
        int _activeQuizIndex;
        int _quizCorrect;
        float _startedAt = -1f;
        float _finishedAt = -1f;
        DateTime _startedUtc;
        DateTime _finishedUtc;

        public AppPhase Phase { get; private set; } = AppPhase.Identification;
        public string ParticipantId { get; private set; } = string.Empty;
        public bool TargetFound { get; private set; }
        public bool TargetEverFound { get; private set; }
        public bool IsCameraActive => _arSession != null && _arSession.gameObject.activeSelf;
        public bool IsComplete => Phase == AppPhase.Finished;
        public string ScenarioTitle => _scenario?.title ?? "SafetyLens";
        public string StatusMessage { get; private set; } = string.Empty;
        public string IdentificationError { get; private set; } = string.Empty;
        public string FeedbackText { get; private set; } = string.Empty;
        public bool FeedbackIsError { get; private set; }
        public float FeedbackAt { get; private set; } = float.NegativeInfinity;
        public int QuizSelectedIndex { get; private set; } = -1;
        public string ExportSummary { get; private set; } = string.Empty;
        public bool ExportFailed { get; private set; }
        public string ShareStatus { get; private set; } = string.Empty;

        AppPhase _phaseBeforeHistory = AppPhase.Identification;
        public int ErrorCount { get; private set; }
        public int HazardCount => _scenario?.hazards?.Length ?? 0;
        public int HazardConfirmedCount
        {
            get
            {
                var n = 0;
                if (_hazardDone != null) foreach (var done in _hazardDone) if (done) n++;
                return n;
            }
        }
        public float HazardProgress => HazardCount == 0 ? 0f : HazardConfirmedCount / (float)HazardCount;
        public SafetyLensHazard CurrentHazard =>
            HazardCount == 0 || _activeHazardIndex < 0 || _activeHazardIndex >= HazardCount
                ? null
                : _scenario.hazards[_activeHazardIndex];
        public int QuizCount => _scenario?.quiz?.Length ?? 0;
        public int ActiveQuizIndex => _activeQuizIndex;
        public SafetyLensQuizQuestion CurrentQuiz =>
            QuizCount == 0 || _activeQuizIndex < 0 || _activeQuizIndex >= QuizCount
                ? null
                : _scenario.quiz[_activeQuizIndex];
        public int QuizCorrectCount => _quizCorrect;
        public float ElapsedSeconds =>
            _startedAt < 0f
                ? 0f
                : Mathf.Max(0f, (_finishedAt >= 0f ? _finishedAt : Time.realtimeSinceStartup) - _startedAt);

        void Awake()
        {
            _imageManager = GetComponent<ARTrackedImageManager>();
            _arSession = FindFirstObjectByType<ARSession>(FindObjectsInactive.Include);
            if (_arSession == null)
                throw new InvalidOperationException("ARSession nao encontrado na cena.");
            LoadScenario();
            PrewarmPictograms();
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
        /// fora das etapas de RA: evita o indicador de câmera ativo sem motivo e poupa bateria.
        /// O GameObject do ARSession começa inativo na cena (ver SafetyLensProjectSetup).
        /// </summary>
        void SetCameraActive(bool active)
        {
            if (_arSession.gameObject.activeSelf != active)
                _arSession.gameObject.SetActive(active);
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

        void OnEnable() => _imageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        void OnDisable() => _imageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);

        void LoadScenario()
        {
            var asset = Resources.Load<TextAsset>("safetylens_scenario");
            if (asset == null)
                throw new InvalidOperationException("Assets/Resources/safetylens_scenario.json nao foi encontrado.");

            _scenario = JsonUtility.FromJson<SafetyLensScenario>(asset.text);
            if (_scenario?.hazards == null || _scenario.hazards.Length == 0)
                throw new InvalidOperationException("Cenario SafetyLens sem hazards.");
            if (string.IsNullOrWhiteSpace(_scenario.targetName))
                throw new InvalidOperationException("Cenario SafetyLens sem targetName.");
            if (_scenario.quiz == null || _scenario.quiz.Length == 0)
                throw new InvalidOperationException("Cenario SafetyLens sem quiz.");
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
            ParticipantId = trimmed;
            TargetFound = false;
            TargetEverFound = false;
            QuizSelectedIndex = -1;
            FeedbackAt = float.NegativeInfinity;
            ErrorCount = 0;
            _quizCorrect = 0;
            _quizAnswers.Clear();
            _activeHazardIndex = 0;
            _activeQuizIndex = 0;
            _hazardDone = new bool[HazardCount];
            _startedAt = -1f;
            _finishedAt = -1f;
            ExportSummary = string.Empty;
            ExportFailed = false;
            Phase = AppPhase.Scanning;
            StatusMessage = "Enquadre a placa do equipamento.";
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
                    SetTargetFound(false, null);
            }
        }

        void HandleTrackedImage(ARTrackedImage image)
        {
            if (image.referenceImage.name != _scenario.targetName) return;
            var tracking = image.trackingState == TrackingState.Tracking;
            SetTargetFound(tracking, image);
        }

        void SetTargetFound(bool found, ARTrackedImage image)
        {
            TargetFound = found;
            if (found && image != null)
            {
                _trackedImage = image;
                // RF04: cronometro inicia no reconhecimento do marcador.
                if (_startedAt < 0f)
                {
                    _startedAt = Time.realtimeSinceStartup;
                    _startedUtc = DateTime.UtcNow;
                }

                EnsureArContent(image.transform);
                _contentRoot.SetActive(true);

                if (Phase == AppPhase.Scanning)
                {
                    Phase = AppPhase.RiskInspection;
                    TargetEverFound = true;
                    ShowFeedback("Marcador reconhecido", false);
                }
            }
            else
            {
                if (_contentRoot != null) _contentRoot.SetActive(false);
                if (Phase == AppPhase.RiskInspection || Phase == AppPhase.Scanning)
                    StatusMessage = "Marcador perdido. Aponte a câmera para a placa de novo.";
            }
        }

        void EnsureArContent(Transform target)
        {
            var created = false;
            if (_contentRoot == null)
            {
                // Criado na origem sem rotação: os visuais medem seus bounds em espaço local.
                _contentRoot = new GameObject("SafetyLens_ArContent");
                foreach (var hazard in _scenario.hazards)
                    _visuals[hazard.id] = SafetyLensArVisuals.Create(hazard, _contentRoot.transform);
                created = true;
            }

            if (_contentRoot.transform.parent != target)
                _contentRoot.transform.SetParent(target, false);
            _contentRoot.transform.localPosition = Vector3.zero;
            // Sem realinhamento por gravidade: o conteúdo usa os eixos nativos do próprio
            // ARTrackedImage (Y = normal, X = direita da imagem, Z = topo da imagem, conforme a
            // Reference Image Library). Reorientar por gravidade era o que deixava a fileira torta
            // sempre que o celular estava um pouco inclinado ao escanear.
            _contentRoot.transform.localRotation = Quaternion.identity;

            if (created)
                MountVisuals(target);
            RefreshVisuals();
        }

        /// <summary>
        /// Decide só a pose do capacete (deitado se o marcador está na mesa, em pé se está na
        /// parede/tela) comparando a normal do marcador com a gravidade. Isso NÃO gira o
        /// _contentRoot — cada item continua no referencial nativo do marcador.
        /// </summary>
        void MountVisuals(Transform target)
        {
            var isVertical = Mathf.Abs(Vector3.Dot(target.up, Vector3.up)) < SafetyLensTheme.ArVerticalMarkerDot;
            foreach (var visual in _visuals.Values)
                SafetyLensArVisuals.ApplyMounting(visual, isVertical);
        }

        void Update()
        {
            if (Phase != AppPhase.RiskInspection || !TargetFound || Input.touchCount != 1) return;

            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Ended || SafetyLensStatusOverlay.IsTouchOverOverlay(touch.position))
                return;

            var camera = Camera.main;
            if (camera == null) return;

            var tag = PickHazard(camera, touch.position);
            if (tag == null || CurrentHazard == null) return;

            if (tag.hazardId == CurrentHazard.id)
                ConfirmCurrentHazard();
            else
            {
                RegisterError($"Selecao incorreta: {tag.hazardId}. Esperado: {CurrentHazard.id}.");
                ShowFeedback($"Não é esse. Procure: {CurrentHazard.label}", true);
            }
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
            if (Phase != AppPhase.RiskInspection || !TargetFound || CurrentHazard == null) return;

            _hazardDone[_activeHazardIndex] = true;
            var confirmedLabel = CurrentHazard.label;
            Debug.Log($"[SafetyLens] Hazard confirmado: {CurrentHazard.id}");

            if (_activeHazardIndex >= HazardCount - 1)
            {
                Phase = AppPhase.Quiz;
                _activeQuizIndex = 0;
                QuizSelectedIndex = -1;
                if (_contentRoot != null) _contentRoot.SetActive(false);
                SetCameraActive(false);
                ShowFeedback("Todos os itens identificados", false);
            }
            else
            {
                _activeHazardIndex++;
                ShowFeedback($"{confirmedLabel} identificado", false);
            }

            RefreshVisuals();
        }

        /// <summary>
        /// Registra a resposta e a revela (certa/errada) antes de avançar: o quiz é de fixação,
        /// então o participante precisa ver a alternativa correta. Avanço via <see cref="NextQuiz"/>.
        /// </summary>
        public void AnswerQuiz(int optionIndex)
        {
            if (Phase != AppPhase.Quiz || CurrentQuiz == null || QuizSelectedIndex >= 0) return;
            if (optionIndex < 0 || CurrentQuiz.options == null || optionIndex >= CurrentQuiz.options.Length)
                return;

            QuizSelectedIndex = optionIndex;
            var selected = CurrentQuiz.options[optionIndex];
            var correct = optionIndex == CurrentQuiz.correctIndex;
            _quizAnswers.Add($"Q{CurrentQuiz.id}:{selected}:{(correct ? "OK" : "ERR")}");

            if (correct)
                _quizCorrect++;
            else
                RegisterError($"Quiz Q{CurrentQuiz.id}: resposta incorreta ({selected}).");

            // RF04: o tempo da tarefa termina na última resposta, não no tempo de leitura do resultado.
            if (_activeQuizIndex >= QuizCount - 1)
            {
                _finishedAt = Time.realtimeSinceStartup;
                _finishedUtc = DateTime.UtcNow;
            }
        }

        public void NextQuiz()
        {
            if (Phase != AppPhase.Quiz || QuizSelectedIndex < 0) return;

            QuizSelectedIndex = -1;
            if (_activeQuizIndex >= QuizCount - 1)
                FinishSession();
            else
                _activeQuizIndex++;
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

            var record = new SafetyLensSessionRecord
            {
                participantId = ParticipantId,
                scenarioId = _scenario.id,
                scenarioTitle = _scenario.title,
                elapsedSeconds = ElapsedSeconds,
                errorCount = ErrorCount,
                hazardsConfirmed = HazardConfirmedCount,
                hazardsTotal = HazardCount,
                quizCorrect = _quizCorrect,
                quizTotal = QuizCount,
                quizAnswers = _quizAnswers.ToArray(),
                startedAtIso = _startedUtc.ToString("o"),
                finishedAtIso = _finishedUtc.ToString("o")
            };

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
                $"Tempo {ElapsedSeconds:F0}s · Erros {ErrorCount} · Quiz {_quizCorrect}/{QuizCount}";
            StatusMessage = "Sessão concluída. Dados salvos neste aparelho.";
            Debug.Log(
                $"[SafetyLens] Sessao concluida em {ElapsedSeconds:F1}s com {ErrorCount} erro(s).\n" +
                $"JSON: {SafetyLensSessionStore.LastJsonPath}\nCSV: {SafetyLensSessionStore.LastCsvPath}");
        }

        void RegisterError(string detail)
        {
            ErrorCount++;
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
            ErrorCount = 0;
            _quizCorrect = 0;
            _quizAnswers.Clear();
            _activeHazardIndex = 0;
            _activeQuizIndex = 0;
            _hazardDone = new bool[HazardCount];
            _startedAt = -1f;
            _finishedAt = -1f;
            ExportSummary = string.Empty;
            ExportFailed = false;
            ShareStatus = string.Empty;
            QuizSelectedIndex = -1;
            TargetFound = false;
            TargetEverFound = false;
            IdentificationError = string.Empty;
            FeedbackAt = float.NegativeInfinity;
            StatusMessage = string.Empty;
            SetCameraActive(false);
        }

        void RefreshVisuals()
        {
            if (_scenario == null || _visuals.Count == 0) return;

            for (var i = 0; i < _scenario.hazards.Length; i++)
            {
                var hazard = _scenario.hazards[i];
                if (!_visuals.TryGetValue(hazard.id, out var visual)) continue;

                var done = _hazardDone != null && i < _hazardDone.Length && _hazardDone[i];
                var active = Phase == AppPhase.RiskInspection && !done && i == _activeHazardIndex;

                // O objeto mantém sua cor real (placa amarela, capacete amarelo); o estado entra como tint:
                // ativo = cor cheia + anel, pendente = acinzentado, concluído = esverdeado.
                Color tint;
                float scale;
                if (done) { tint = SafetyLensTheme.HazardTintDone; scale = SafetyLensTheme.ArDoneScale; }
                else if (active) { tint = SafetyLensTheme.HazardTintActive; scale = SafetyLensTheme.ArActiveScale; }
                else { tint = SafetyLensTheme.HazardTintIdle; scale = SafetyLensTheme.ArIdleScale; }

                if (visual.Material != null)
                {
                    // Standard (capacete) não é transparente: ignora o alpha do tint.
                    if (hazard.IsEpi) tint.a = 1f;
                    visual.Material.color = hazard.IsEpi ? SafetyLensTheme.HelmetBase * tint : tint;
                }

                visual.Root.localScale = Vector3.one * scale;
                if (visual.Ring != null)
                {
                    // Anel pulsante colorido = item da vez; anel verde parado = já identificado.
                    visual.Ring.gameObject.SetActive(active || done);
                    visual.RingMaterial.color = done ? SafetyLensTheme.HazardDone : visual.ActiveRingColor;
                    visual.Ring.localScale = Vector3.one * (SafetyLensTheme.ArRingDiameter *
                                                            (done ? SafetyLensTheme.ArDoneRingScale : 1f));
                }
                if (visual.Label != null) visual.Label.SetActive(active);
            }
        }

        /// <summary>Pulso discreto no anel do item ativo: diz "é este" sem competir com o pictograma.</summary>
        void LateUpdate()
        {
            if (Phase != AppPhase.RiskInspection || _visuals.Count == 0) return;
            var current = CurrentHazard;
            if (current == null || !_visuals.TryGetValue(current.id, out var visual) || visual.Ring == null) return;
            if (!visual.Ring.gameObject.activeSelf) return;

            var pulse = 1f + SafetyLensTheme.ArPulseAmplitude *
                (0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * SafetyLensTheme.ArPulseSpeed * Mathf.PI));
            visual.Ring.localScale = Vector3.one * (SafetyLensTheme.ArRingDiameter * pulse);
        }

        void OnDestroy()
        {
            foreach (var visual in _visuals.Values)
                visual.DestroyMaterials();
        }
    }
}
