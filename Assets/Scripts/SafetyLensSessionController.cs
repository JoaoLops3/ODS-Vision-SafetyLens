using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Facade da sessão do experimento: identifica participante, ancora Image Tracking,
    /// conduz inspeção/quiz/telemetria e exporta JSON/CSV offline.
    /// A API pública permanece estável para o IMGUI (<see cref="SafetyLensStatusOverlay"/>).
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public sealed class SafetyLensSessionController : MonoBehaviour
    {
        /// <summary>Limite do ID — alias estável para a overlay e testes.</summary>
        public const int MaxParticipantIdLength = SafetyLensAppConstants.MaxParticipantIdLength;

        /// <summary>Image Target principal — alias estável para o Editor e testes.</summary>
        public const string ExpectedTargetName = SafetyLensAppConstants.ExpectedTargetName;

        readonly SafetyLensSessionClock _clock = new SafetyLensSessionClock();
        readonly SafetyLensStageTimeline _timeline = new SafetyLensStageTimeline();
        readonly SafetyLensSessionFlow _flow = new SafetyLensSessionFlow();
        readonly SafetyLensPerformanceProbe _perf = new SafetyLensPerformanceProbe();
        readonly SafetyLensTelemetrySession _telemetry = new SafetyLensTelemetrySession();
        readonly SafetyLensArContentService _content = new SafetyLensArContentService();

        ARTrackedImageManager _imageManager;
        SafetyLensArTrackingService _tracking;
        SafetyLensScenario _scenario;
        SafetyLensInspectionProgress _progress;
        int _perfFrames;

        [Header("Tracking Limited (P1-6)")]
        [SerializeField] float limitedGraceSeconds = SafetyLensAppConstants.TrackingGraceSeconds;
        [SerializeField] int limitedGraceMinFrames = SafetyLensAppConstants.TrackingGraceMinFrames;

        /// <summary>Fase corrente da jornada. Só o fluxo interno atribui.</summary>
        public AppPhase Phase => _flow.Phase;

        /// <summary>ID sanitizado do participante (RF06).</summary>
        public string ParticipantId { get; private set; } = string.Empty;

        /// <summary>True enquanto o marcador da sessão está em Tracking.</summary>
        public bool TargetFound => _tracking != null && _tracking.TargetFound;

        /// <summary>True depois do primeiro reconhecimento nesta sessão.</summary>
        public bool TargetEverFound => _tracking != null && _tracking.TargetEverFound;

        /// <summary>True durante a graça Limited (P1-6).</summary>
        public bool TargetUnstable => _tracking != null && _tracking.TargetUnstable;

        /// <summary>Aviso persistente em Limited/None dentro do prazo de graça. Null fora de Unstable.</summary>
        public string TargetStatusMessage => _tracking?.TargetStatusMessage;

        /// <summary>True se o ARSession está ativo na hierarquia.</summary>
        public bool IsCameraActive => _tracking != null && _tracking.IsCameraActive;

        /// <summary>True na tela de resultado.</summary>
        public bool IsComplete => Phase == AppPhase.Finished;

        /// <summary>Título do cenário carregado.</summary>
        public string ScenarioTitle => _scenario?.title ?? "SafetyLens";

        /// <summary>Mensagem de status para o card da câmera / resultado.</summary>
        public string StatusMessage { get; private set; } = string.Empty;

        /// <summary>P1-8: sessão AR não sobe (Unsupported / timeout). Null quando OK.</summary>
        public string ArAvailabilityMessage => _tracking?.AvailabilityMessage;

        /// <summary>Erro de ID vazio na identificação.</summary>
        public string IdentificationError { get; private set; } = string.Empty;

        /// <summary>Texto do toast (seleção, confirmação, erro de toque).</summary>
        public string FeedbackText { get; private set; } = string.Empty;

        /// <summary>True se o toast é de erro.</summary>
        public bool FeedbackIsError { get; private set; }

        /// <summary>Instante do último toast (realtimeSinceStartup).</summary>
        public float FeedbackAt { get; private set; } = float.NegativeInfinity;

        /// <summary>Índice da alternativa escolhida na pergunta atual. -1 se ainda não respondeu.</summary>
        public int QuizSelectedIndex { get; private set; } = -1;

        /// <summary>Resumo curto gravado após o export.</summary>
        public string ExportSummary { get; private set; } = string.Empty;

        /// <summary>True se o JSON/CSV não pôde ser gravado.</summary>
        public bool ExportFailed { get; private set; }

        /// <summary>Status do share sheet / exclusão no histórico.</summary>
        public string ShareStatus { get; private set; } = string.Empty;

        /// <summary>Erros de inspeção e quiz (núcleo da pesquisa). Fonte: progresso.</summary>
        public int ErrorCount => _progress?.ErrorCount ?? 0;

        /// <summary>Quantidade de hazards no cenário.</summary>
        public int HazardCount => _scenario?.hazards?.Length ?? 0;

        /// <summary>Hazards já confirmados (AR + botão).</summary>
        public int HazardConfirmedCount => _progress?.HazardConfirmedCount ?? 0;

        /// <summary>Fração confirmada / total (0 se não há hazards).</summary>
        public float HazardProgress => HazardCount == 0 ? 0f : HazardConfirmedCount / (float)HazardCount;

        /// <summary>True após toque no overlay AR do item ativo — habilita “Confirmar item”.</summary>
        public bool CurrentHazardArSelected => _progress != null && _progress.CurrentHazardArSelected;

        /// <summary>RF03: confirmar só com marcador visível + seleção por toque no overlay.</summary>
        public bool CanConfirmCurrentHazard =>
            Phase == AppPhase.RiskInspection
            && TargetFound
            && !TargetUnstable
            && CurrentHazard != null
            && CurrentHazardArSelected;

        /// <summary>Hazard da vez, ou null fora da inspeção / sem progresso.</summary>
        public SafetyLensHazard CurrentHazard
        {
            get
            {
                if (_progress == null || HazardCount == 0) return null;
                var i = _progress.ActiveHazardIndex;
                return i < 0 || i >= HazardCount ? null : _scenario.hazards[i];
            }
        }

        /// <summary>Quantidade de perguntas do quiz.</summary>
        public int QuizCount => _scenario?.quiz?.Length ?? 0;

        /// <summary>Índice da pergunta ativa.</summary>
        public int ActiveQuizIndex => _progress?.ActiveQuizIndex ?? 0;

        /// <summary>Pergunta corrente, ou null se o índice estiver fora.</summary>
        public SafetyLensQuizQuestion CurrentQuiz
        {
            get
            {
                if (_progress == null || QuizCount == 0) return null;
                var i = _progress.ActiveQuizIndex;
                return i < 0 || i >= QuizCount ? null : _scenario.quiz[i];
            }
        }

        /// <summary>Acertos do quiz.</summary>
        public int QuizCorrectCount => _progress?.QuizCorrect ?? 0;

        /// <summary>RF12: cenário com telemetria habilitada (última pergunta do quiz abre a leitura).</summary>
        public bool TelemetryEnabled => SafetyLensTelemetry.IsEnabled(_scenario);

        /// <summary>Ficha do equipamento do cenário. Null só se o JSON estiver inválido (boot já valida).</summary>
        public SafetyLensEquipment Equipment => _scenario?.equipment;

        /// <summary>CTA da ficha: só com tracking estável (P1-6).</summary>
        public bool CanConfirmEquipmentBrief =>
            Phase == AppPhase.EquipmentBrief && TargetFound && !TargetUnstable;

        /// <summary>RF12: nome do equipamento no painel de telemetria.</summary>
        public string TelemetryEquipmentName =>
            !string.IsNullOrWhiteSpace(_scenario?.equipment?.name)
                ? _scenario.equipment.name
                : string.IsNullOrWhiteSpace(_scenario?.telemetry?.equipmentName)
                    ? SafetyLensAppConstants.DefaultEquipmentName
                    : _scenario.telemetry.equipmentName;

        /// <summary>Frase Ideal/Cuidado/Perigoso da amostra RF12 corrente.</summary>
        public string CurrentTelemetryNarrativeCopy =>
            SafetyLensTelemetry.NarrativeCopy(_scenario?.telemetry, _telemetry.Status);

        /// <summary>Amostra RF12 na tela. Null se a fase não rodou.</summary>
        public SafetyLensTelemetrySample CurrentTelemetrySample => _telemetry.Sample;

        /// <summary>Status RF12 da amostra corrente.</summary>
        public SafetyLensTelemetryStatus CurrentTelemetryStatus => _telemetry.Status;

        /// <summary>Horário local da última atualização RF12.</summary>
        public DateTime CurrentTelemetryLocalTime => _telemetry.SampleLocalTime;

        /// <summary>Segundos desde o início da fase RF12. Zero fora de Telemetry.</summary>
        public float TelemetryPhaseSeconds =>
            _telemetry.PhaseSeconds(Time.realtimeSinceStartup, Phase == AppPhase.Telemetry);

        /// <summary>RF04: do primeiro tracking à última resposta do quiz.</summary>
        public float ElapsedSeconds => _clock.ElapsedSeconds(Time.realtimeSinceStartup);

        /// <summary>RF04: do reconhecimento do marcador à confirmação do último risco.</summary>
        public float InspectionSeconds => _clock.InspectionSeconds(Time.realtimeSinceStartup);

        /// <summary>RF04: tempo entre o fim da inspeção e a última resposta do quiz.</summary>
        public float QuizSeconds => _clock.QuizSeconds(Time.realtimeSinceStartup);

        void Awake()
        {
            Application.targetFrameRate = SafetyLensAppConstants.TargetFrameRate;
            _imageManager = GetComponent<ARTrackedImageManager>();
            var arSession = FindFirstObjectByType<ARSession>(FindObjectsInactive.Include);
            if (arSession == null)
                throw new InvalidOperationException("ARSession nao encontrado na cena.");

            _tracking = new SafetyLensArTrackingService(arSession, this);
            _tracking.ConfigureGate(limitedGraceSeconds, limitedGraceMinFrames);
            _scenario = SafetyLensScenarioLoader.LoadFromResources(ExpectedTargetName);
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

        /// <summary>Mantém só caracteres seguros para nome de arquivo e CSV.</summary>
        public static string SanitizeParticipantId(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var sb = new StringBuilder(Mathf.Min(raw.Length, MaxParticipantIdLength));
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
            Application.onBeforeRender += AlignContentToMarker;
        }

        void OnDisable()
        {
            _imageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
            Application.onBeforeRender -= AlignContentToMarker;
        }

        /// <summary>Abre a câmera e entra em Scanning após validar o ID.</summary>
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
            _tracking.ResetLock();
            _tracking.ConfigureGate(limitedGraceSeconds, limitedGraceMinFrames);
            QuizSelectedIndex = -1;
            FeedbackAt = float.NegativeInfinity;
            _progress = new SafetyLensInspectionProgress(HazardCount);
            _clock.Reset();
            _timeline.Reset();
            _telemetry.Reset();
            _perf.Reset();
            _perfFrames = 0;
            ExportSummary = string.Empty;
            ExportFailed = false;
            if (!_flow.TryEnterScanning()) return;
            StatusMessage = "Enquadre a placa do equipamento ou o QR Code do treinamento.";
            _tracking.SetCameraActive(true);
            _content.Refresh(_scenario, _progress, Phase);
            Debug.Log($"[SafetyLens] Sessao iniciada para participante {ParticipantId}.");
        }

        void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            if (Phase == AppPhase.Identification) return;

            foreach (var image in args.added) HandleTrackedImage(image);
            foreach (var image in args.updated) HandleTrackedImage(image);
            foreach (var removed in args.removed)
                _tracking.ClearTrackedImageIf(removed.Key);
        }

        void HandleTrackedImage(ARTrackedImage image)
        {
            if (!_tracking.TryHandleImage(image, _scenario)) return;
            OnMarkerTracking();
        }

        void OnMarkerTracking()
        {
            var image = _tracking.TrackedImage;
            if (image == null) return;

            _clock.MarkInspectionStarted(Time.realtimeSinceStartup, DateTime.UtcNow);
            _timeline.MarkMarkerFound(Time.realtimeSinceStartup);
            _content.Ensure(_scenario);
            _content.SetContentActive(
                Phase == AppPhase.Scanning
                || Phase == AppPhase.EquipmentBrief
                || Phase == AppPhase.RiskInspection);
            AlignContentToMarker();
            _content.Refresh(_scenario, _progress, Phase);

            if (_flow.TryEnterEquipmentBrief())
            {
                _tracking.MarkEverFound();
                ShowFeedback("Marcador reconhecido", false);
                StatusMessage = "Leia a ficha do equipamento e inicie a inspeção.";
            }
        }

        void OnMarkerLost()
        {
            _content.SetContentActive(false);
            _progress?.ClearArSelection();
            if (Phase == AppPhase.RiskInspection
                || Phase == AppPhase.Scanning
                || Phase == AppPhase.EquipmentBrief)
                StatusMessage = "Marcador perdido. Aponte a câmera para a placa de novo.";
        }

        /// <summary>Fecha a ficha e entra na inspeção. No-op sem tracking estável.</summary>
        public void ConfirmEquipmentBrief()
        {
            if (!CanConfirmEquipmentBrief) return;
            _timeline.MarkBriefDone(Time.realtimeSinceStartup);
            if (!_flow.TryEnterRiskInspection()) return;
            StatusMessage = string.Empty;
            _content.SetContentActive(true);
            _content.Refresh(_scenario, _progress, Phase);
            ShowFeedback("Inspeção iniciada", false);
        }

        void AlignContentToMarker()
        {
            var image = _tracking?.TrackedImage;
            if (image == null) return;
            _content.AlignToMarker(
                image.transform,
                image.size,
                _scenario,
                _tracking.SessionTargetName,
                TargetFound);
        }

        void Update()
        {
            if (Phase == AppPhase.Telemetry)
                _telemetry.Refresh(_scenario.telemetry, TelemetryPhaseSeconds, force: false);

            if (Phase == AppPhase.RiskInspection)
            {
                _perf.AddFrame(Time.unscaledDeltaTime);
                if (++_perfFrames % SafetyLensAppConstants.MemorySampleEveryFrames == 0)
                    _perf.RecordMemoryBytes(Profiler.GetTotalAllocatedMemoryLong());
            }

            _tracking.UpdateVisibility(Phase, Time.unscaledTime, out var becameFound, out var becameLost);
            if (becameFound)
                OnMarkerTracking();
            else if (becameLost)
                OnMarkerLost();

            if ((Phase == AppPhase.Scanning
                    || Phase == AppPhase.EquipmentBrief
                    || Phase == AppPhase.RiskInspection) && IsCameraActive
                && (ARSession.state == ARSessionState.Unsupported
                    || ARSession.state == ARSessionState.Ready
                    || ARSession.state == ARSessionState.SessionTracking
                    || ArAvailabilityMessage != null))
            {
                _tracking.RefreshAvailability();
                if (ArAvailabilityMessage != null)
                    StatusMessage = ArAvailabilityMessage;
            }

            if (Phase != AppPhase.RiskInspection || !TargetFound || Input.touchCount != 1) return;

            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Ended || SafetyLensStatusOverlay.IsTouchOverOverlay(touch.position))
                return;

            if (TargetUnstable) return;

            var camera = _content.ResolveArCamera();
            if (camera == null) return;

            var tag = _content.PickHazard(camera, touch.position);
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

        /// <summary>RF03 — primeiro passo: toque no overlay AR do item ativo. Só depois o botão confirma.</summary>
        void SelectCurrentHazardFromAr()
        {
            if (Phase != AppPhase.RiskInspection || !TargetFound || CurrentHazard == null) return;
            if (!_progress.SelectCurrentHazardFromAr()) return;

            ShowFeedback($"Selecionado: {CurrentHazard.label}. Confirme abaixo.", false);
            Debug.Log($"[SafetyLens] Hazard selecionado via AR: {CurrentHazard.id}");
        }

        /// <summary>Confirma o item ativo. Sem seleção AR o botão não avança (RF03).</summary>
        public void ConfirmCurrentHazard()
        {
            if (!CanConfirmCurrentHazard) return;

            var confirmedLabel = CurrentHazard.label;
            var confirmedId = CurrentHazard.id;
            var inspectionDone = _progress.ConfirmCurrentHazard(HazardCount);
            Debug.Log($"[SafetyLens] Hazard confirmado (AR+botão): {confirmedId}");

            if (inspectionDone)
            {
                _clock.MarkInspectionEnded(Time.realtimeSinceStartup);
                _timeline.MarkInspectionEnded(Time.realtimeSinceStartup);
                _perf.RecordMemoryBytes(Profiler.GetTotalAllocatedMemoryLong());
                _tracking.ResetGate();
                _flow.TryEnterQuiz();
                QuizSelectedIndex = -1;
                _content.SetContentActive(false);
                _tracking.SetCameraActive(false);
                ShowFeedback("Todos os itens identificados", false);
            }
            else
                ShowFeedback($"{confirmedLabel} identificado", false);

            _content.Refresh(_scenario, _progress, Phase);
        }

        /// <summary>
        /// Registra a resposta e a revela (certa/errada) antes de avançar. Avanço via <see cref="NextQuiz"/>.
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

            if (isLast)
            {
                _clock.MarkQuizFinished(Time.realtimeSinceStartup, DateTime.UtcNow);
                _timeline.MarkQuizEnded(Time.realtimeSinceStartup);
            }
        }

        /// <summary>Avança o quiz. Na última pergunta abre telemetria (RF12) ou exporta.</summary>
        public void NextQuiz()
        {
            if (Phase != AppPhase.Quiz || QuizSelectedIndex < 0) return;

            QuizSelectedIndex = -1;
            if (!_progress.NextQuiz(QuizCount)) return;

            if (SafetyLensTelemetry.IsEnabled(_scenario))
                EnterTelemetry();
            else
                FinishSession();
        }

        void EnterTelemetry()
        {
            if (!_flow.TryEnterTelemetry()) return;
            _telemetry.Begin(Time.realtimeSinceStartup);
            _content.SetContentActive(false);
            _tracking.SetCameraActive(true);
            _telemetry.Refresh(_scenario.telemetry, TelemetryPhaseSeconds, force: true);
            StatusMessage = "Leitura do equipamento. Observe a mudança de status e conclua.";
            Debug.Log("[SafetyLens] Fase de telemetria iniciada (RF12).");
        }

        /// <summary>RF12 — botão Concluir da leitura: exporta com a amostra que estava na tela.</summary>
        public void CompleteTelemetry()
        {
            if (Phase != AppPhase.Telemetry) return;
            _telemetry.Refresh(_scenario.telemetry, TelemetryPhaseSeconds, force: false);
            _timeline.MarkTelemetryEnded(Time.realtimeSinceStartup);
            FinishSession();
        }

        void FinishSession()
        {
            _clock.EnsureFinished(Time.realtimeSinceStartup, DateTime.UtcNow);
            _timeline.MarkFinished(Time.realtimeSinceStartup);
            _flow.TryEnterFinished();
            _tracking.SetCameraActive(false);
            _telemetry.StopPhaseClock();

            var ranTelemetry = _telemetry.HasSample;
            var record = _progress.BuildRecord(
                ParticipantId,
                _scenario,
                ElapsedSeconds,
                InspectionSeconds,
                QuizSeconds,
                _perf.Summarize(),
                _tracking.SessionTargetName ?? _scenario.targetName,
                _clock.StartedUtc.ToString("o"),
                _clock.FinishedUtc.ToString("o"),
                ranTelemetry ? SafetyLensTelemetry.Label(_telemetry.Status) : string.Empty,
                ranTelemetry ? _telemetry.Sample.temperature : 0f,
                ranTelemetry ? _telemetry.Sample.pressure : 0f,
                ranTelemetry ? _telemetry.Sample.rpm : 0f,
                _timeline.BriefSeconds,
                _timeline.InspectionEndedAtSeconds,
                _timeline.QuizEndedAtSeconds,
                _timeline.TelemetryEndedAtSeconds,
                _timeline.FinishedAtSeconds);

            try
            {
                SafetyLensSessionStore.Export(record);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
            {
                ExportFailed = true;
                Debug.LogError($"[SafetyLens] Falha ao salvar sessao: {ex.Message}");
            }

            ExportSummary =
                $"Inspeção {InspectionSeconds:F0}s · Quiz {QuizSeconds:F0}s · Erros {ErrorCount} · Acertos {QuizCorrectCount}/{QuizCount}";
            StatusMessage = ExportFailed
                ? "Sessão concluída. Não foi possível salvar os dados neste aparelho."
                : "Sessão concluída. Dados salvos neste aparelho.";
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

        /// <summary>Destroi overlays 3D e volta à identificação.</summary>
        public void RestartSession()
        {
            _content.DestroyContent();
            _tracking.ResetLock();
            _tracking.ConfigureGate(limitedGraceSeconds, limitedGraceMinFrames);
            EnterIdentification();
        }

        /// <summary>Abre o histórico a partir da identificação ou do resultado.</summary>
        public void OpenHistory()
        {
            if (!_flow.TryOpenHistory()) return;
            ShareStatus = string.Empty;
        }

        /// <summary>Fecha o histórico restaurando Identification ou Finished.</summary>
        public void CloseHistory()
        {
            if (!_flow.TryCloseHistory()) return;
            ShareStatus = string.Empty;
        }

        /// <summary>Abre o share sheet do último CSV exportado.</summary>
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

        /// <summary>Compartilha o CSV de uma entrada do histórico.</summary>
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

        /// <summary>Apaga o par JSON/CSV da sessão no aparelho.</summary>
        public bool DeleteSession(SafetyLensSessionEntry entry)
        {
            if (entry == null) return false;
            var ok = SafetyLensSessionStore.DeleteSession(entry);
            ShareStatus = ok ? "Sessão apagada." : "Não foi possível apagar a sessão.";
            return ok;
        }

        /// <summary>Gera e compartilha um CSV agregado de todas as sessões.</summary>
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
                Debug.LogError($"[SafetyLens] Aggregate CSV: {ex.Message}");
            }
        }

        void EnterIdentification()
        {
            _flow.EnterIdentification();
            ParticipantId = string.Empty;
            _progress = new SafetyLensInspectionProgress(HazardCount);
            _clock.Reset();
            _timeline.Reset();
            _telemetry.Reset();
            _perf.Reset();
            _perfFrames = 0;
            ExportSummary = string.Empty;
            ExportFailed = false;
            ShareStatus = string.Empty;
            QuizSelectedIndex = -1;
            IdentificationError = string.Empty;
            FeedbackAt = float.NegativeInfinity;
            StatusMessage = string.Empty;
            _tracking.SetCameraActive(false);
        }

        void LateUpdate()
        {
            if (Phase != AppPhase.RiskInspection || !_content.HasContent) return;
            _content.PulseActive(CurrentHazard, Time.realtimeSinceStartup);
        }

        void OnDestroy()
        {
            _content.DestroyContent();
        }
    }
}
