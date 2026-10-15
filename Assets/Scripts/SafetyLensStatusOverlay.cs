using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using T = Facens.SafetyLens.SafetyLensTheme;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Interface do SafetyLens em IMGUI.
    /// Todo layout é escrito em pontos lógicos (T.ReferenceWidth) e convertido para pixels via
    /// <see cref="U"/>; as fontes são criadas já no tamanho em pixels para renderizar nítidas em telas 3x.
    /// Alturas de texto são sempre medidas (CalcHeight) — nada de altura fixa que corta conteúdo.
    /// </summary>
    public sealed class SafetyLensStatusOverlay : MonoBehaviour
    {
        const string ParticipantFieldName = "participant-id";
        const float CompactHeightThreshold = 700f;

        static readonly List<Rect> BlockingRects = new();
        static bool _blocksWholeScreen;
        static readonly GUIContent Content = new();

        readonly Dictionary<(GUIStyle, Color, TextAnchor), GUIStyle> _variants = new();
        readonly List<SafetyLensSessionEntry> _history = new();

        SafetyLensSessionController _controller;
        string _participantInput = string.Empty;
        float _scale = 1f;
        int _stylesForWidth = -1;
        AppPhase _lastPhase = AppPhase.Identification;
        Vector2 _historyScroll;
        SafetyLensSessionEntry _pendingDelete;

        GUIStyle _display, _title, _bodyLarge, _body, _bodyStrong, _caption, _captionStrong, _overline;
        GUIStyle _button, _field, _score, _metricValue, _badge, _glyph, _hitArea;

        void Start()
        {
            _controller = FindFirstObjectByType<SafetyLensSessionController>();
            TouchScreenKeyboard.hideInput = true;
        }

        float U(float points) => points * _scale;

        void EnsureStyles()
        {
            if (_stylesForWidth == Screen.width && _display != null) return;
            _stylesForWidth = Screen.width;
            _variants.Clear();
            // O lado curto define a escala: em paisagem a UI não cresce além do retrato.
            _scale = Mathf.Min(Screen.width, Screen.height) / T.ReferenceWidth;

            _display = Text(T.FontDisplay, FontStyle.Bold, T.Ink);
            _title = Text(T.FontTitle, FontStyle.Bold, T.Ink);
            _bodyLarge = Text(T.FontBodyLarge, FontStyle.Normal, T.InkSoft);
            _body = Text(T.FontBody, FontStyle.Normal, T.InkSoft);
            _bodyStrong = Text(T.FontBody, FontStyle.Bold, T.Ink);
            _caption = Text(T.FontCaption, FontStyle.Normal, T.InkFaint);
            _captionStrong = Text(T.FontCaption, FontStyle.Bold, T.Ink);
            _overline = Text(T.FontOverline, FontStyle.Bold, T.InkFaint);
            _score = Text(T.FontScore, FontStyle.Bold, T.Ink);
            _score.alignment = TextAnchor.MiddleCenter;
            _score.wordWrap = false;
            _metricValue = Text(T.FontTitle, FontStyle.Bold, T.Ink);
            _metricValue.alignment = TextAnchor.UpperCenter;
            _badge = Text(T.FontBody, FontStyle.Bold, T.Ink);
            _badge.alignment = TextAnchor.MiddleCenter;
            _badge.wordWrap = false;
            _glyph = Text(26, FontStyle.Normal, T.Ink);
            _glyph.alignment = TextAnchor.MiddleCenter;
            _glyph.wordWrap = false;

            _button = new GUIStyle(GUIStyle.none)
            {
                fontSize = Px(T.FontBodyLarge),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(Px(T.SpaceLg), Px(T.SpaceLg), 0, 0)
            };
            _hitArea = new GUIStyle(GUIStyle.none);

            _field = new GUIStyle(GUI.skin.textField)
            {
                fontSize = Px(T.FontBodyLarge),
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(Px(T.SpaceLg), Px(T.SpaceLg), 0, 0),
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            foreach (var state in new[] { _field.normal, _field.focused, _field.hover, _field.active })
            {
                state.background = null;
                state.textColor = T.Ink;
            }
        }

        int Px(float points) => Mathf.RoundToInt(points * _scale);

        GUIStyle Text(int size, FontStyle style, Color color) =>
            new GUIStyle(GUIStyle.none)
            {
                fontSize = Px(size),
                fontStyle = style,
                wordWrap = true,
                richText = false,
                clipping = TextClipping.Overflow,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = color }
            };

        void OnGUI()
        {
            if (_controller == null)
            {
                _controller = FindFirstObjectByType<SafetyLensSessionController>();
                if (_controller == null) return;
            }

            EnsureStyles();
            GUI.skin.settings.cursorColor = T.Ink;
            GUI.skin.settings.selectionColor = T.Brand;

            if (Event.current.type == EventType.Layout)
            {
                BlockingRects.Clear();
                _blocksWholeScreen = false;
            }

            if (_controller.Phase == AppPhase.History && _lastPhase != AppPhase.History)
                RefreshHistory();
            _lastPhase = _controller.Phase;

            switch (_controller.Phase)
            {
                case AppPhase.Identification: DrawHome(); break;
                case AppPhase.Scanning:
                case AppPhase.RiskInspection: DrawCamera(); break;
                case AppPhase.Quiz: DrawQuiz(); break;
                case AppPhase.Finished: DrawResult(); break;
                case AppPhase.History: DrawHistory(); break;
            }
        }

        void RefreshHistory()
        {
            _history.Clear();
            _history.AddRange(SafetyLensSessionStore.ListSessions());
            _historyScroll = Vector2.zero;
            _pendingDelete = null;
        }

        // ---------- Telas ----------

        void DrawHome()
        {
            _blocksWholeScreen = true;
            var sa = SafeArea();
            Fill(FullScreen(), T.Background, 0f);

            var pad = U(T.SpaceXl);
            var x = sa.x + pad;
            var w = sa.width - pad * 2f;
            var compact = sa.height / _scale < CompactHeightThreshold;

            const string headline = "Reconheça os riscos antes de operar.";
            const string lead = "Aponte a câmera para a placa do equipamento, encontre as zonas de risco e os EPIs e responda um quiz rápido.";
            var steps = new[]
            {
                "Abra a câmera e enquadre a placa",
                "Toque em cada item destacado",
                "Responda 3 perguntas de fixação"
            };
            var helper = string.IsNullOrEmpty(_controller.IdentificationError)
                ? "Use o código informado pelo pesquisador."
                : _controller.IdentificationError;
            var helperStyle = string.IsNullOrEmpty(_controller.IdentificationError) ? _caption : ErrorCaption();

            // Medição prévia: permite subir o conteúdo quando o teclado cobre o botão.
            var headlineH = Measure(headline, _display, w);
            var leadH = Measure(lead, _bodyLarge, w);
            var stepsH = compact ? 0f : MeasureSteps(steps, w);
            var helperH = Measure(helper, helperStyle, w);
            var brandH = U(40f);

            var formTop = U(T.SpaceLg) + brandH + U(T.Space2Xl) + headlineH + U(T.SpaceMd) + leadH + U(T.SpaceXl)
                          + (compact ? 0f : stepsH + U(T.SpaceXl));
            // Dois botões: Abrir câmera + Histórico.
            var buttonBottom = formTop + Measure("ID", _overline, w) + U(T.SpaceSm) + U(T.ControlHeight)
                               + U(T.SpaceSm) + helperH + U(T.SpaceXl)
                               + U(T.ControlHeight) + U(T.SpaceMd) + U(T.ControlHeight);

            var shift = 0f;
            if (TouchScreenKeyboard.visible)
            {
                var kb = TouchScreenKeyboard.area.height;
                if (kb <= 1f) kb = Screen.height * 0.42f;
                var limit = Screen.height - kb - U(T.SpaceLg);
                shift = Mathf.Min(0f, limit - (sa.y + buttonBottom));
            }

            var y = sa.y + U(T.SpaceLg) + shift;

            // Marca
            var mark = new Rect(x, y, brandH, brandH);
            Fill(mark, T.Brand, U(T.RadiusSm));
            Label(mark, "SL", _badge);
            var brandX = mark.xMax + U(T.SpaceMd);
            Label(new Rect(brandX, y + U(2f), w - brandH, U(20f)), "SafetyLens", _bodyStrong);
            Label(new Rect(brandX, y + U(21f), w - brandH, U(18f)), "Treinamento de segurança em RA", _caption);
            y += brandH + U(T.Space2Xl);

            y += Paragraph(x, y, w, headline, _display) + U(T.SpaceMd);
            y += Paragraph(x, y, w, lead, _bodyLarge) + U(T.SpaceXl);

            if (!compact)
            {
                DrawSteps(new Rect(x, y, w, stepsH), steps);
                y += stepsH + U(T.SpaceXl);
            }

            y += Paragraph(x, y, w, "ID DO PARTICIPANTE", _overline) + U(T.SpaceSm);

            var field = new Rect(x, y, w, U(T.ControlHeight));
            var focused = GUI.GetNameOfFocusedControl() == ParticipantFieldName;
            var hasError = !string.IsNullOrEmpty(_controller.IdentificationError);
            Fill(field, T.Surface, U(T.RadiusMd));
            Stroke(field, hasError ? T.Danger : focused ? T.Ink : T.Border,
                U(focused || hasError ? T.StrokeThick : T.StrokeThin), U(T.RadiusMd));
            if (string.IsNullOrEmpty(_participantInput))
                Label(new Rect(field.x + U(T.SpaceLg), field.y, field.width, field.height), "Ex.: P01", PlaceholderStyle());
            GUI.SetNextControlName(ParticipantFieldName);
            _participantInput = SafetyLensSessionController.SanitizeParticipantId(
                GUI.TextField(field, _participantInput, SafetyLensSessionController.MaxParticipantIdLength, _field));
            y += field.height + U(T.SpaceSm);

            y += Paragraph(x, y, w, helper, helperStyle) + U(T.SpaceXl);

            if (PrimaryButton(new Rect(x, y, w, U(T.ControlHeight)), "Abrir câmera"))
            {
                GUI.FocusControl(null);
                _controller.StartSession(_participantInput);
            }
            y += U(T.ControlHeight) + U(T.SpaceMd);

            if (SecondaryButton(new Rect(x, y, w, U(T.ControlHeight)), "Histórico de sessões"))
            {
                GUI.FocusControl(null);
                _controller.OpenHistory();
            }
            y += U(T.ControlHeight) + U(T.SpaceMd);

            var footer = "Funciona offline. Os resultados ficam salvos neste iPhone.";
            var footerStyle = Centered(_caption);
            var footerH = Measure(footer, footerStyle, w);
            Paragraph(x, Mathf.Min(y, sa.yMax - footerH - U(T.SpaceSm)), w, footer, footerStyle);
        }

        void DrawCamera()
        {
            var sa = SafeArea();
            var c = _controller;
            var pad = U(T.SpaceLg);

            // Barra superior: sair + cronômetro/erros.
            var top = sa.y + U(T.SpaceSm);
            var close = new Rect(sa.x + pad, top, U(T.TouchMin), U(T.TouchMin));
            Fill(close, T.Surface, close.height * 0.5f);
            Label(close, "×", _glyph);
            Block(close);
            if (GUI.Button(close, GUIContent.none, _hitArea)) c.RestartSession();

            var stats = $"{FormatTime(c.ElapsedSeconds)}   ·   {c.ErrorCount} {(c.ErrorCount == 1 ? "erro" : "erros")}";
            var statsStyle = OnDark(_captionStrong);
            var statsW = MeasureWidth(stats, statsStyle) + U(T.SpaceLg) * 2f;
            var statsPill = new Rect(sa.xMax - pad - statsW, top + (U(T.TouchMin) - U(36f)) * 0.5f, statsW, U(36f));
            Fill(statsPill, T.Scrim, statsPill.height * 0.5f);
            Label(statsPill, stats, Centered(statsStyle));
            Block(statsPill);

            var toastY = top + U(T.TouchMin) + U(T.SpaceMd);
            toastY = DrawArAvailabilityBanner(sa, toastY);
            toastY = DrawTrackingBanner(sa, toastY);
            DrawToast(sa, toastY);

            var searching = c.Phase == AppPhase.Scanning || !c.TargetFound;
            if (searching) DrawReticle(sa, c.Phase == AppPhase.Scanning ? "Enquadre a placa do equipamento" : "Marcador perdido");

            DrawCameraSheet(sa);
        }

        void DrawCameraSheet(Rect sa)
        {
            var c = _controller;
            // Card compacto: a metade de baixo da câmera também mostra itens da placa que precisam de toque.
            var margin = U(T.SpaceMd);
            var inner = U(T.SpaceLg);
            var sheetW = sa.width - margin * 2f;
            var w = sheetW - inner * 2f;

            string overline, title, body, hint, buttonLabel;
            Color chipBg = T.SurfaceMuted, chipFg = T.InkSoft;
            var hazard = c.CurrentHazard;
            var inspecting = c.Phase == AppPhase.RiskInspection && hazard != null;
            // RF03: botão só libera depois do toque no objeto AR do item ativo.
            var canConfirm = c.CanConfirmCurrentHazard;

            if (!inspecting)
            {
                overline = "PREPARAÇÃO";
                title = "Procurando a placa";
                body = "Segure o iPhone a uns 30 cm da placa, com boa iluminação. O cronômetro começa quando ela for reconhecida.";
                hint = string.Empty;
                buttonLabel = string.Empty;
            }
            else
            {
                overline = hazard.IsEpi ? "EPI OBRIGATÓRIO" : "ZONA DE RISCO";
                chipBg = hazard.IsEpi ? T.InfoSoft : T.DangerSoft;
                chipFg = hazard.IsEpi ? T.Info : T.Danger;
                title = hazard.label;
                body = hazard.instruction;
                if (!c.TargetFound)
                {
                    hint = c.StatusMessage;
                    buttonLabel = "Aponte para a placa";
                }
                else if (!c.CurrentHazardArSelected)
                {
                    hint = "Toque no item destacado na câmera para selecionar.";
                    buttonLabel = "Toque no item na câmera";
                }
                else
                {
                    hint = string.Empty;
                    buttonLabel = "Confirmar item";
                }
            }

            var chipStyle = Colored(_overline, chipFg);
            var stepText = inspecting ? $"Item {c.HazardConfirmedCount + 1} de {c.HazardCount}" : string.Empty;

            var h = inner;
            if (inspecting) h += U(6f) + U(T.SpaceMd);
            h += U(26f) + U(T.SpaceSm);
            h += Measure(title, _title, w) + U(T.SpaceXs);
            h += Measure(body, _body, w);
            if (!string.IsNullOrEmpty(hint)) h += U(T.SpaceSm) + Measure(hint, ErrorCaption(), w);
            if (inspecting) h += U(T.SpaceMd) + U(T.ControlHeight);
            h += inner;

            var sheet = new Rect(sa.x + margin, sa.yMax - margin - h, sheetW, h);
            Fill(sheet, T.Surface, U(T.RadiusLg));
            Block(sheet);

            var x = sheet.x + inner;
            var y = sheet.y + inner;

            if (inspecting)
            {
                DrawSegments(new Rect(x, y, w, U(6f)), c.HazardCount, c.HazardConfirmedCount, true);
                y += U(6f) + U(T.SpaceMd);
            }

            var chipW = MeasureWidth(overline, chipStyle) + U(T.SpaceMd) * 2f;
            var chip = new Rect(x, y, chipW, U(26f));
            Fill(chip, chipBg, chip.height * 0.5f);
            Label(chip, overline, Centered(chipStyle));
            if (!string.IsNullOrEmpty(stepText))
                Label(new Rect(x, y, w, chip.height), stepText, Aligned(_caption, TextAnchor.MiddleRight));
            y += chip.height + U(T.SpaceSm);

            y += Paragraph(x, y, w, title, _title) + U(T.SpaceXs);
            y += Paragraph(x, y, w, body, _body);

            if (!string.IsNullOrEmpty(hint))
            {
                y += U(T.SpaceSm);
                y += Paragraph(x, y, w, hint, ErrorCaption());
            }

            if (inspecting)
            {
                y += U(T.SpaceMd);
                if (PrimaryButton(new Rect(x, y, w, U(T.ControlHeight)), buttonLabel, canConfirm))
                    c.ConfirmCurrentHazard();
            }
        }

        void DrawQuiz()
        {
            _blocksWholeScreen = true;
            var sa = SafeArea();
            var c = _controller;
            var quiz = c.CurrentQuiz;
            Fill(FullScreen(), T.Background, 0f);
            if (quiz?.options == null) return;

            var pad = U(T.SpaceXl);
            var x = sa.x + pad;
            var w = sa.width - pad * 2f;
            var y = sa.y + U(T.SpaceLg);

            Label(new Rect(x, y, w, U(18f)), "QUIZ DE FIXAÇÃO", _overline);
            Label(new Rect(x, y, w, U(18f)), $"{c.ActiveQuizIndex + 1} de {c.QuizCount}", Aligned(_captionStrong, TextAnchor.UpperRight));
            y += U(18f) + U(T.SpaceSm);
            DrawSegments(new Rect(x, y, w, U(6f)), c.QuizCount, c.ActiveQuizIndex + (c.QuizSelectedIndex >= 0 ? 1 : 0), false);
            y += U(6f) + U(T.Space2Xl);

            y += Paragraph(x, y, w, quiz.question, _title) + U(T.SpaceXl);

            var answered = c.QuizSelectedIndex >= 0;
            for (var i = 0; i < quiz.options.Length; i++)
                y += DrawOption(x, y, w, i, quiz, answered) + U(T.SpaceMd);

            var buttonRect = new Rect(x, sa.yMax - U(T.SpaceLg) - U(T.ControlHeight), w, U(T.ControlHeight));
            if (answered)
            {
                var correct = c.QuizSelectedIndex == quiz.correctIndex;
                var verdict = correct ? "Resposta correta." : $"Resposta certa: {quiz.options[quiz.correctIndex]}";
                var verdictStyle = Colored(_bodyStrong, correct ? T.Success : T.Danger);
                var verdictY = Mathf.Min(y + U(T.SpaceSm), buttonRect.y - U(T.SpaceLg) - Measure(verdict, verdictStyle, w));
                Paragraph(x, verdictY, w, verdict, verdictStyle);

                var last = c.ActiveQuizIndex >= c.QuizCount - 1;
                if (PrimaryButton(buttonRect, last ? "Ver resultado" : "Próxima pergunta"))
                    c.NextQuiz();
            }
            else
            {
                Label(buttonRect, "Toque em uma alternativa", Centered(_caption));
            }
        }

        float DrawOption(float x, float y, float w, int index, SafetyLensQuizQuestion quiz, bool answered)
        {
            var c = _controller;
            var inner = U(T.SpaceLg);
            var badgeSize = U(32f);
            var textX = inner + badgeSize + U(T.SpaceMd);
            var textW = w - textX - inner;
            var text = quiz.options[index];
            var textH = Measure(text, _bodyLarge, textW);
            var h = Mathf.Max(badgeSize, textH) + inner * 2f;
            var rect = new Rect(x, y, w, h);

            var isCorrect = index == quiz.correctIndex;
            var isSelected = index == c.QuizSelectedIndex;
            Color bg = T.Surface, border = T.Border, badgeBg = T.SurfaceMuted, badgeFg = T.Ink, textColor = T.Ink;
            var borderWidth = U(T.StrokeThin);

            if (answered && isCorrect)
            {
                bg = T.SuccessSoft; border = T.Success; badgeBg = T.Success; badgeFg = T.OnDark;
                borderWidth = U(T.StrokeThick);
            }
            else if (answered && isSelected)
            {
                bg = T.DangerSoft; border = T.Danger; badgeBg = T.Danger; badgeFg = T.OnDark;
                borderWidth = U(T.StrokeThick);
            }
            else if (answered)
            {
                textColor = T.InkFaint;
            }
            else if (IsPressed(rect))
            {
                bg = T.SurfaceMuted; border = T.Ink;
            }

            Fill(rect, bg, U(T.RadiusMd));
            Stroke(rect, border, borderWidth, U(T.RadiusMd));

            var badge = new Rect(x + inner, y + (h - badgeSize) * 0.5f, badgeSize, badgeSize);
            Fill(badge, badgeBg, badgeSize * 0.5f);
            Label(badge, ((char)('A' + index)).ToString(), Colored(_badge, badgeFg));

            Label(new Rect(x + textX, y + (h - textH) * 0.5f, textW, textH), text, Colored(_bodyLarge, textColor));

            if (!answered && GUI.Button(rect, GUIContent.none, _hitArea))
                c.AnswerQuiz(index);
            return h;
        }

        void DrawResult()
        {
            _blocksWholeScreen = true;
            var sa = SafeArea();
            var c = _controller;
            Fill(FullScreen(), T.Background, 0f);

            var pad = U(T.SpaceXl);
            var x = sa.x + pad;
            var w = sa.width - pad * 2f;
            var footerH = U(T.ControlHeight) * 2f + U(T.SpaceMd) + U(T.SpaceLg);
            var y = sa.y + U(T.SpaceLg);

            var ratio = c.QuizCount == 0 ? 0f : c.QuizCorrectCount / (float)c.QuizCount;
            var accent = ratio >= 0.999f ? T.Success : ratio >= 0.5f ? T.Brand : T.Danger;
            var accentSoft = ratio >= 0.999f ? T.SuccessSoft : ratio >= 0.5f ? T.SurfaceMuted : T.DangerSoft;

            var ring = U(112f);
            var ringRect = new Rect(x + (w - ring) * 0.5f, y, ring, ring);
            Fill(ringRect, accent, ring * 0.5f);
            var innerRing = new Rect(ringRect.x + U(7f), ringRect.y + U(7f), ring - U(14f), ring - U(14f));
            Fill(innerRing, accentSoft, innerRing.width * 0.5f);
            Label(new Rect(innerRing.x, innerRing.y + U(14f), innerRing.width, U(40f)), $"{c.QuizCorrectCount}/{c.QuizCount}", _score);
            Label(new Rect(innerRing.x, innerRing.y + U(54f), innerRing.width, U(18f)), "no quiz", Centered(_caption));
            y += ring + U(T.SpaceLg);

            var headline = ratio >= 0.999f ? "Excelente trabalho!" : "Treinamento concluído";
            y += Paragraph(x, y, w, headline, Centered(_display)) + U(T.SpaceXs);
            var summary = $"{c.ParticipantId}, você identificou {c.HazardConfirmedCount} de {c.HazardCount} itens de segurança.";
            y += Paragraph(x, y, w, summary, Centered(_bodyLarge)) + U(T.SpaceLg);

            var gap = U(T.SpaceMd);
            var cellW = (w - gap * 2f) / 3f;
            var cellH = U(78f);
            DrawMetric(new Rect(x, y, cellW, cellH), FormatTime(c.ElapsedSeconds), "Tempo");
            DrawMetric(new Rect(x + cellW + gap, y, cellW, cellH), c.ErrorCount.ToString(), c.ErrorCount == 1 ? "Erro" : "Erros");
            DrawMetric(new Rect(x + (cellW + gap) * 2f, y, cellW, cellH), $"{c.HazardConfirmedCount}/{c.HazardCount}", "Itens");
            y += cellH + U(T.SpaceMd);

            var savedTitle = c.ExportFailed ? "Não foi possível salvar" : "Salvo offline neste iPhone";
            var savedBody = c.ExportFailed
                ? "Verifique o espaço livre do aparelho e faça uma nova sessão."
                : ShortenMiddle(Path.GetFileName(SafetyLensSessionStore.LastCsvPath), 38);
            var savedStyle = c.ExportFailed ? Colored(_bodyStrong, T.Danger) : _bodyStrong;
            var inner = U(T.SpaceMd);
            var savedH = inner * 2f + Measure(savedTitle, savedStyle, w - inner * 2f) + U(T.SpaceXs)
                         + Measure(savedBody, _caption, w - inner * 2f);
            var saved = new Rect(x, y, w, savedH);
            Fill(saved, c.ExportFailed ? T.DangerSoft : T.SurfaceMuted, U(T.RadiusMd));
            var sy = saved.y + inner;
            sy += Paragraph(saved.x + inner, sy, w - inner * 2f, savedTitle, savedStyle) + U(T.SpaceXs);
            Paragraph(saved.x + inner, sy, w - inner * 2f, savedBody, _caption);
            y += savedH + U(T.SpaceSm);

            if (!string.IsNullOrEmpty(c.ShareStatus))
                Paragraph(x, y, w, c.ShareStatus, Centered(_caption));

            var shareY = sa.yMax - footerH;
            var canShare = !c.ExportFailed && SafetyLensSessionStore.CanShareLastCsv();
            if (PrimaryButton(new Rect(x, shareY, w, U(T.ControlHeight)), "Compartilhar resultado", canShare))
                c.ShareLastCsv();
            if (SecondaryButton(new Rect(x, shareY + U(T.ControlHeight) + U(T.SpaceMd), w, U(T.ControlHeight)), "Nova sessão"))
            {
                _participantInput = string.Empty;
                c.RestartSession();
            }
        }

        void DrawHistory()
        {
            _blocksWholeScreen = true;
            var sa = SafeArea();
            var c = _controller;
            Fill(FullScreen(), T.Background, 0f);

            var pad = U(T.SpaceXl);
            var x = sa.x + pad;
            var w = sa.width - pad * 2f;
            var y = sa.y + U(T.SpaceSm);

            var back = new Rect(x, y, U(T.TouchMin), U(T.TouchMin));
            Fill(back, T.Surface, back.height * 0.5f);
            Stroke(back, T.Border, U(T.StrokeThin), back.height * 0.5f);
            Label(back, "←", _glyph);
            Block(back);
            if (GUI.Button(back, GUIContent.none, _hitArea)) c.CloseHistory();

            Label(new Rect(x + U(T.TouchMin) + U(T.SpaceMd), y + U(4f), w - U(T.TouchMin), U(22f)), "Histórico", _title);
            Label(
                new Rect(x + U(T.TouchMin) + U(T.SpaceMd), y + U(28f), w - U(T.TouchMin), U(18f)),
                _history.Count == 0 ? "Nenhuma sessão ainda" : $"{_history.Count} sessão(ões) neste aparelho",
                _caption);
            y += U(T.TouchMin) + U(T.SpaceLg);

            var footerH = _history.Count > 0
                ? U(T.ControlHeight) + U(T.SpaceLg) + (!string.IsNullOrEmpty(c.ShareStatus) ? U(22f) : 0f)
                : U(T.SpaceLg);
            var listTop = y;
            var listH = sa.yMax - listTop - footerH - U(T.SpaceSm);

            if (_history.Count == 0)
            {
                var empty = new Rect(x, listTop, w, Mathf.Min(listH, U(160f)));
                Fill(empty, T.Surface, U(T.RadiusMd));
                Stroke(empty, T.Border, U(T.StrokeThin), U(T.RadiusMd));
                Paragraph(
                    empty.x + U(T.SpaceLg),
                    empty.y + U(T.SpaceXl),
                    empty.width - U(T.SpaceLg) * 2f,
                    "Conclua um treinamento para ver ID, tempo, erros e quiz aqui. Depois você pode compartilhar o CSV.",
                    Centered(_body));
                return;
            }

            // Altura total do conteúdo: cada linha ~92pt (+ confirmação) + gap.
            var rowH = U(92f);
            var confirmExtra = U(48f);
            var gap = U(T.SpaceMd);
            var contentH = 0f;
            foreach (var entry in _history)
                contentH += rowH + gap + (_pendingDelete == entry ? confirmExtra : 0f);
            _historyScroll = GUI.BeginScrollView(
                new Rect(x, listTop, w, listH),
                _historyScroll,
                new Rect(0f, 0f, w - U(8f), contentH),
                false,
                false);

            var rowY = 0f;
            var rowW = w - U(8f);
            for (var i = 0; i < _history.Count; i++)
            {
                var entry = _history[i];
                DrawHistoryRow(new Rect(0f, rowY, rowW, rowH), entry);
                rowY += rowH;
                if (_pendingDelete == entry)
                {
                    DrawHistoryDeleteConfirm(new Rect(0f, rowY, rowW, confirmExtra), entry);
                    rowY += confirmExtra;
                }

                rowY += gap;
            }

            GUI.EndScrollView();

            if (!string.IsNullOrEmpty(c.ShareStatus))
                Paragraph(x, sa.yMax - footerH, w, c.ShareStatus, Centered(_caption));

            if (SecondaryButton(
                    new Rect(x, sa.yMax - U(T.SpaceLg) - U(T.ControlHeight), w, U(T.ControlHeight)),
                    "Compartilhar todas (CSV)"))
                c.ShareAllSessionsCsv();
        }

        void DrawHistoryRow(Rect r, SafetyLensSessionEntry entry)
        {
            var record = entry.Record;
            Fill(r, T.Surface, U(T.RadiusMd));
            Stroke(r, T.Border, U(T.StrokeThin), U(T.RadiusMd));

            var pad = U(T.SpaceMd);
            var btnW = U(72f);
            var textW = r.width - pad * 2f - btnW * 2f - U(T.SpaceSm) * 2f;

            Label(new Rect(r.x + pad, r.y + pad, textW, U(22f)), record.participantId, _bodyStrong);
            Label(
                new Rect(r.x + pad, r.y + pad + U(22f), textW, U(18f)),
                FormatSessionWhen(record),
                _caption);

            var meta = $"{FormatTime(record.elapsedSeconds)}  ·  {record.errorCount} erro(s)  ·  quiz {record.quizCorrect}/{record.quizTotal}";
            Label(new Rect(r.x + pad, r.y + pad + U(44f), textW, U(20f)), meta, _captionStrong);

            var share = new Rect(r.xMax - pad - btnW, r.y + (r.height - U(40f)) * 0.5f, btnW, U(40f));
            var del = new Rect(share.x - U(T.SpaceSm) - btnW, share.y, btnW, U(40f));
            if (SecondaryButton(del, "Apagar", true, compact: true))
                _pendingDelete = entry;
            if (SecondaryButton(share, "CSV", true, compact: true))
                _controller.ShareSessionCsv(entry);
        }

        void DrawHistoryDeleteConfirm(Rect r, SafetyLensSessionEntry entry)
        {
            Label(new Rect(r.x + U(T.SpaceMd), r.y + U(4f), r.width * 0.5f, U(36f)), "Apagar esta sessão?", _captionStrong);
            var no = new Rect(r.xMax - U(T.SpaceMd) - U(72f) * 2f - U(T.SpaceSm), r.y + U(4f), U(72f), U(36f));
            var yes = new Rect(r.xMax - U(T.SpaceMd) - U(72f), r.y + U(4f), U(72f), U(36f));
            if (SecondaryButton(no, "Não", true, compact: true))
                _pendingDelete = null;
            if (PrimaryButton(yes, "Sim"))
            {
                if (_controller.DeleteSession(entry))
                {
                    _pendingDelete = null;
                    RefreshHistory();
                }
            }
        }

        // ---------- Componentes ----------

        void DrawSteps(Rect area, string[] steps)
        {
            Fill(area, T.Surface, U(T.RadiusMd));
            Stroke(area, T.Border, U(T.StrokeThin), U(T.RadiusMd));
            var inner = U(T.SpaceLg);
            var badge = U(28f);
            var textX = area.x + inner + badge + U(T.SpaceMd);
            var textW = area.xMax - inner - textX;
            var y = area.y + inner;
            for (var i = 0; i < steps.Length; i++)
            {
                var textH = Measure(steps[i], _body, textW);
                var rowH = Mathf.Max(badge, textH);
                var b = new Rect(area.x + inner, y + (rowH - badge) * 0.5f, badge, badge);
                Fill(b, T.Brand, badge * 0.5f);
                Label(b, (i + 1).ToString(), Colored(_badge, T.OnBrand));
                Label(new Rect(textX, y + (rowH - textH) * 0.5f, textW, textH), steps[i], Colored(_body, T.Ink));
                y += rowH + U(T.SpaceMd);
            }
        }

        float MeasureSteps(string[] steps, float width)
        {
            var inner = U(T.SpaceLg);
            var badge = U(28f);
            var textW = width - inner * 2f - badge - U(T.SpaceMd);
            var h = inner * 2f - U(T.SpaceMd);
            foreach (var s in steps)
                h += Mathf.Max(badge, Measure(s, _body, textW)) + U(T.SpaceMd);
            return h;
        }

        void DrawSegments(Rect r, int total, int done, bool highlightCurrent)
        {
            if (total <= 0) return;
            var gap = U(T.SpaceXs) * 1.5f;
            var segW = (r.width - gap * (total - 1)) / total;
            for (var i = 0; i < total; i++)
            {
                var color = i < done ? T.Success : highlightCurrent && i == done ? T.Brand : T.Border;
                Fill(new Rect(r.x + i * (segW + gap), r.y, segW, r.height), color, r.height * 0.5f);
            }
        }

        void DrawMetric(Rect r, string value, string label)
        {
            Fill(r, T.Surface, U(T.RadiusMd));
            Stroke(r, T.Border, U(T.StrokeThin), U(T.RadiusMd));
            Label(new Rect(r.x, r.y + U(T.SpaceLg), r.width, U(30f)), value, _metricValue);
            Label(new Rect(r.x, r.y + U(52f), r.width, U(18f)), label, Centered(_caption));
        }

        /// <summary>P1-8: device sem AR / sessão que não sobe. Banner vermelho, não bloqueia toque do ×.</summary>
        float DrawArAvailabilityBanner(Rect sa, float y)
        {
            var msg = _controller.ArAvailabilityMessage;
            if (string.IsNullOrEmpty(msg)) return y;
            return DrawStatusBanner(sa, y, msg, T.Danger, OnDark(_bodyStrong));
        }

        /// <summary>Banner persistente em Unstable. Não entra em BlockingRects (não intercepta toque).</summary>
        float DrawTrackingBanner(Rect sa, float y)
        {
            var msg = _controller.TargetStatusMessage;
            if (string.IsNullOrEmpty(msg)) return y;
            return DrawStatusBanner(sa, y, msg, T.Brand, Colored(_bodyStrong, T.OnBrand));
        }

        float DrawStatusBanner(Rect sa, float y, string msg, Color fill, GUIStyle style)
        {
            style = Centered(style);
            var maxW = sa.width - U(T.SpaceLg) * 2f;
            var textW = Mathf.Min(MeasureWidth(msg, style), maxW - U(T.SpaceXl) * 2f);
            var textH = Measure(msg, style, textW);
            var banner = new Rect(
                sa.x + (sa.width - textW - U(T.SpaceXl) * 2f) * 0.5f, y,
                textW + U(T.SpaceXl) * 2f, textH + U(T.SpaceMd) * 2f);
            Fill(banner, fill, U(T.RadiusMd));
            Label(new Rect(banner.x + U(T.SpaceXl), banner.y + U(T.SpaceMd), textW, textH), msg, style);
            return y + banner.height + U(T.SpaceSm);
        }

        void DrawToast(Rect sa, float y)
        {
            var c = _controller;
            var age = Time.realtimeSinceStartup - c.FeedbackAt;
            if (string.IsNullOrEmpty(c.FeedbackText) || age < 0f || age > T.FeedbackDuration) return;

            var style = Centered(OnDark(_bodyStrong));
            var maxW = sa.width - U(T.SpaceLg) * 2f;
            var textW = Mathf.Min(MeasureWidth(c.FeedbackText, style), maxW - U(T.SpaceXl) * 2f);
            var textH = Measure(c.FeedbackText, style, textW);
            var toast = new Rect(
                sa.x + (sa.width - textW - U(T.SpaceXl) * 2f) * 0.5f, y,
                textW + U(T.SpaceXl) * 2f, textH + U(T.SpaceMd) * 2f);
            Fill(toast, c.FeedbackIsError ? T.Danger : T.Success, U(T.RadiusMd));
            Label(new Rect(toast.x + U(T.SpaceXl), toast.y + U(T.SpaceMd), textW, textH), c.FeedbackText, style);
        }

        void DrawReticle(Rect sa, string caption)
        {
            var w = Mathf.Min(sa.width - U(T.Space2Xl) * 2f, U(300f));
            var h = w * 0.66f;
            var r = new Rect(sa.x + (sa.width - w) * 0.5f, sa.y + sa.height * 0.30f - h * 0.5f, w, h);
            var len = U(28f);
            var th = U(4f);
            foreach (var corner in new[]
                     {
                         new Rect(r.x, r.y, len, th), new Rect(r.x, r.y, th, len),
                         new Rect(r.xMax - len, r.y, len, th), new Rect(r.xMax - th, r.y, th, len),
                         new Rect(r.x, r.yMax - th, len, th), new Rect(r.x, r.yMax - len, th, len),
                         new Rect(r.xMax - len, r.yMax - th, len, th), new Rect(r.xMax - th, r.yMax - len, th, len)
                     })
                Fill(corner, T.ReticleStroke, th * 0.5f);

            var style = Centered(OnDark(_captionStrong));
            var pillW = MeasureWidth(caption, style) + U(T.SpaceLg) * 2f;
            var pill = new Rect(r.x + (r.width - pillW) * 0.5f, r.yMax + U(T.SpaceLg), pillW, U(32f));
            Fill(pill, T.Scrim, pill.height * 0.5f);
            Label(pill, caption, style);
        }

        bool PrimaryButton(Rect r, string label, bool enabled = true)
        {
            var bg = enabled ? T.Brand : T.SurfaceMuted;
            if (enabled && IsPressed(r)) bg = Color.Lerp(T.Brand, T.Ink, 0.12f);
            Fill(r, bg, U(T.RadiusMd));
            _button.normal.textColor = enabled ? T.OnBrand : T.InkFaint;
            _button.active.textColor = _button.normal.textColor;
            _button.hover.textColor = _button.normal.textColor;
            Block(r);
            var clicked = GUI.Button(r, label, _button);
            return enabled && clicked;
        }

        bool SecondaryButton(Rect r, string label, bool enabled = true, bool compact = false)
        {
            var bg = enabled ? T.Surface : T.SurfaceMuted;
            if (enabled && IsPressed(r)) bg = T.SurfaceMuted;
            Fill(r, bg, U(compact ? T.RadiusSm : T.RadiusMd));
            Stroke(r, enabled ? T.Border : T.SurfaceMuted, U(T.StrokeThin), U(compact ? T.RadiusSm : T.RadiusMd));
            _button.normal.textColor = enabled ? T.Ink : T.InkFaint;
            _button.active.textColor = _button.normal.textColor;
            _button.hover.textColor = _button.normal.textColor;
            Block(r);
            var clicked = GUI.Button(r, label, _button);
            return enabled && clicked;
        }

        // ---------- Utilitários ----------

        Rect SafeArea()
        {
            var sa = Screen.safeArea;
            if (sa.width < 8f || sa.height < 8f) return FullScreen();
            // Screen.safeArea tem origem embaixo; IMGUI tem origem em cima.
            return new Rect(sa.x, Screen.height - sa.yMax, sa.width, sa.height);
        }

        static Rect FullScreen() => new Rect(0f, 0f, Screen.width, Screen.height);

        static void Fill(Rect r, Color color, float radius)
        {
            var maxRadius = Mathf.Min(r.width, r.height) * 0.5f;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f,
                Mathf.Min(radius, maxRadius));
        }

        static void Stroke(Rect r, Color color, float width, float radius)
        {
            var maxRadius = Mathf.Min(r.width, r.height) * 0.5f;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, width,
                Mathf.Min(radius, maxRadius));
        }

        static float Measure(string text, GUIStyle style, float width)
        {
            Content.text = text ?? string.Empty;
            return style.CalcHeight(Content, width);
        }

        static float MeasureWidth(string text, GUIStyle style)
        {
            Content.text = text ?? string.Empty;
            return style.CalcSize(Content).x;
        }

        static float Paragraph(float x, float y, float width, string text, GUIStyle style)
        {
            var h = Measure(text, style, width);
            GUI.Label(new Rect(x, y, width, h), Content, style);
            return h;
        }

        static void Label(Rect r, string text, GUIStyle style)
        {
            Content.text = text ?? string.Empty;
            GUI.Label(r, Content, style);
        }

        GUIStyle Variant(GUIStyle source, Color color, TextAnchor anchor)
        {
            if (source.normal.textColor == color && source.alignment == anchor) return source;
            var key = (source, color, anchor);
            if (_variants.TryGetValue(key, out var cached)) return cached;
            var style = new GUIStyle(source) { alignment = anchor };
            style.normal.textColor = color;
            _variants[key] = style;
            return style;
        }

        GUIStyle Colored(GUIStyle source, Color color) => Variant(source, color, source.alignment);
        GUIStyle Aligned(GUIStyle source, TextAnchor anchor) => Variant(source, source.normal.textColor, anchor);
        GUIStyle Centered(GUIStyle source) => Aligned(source, TextAnchor.MiddleCenter);
        GUIStyle OnDark(GUIStyle source) => Colored(source, T.OnDark);
        GUIStyle ErrorCaption() => Colored(_captionStrong, T.Danger);
        GUIStyle PlaceholderStyle() => Aligned(Colored(_bodyLarge, T.InkFaint), TextAnchor.MiddleLeft);

        static bool IsPressed(Rect r)
        {
            if (Input.touchCount == 0) return false;
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) return false;
            return r.Contains(new Vector2(t.position.x, Screen.height - t.position.y));
        }

        static void Block(Rect r) => BlockingRects.Add(r);

        static string FormatTime(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        static string ShortenMiddle(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return "Arquivo local";
            if (value.Length <= max) return value;
            var half = (max - 1) / 2;
            return value.Substring(0, half) + "…" + value.Substring(value.Length - half);
        }

        static string FormatSessionWhen(SafetyLensSessionRecord record)
        {
            var raw = record.finishedAtIso ?? record.exportedAtIso;
            if (string.IsNullOrEmpty(raw)) return "Data desconhecida";
            if (!DateTime.TryParse(raw, null, DateTimeStyles.RoundtripKind, out var utc))
                return raw;
            var local = utc.ToLocalTime();
            return local.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR"));
        }

        /// <summary>
        /// Usado pelo controller para não tratar toques na UI como seleção de objeto na RA.
        /// </summary>
        public static bool IsTouchOverOverlay(Vector2 screenPosition)
        {
            if (_blocksWholeScreen) return true;
            var guiPoint = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            foreach (var r in BlockingRects)
                if (r.Contains(guiPoint)) return true;
            return false;
        }
    }
}
