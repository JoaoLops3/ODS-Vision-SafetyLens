using System.Globalization;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace Facens.SafetyLens.Tests
{
    /// <summary>
    /// Caracterização do domínio puro (UV→metros, sanitize, CSV) sem cena AR.
    /// </summary>
    public sealed class SafetyLensDomainTests
    {
        static SafetyLensHazard Hazard(string kind, float u, float v, float x = 0f, float y = 0f, float z = 0f) =>
            new SafetyLensHazard
            {
                id = "T",
                kind = kind,
                u = u,
                v = v,
                x = x,
                y = y,
                z = z
            };

        // --- A1: LocalPositionOnMarker (método de instância em SafetyLensHazard) ---

        [Test]
        public void LocalPositionOnMarker_Center_ReturnsOriginWithAlertLift()
        {
            var h = Hazard("alert", 0.5f, 0.5f);
            var pos = h.LocalPositionOnMarker(new Vector2(0.20f, 0.133333f));

            Assert.AreEqual(0f, pos.x, 1e-5f);
            Assert.AreEqual(SafetyLensTheme.ArMarkerLift, pos.y, 1e-5f);
            Assert.AreEqual(0f, pos.z, 1e-5f);
        }

        [Test]
        public void LocalPositionOnMarker_AAndC_MatchUvMath()
        {
            var size = new Vector2(0.20f, 0.133333f);

            var posA = Hazard("alert", 0.238f, 0.470f).LocalPositionOnMarker(size);
            Assert.AreEqual((0.238f - 0.5f) * 0.20f, posA.x, 1e-5f);
            Assert.AreEqual((0.470f - 0.5f) * 0.133333f, posA.z, 1e-5f);

            var posC = Hazard("epi", 0.763f, 0.493f).LocalPositionOnMarker(size);
            Assert.AreEqual((0.763f - 0.5f) * 0.20f, posC.x, 1e-5f);
            Assert.AreEqual((0.493f - 0.5f) * 0.133333f, posC.z, 1e-5f);
            Assert.AreEqual(SafetyLensTheme.ArHelmetLift, posC.y, 1e-5f);
        }

        [Test]
        public void LocalPositionOnMarker_ZeroSize_FallsBackToTheme()
        {
            var h = Hazard("alert", 1f, 0f);
            var pos = h.LocalPositionOnMarker(Vector2.zero);

            Assert.AreEqual(0.5f * SafetyLensTheme.ArMarkerWidth, pos.x, 1e-5f);
            Assert.AreEqual(-0.5f * SafetyLensTheme.ArMarkerHeight, pos.z, 1e-5f);
        }

        [Test]
        public void LocalPositionOnMarker_ExplicitZ_OverridesLift()
        {
            var h = Hazard("epi", 0.5f, 0.5f, z: 0.01f);
            var pos = h.LocalPositionOnMarker(new Vector2(0.20f, 0.133333f));
            Assert.AreEqual(0.01f, pos.y, 1e-5f);
        }

        // --- A2: SanitizeParticipantId vs SanitizeFilePart ---

        [Test]
        public void SanitizeParticipantId_LimitsTo24_AndDropsInvalid_KeepsAccentAndSpace()
        {
            var input = "João_da_Silva_1234567890@Test!";
            var result = SafetyLensSessionController.SanitizeParticipantId(input);

            Assert.AreEqual(24, result.Length);
            Assert.IsFalse(result.Contains("@"));
            Assert.IsFalse(result.Contains("!"));
            Assert.IsTrue(result.Contains("João") || result.StartsWith("Jo"));
            Assert.IsTrue(result.Contains(" ") || result.Contains("_"));
        }

        [Test]
        public void SanitizeParticipantId_Empty_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, SafetyLensSessionController.SanitizeParticipantId(null));
            Assert.AreEqual(string.Empty, SafetyLensSessionController.SanitizeParticipantId(""));
        }

        [Test]
        public void SanitizeFilePart_EmptyBecomesAnon_SpaceBecomesUnderscore()
        {
            Assert.AreEqual("anon", SafetyLensSessionStore.SanitizeFilePart("  "));
            Assert.AreEqual("P_01", SafetyLensSessionStore.SanitizeFilePart("P 01"));
            Assert.AreEqual("João_da_Silva", SafetyLensSessionStore.SanitizeFilePart("João da Silva"));
        }

        // --- A3: BuildCsv / Escape ---

        [Test]
        public void BuildCsv_UsesInvariantCultureForDecimals()
        {
            var original = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-BR");
            try
            {
                var record = new SafetyLensSessionRecord
                {
                    participantId = "T1",
                    elapsedSeconds = 45.5f,
                    inspectionSeconds = 30.25f,
                    quizSeconds = 15.25f,
                    errorCount = 0
                };
                var csv = SafetyLensSessionStore.BuildCsv(new[] { record });
                Assert.IsTrue(csv.Contains("45.50"), csv);
                Assert.IsFalse(csv.Contains("45,50"), csv);
                Assert.IsTrue(csv.Contains("30.25"), csv);
                Assert.IsTrue(csv.Contains("15.25"), csv);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Test]
        public void BuildCsv_EscapesCommaAndQuotes_AndKeepsHeaderColumns()
        {
            var record = new SafetyLensSessionRecord
            {
                participantId = "A,B",
                scenarioTitle = "diz \"oi\"",
                quizAnswers = new[] { "Q1:a:OK", "Q2:b:ERR" },
                elapsedSeconds = 1f
            };
            var csv = SafetyLensSessionStore.BuildCsv(new[] { record });

            StringAssert.StartsWith(
                "participantId,scenarioId,scenarioTitle,elapsedSeconds,errorCount,hazardsConfirmed,hazardsTotal,quizCorrect,quizTotal,quizAnswers,startedAtIso,finishedAtIso,exportedAtIso",
                csv);
            Assert.IsTrue(csv.Contains("\"A,B\""), csv);
            Assert.IsTrue(csv.Contains("\"diz \"\"oi\"\"\""), csv);
            Assert.IsTrue(csv.Contains("Q1:a:OK|Q2:b:ERR"), csv);
        }

        [Test]
        public void Escape_WrapsWhenNeeded()
        {
            Assert.AreEqual(string.Empty, SafetyLensSessionStore.Escape(null));
            Assert.AreEqual("ok", SafetyLensSessionStore.Escape("ok"));
            Assert.AreEqual("\"a,b\"", SafetyLensSessionStore.Escape("a,b"));
            Assert.AreEqual("\"a\"\"b\"", SafetyLensSessionStore.Escape("a\"b"));
        }
    }
}
