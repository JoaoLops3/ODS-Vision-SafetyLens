using NUnit.Framework;

namespace Facens.SafetyLens.Tests
{
    public sealed class SafetyLensSessionFlowTests
    {
        [Test]
        public void HappyPath_WithTelemetry()
        {
            var flow = new SafetyLensSessionFlow();
            Assert.AreEqual(AppPhase.Identification, flow.Phase);

            Assert.IsTrue(flow.TryEnterScanning());
            Assert.IsTrue(flow.TryEnterEquipmentBrief());
            Assert.IsTrue(flow.TryEnterRiskInspection());
            Assert.IsTrue(flow.TryEnterQuiz());
            Assert.IsTrue(flow.TryEnterTelemetry());
            Assert.IsTrue(flow.TryEnterFinished());
            Assert.AreEqual(AppPhase.Finished, flow.Phase);
        }

        [Test]
        public void Quiz_CanSkipTelemetry_ToFinished()
        {
            var flow = new SafetyLensSessionFlow();
            flow.TryEnterScanning();
            flow.TryEnterEquipmentBrief();
            flow.TryEnterRiskInspection();
            flow.TryEnterQuiz();
            Assert.AreEqual(AppPhase.Quiz, flow.Phase);
            Assert.IsTrue(flow.TryEnterFinished());
            Assert.AreEqual(AppPhase.Finished, flow.Phase);
        }

        [Test]
        public void InvalidTransitions_AreRejected()
        {
            var flow = new SafetyLensSessionFlow();
            Assert.IsFalse(flow.TryEnterEquipmentBrief());
            Assert.IsFalse(flow.TryEnterRiskInspection());
            Assert.IsFalse(flow.TryEnterQuiz());
            Assert.IsFalse(flow.TryEnterTelemetry());
            Assert.IsFalse(flow.TryEnterFinished());
            Assert.AreEqual(AppPhase.Identification, flow.Phase);
        }

        [Test]
        public void Scanning_CanEnterQrMachine()
        {
            var flow = new SafetyLensSessionFlow();
            Assert.IsFalse(flow.TryEnterQrMachine());
            Assert.IsTrue(flow.TryEnterScanning());
            Assert.IsTrue(flow.TryEnterQrMachine());
            Assert.AreEqual(AppPhase.QrMachine, flow.Phase);
            Assert.IsFalse(flow.TryEnterEquipmentBrief());
            Assert.IsFalse(flow.TryEnterQuiz());
        }

        [Test]
        public void Scanning_CannotSkipBrief_ToRiskInspection()
        {
            var flow = new SafetyLensSessionFlow();
            flow.TryEnterScanning();
            Assert.IsFalse(flow.TryEnterRiskInspection());
            Assert.AreEqual(AppPhase.Scanning, flow.Phase);
        }

        [Test]
        public void History_OnlyFromIdentificationOrFinished()
        {
            var flow = new SafetyLensSessionFlow();
            Assert.IsTrue(flow.TryOpenHistory());
            Assert.AreEqual(AppPhase.History, flow.Phase);
            Assert.IsTrue(flow.TryCloseHistory());
            Assert.AreEqual(AppPhase.Identification, flow.Phase);

            flow.TryEnterScanning();
            Assert.IsFalse(flow.TryOpenHistory());

            flow.EnterIdentification();
            flow.TryEnterScanning();
            flow.TryEnterEquipmentBrief();
            flow.TryEnterRiskInspection();
            flow.TryEnterQuiz();
            flow.TryEnterFinished();
            Assert.IsTrue(flow.TryOpenHistory());
            Assert.IsTrue(flow.TryCloseHistory());
            Assert.AreEqual(AppPhase.Finished, flow.Phase);
        }
    }
}
