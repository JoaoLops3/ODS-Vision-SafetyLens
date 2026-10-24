using NUnit.Framework;
using UnityEngine;

namespace Facens.SafetyLens.Tests
{
    public sealed class SafetyLensQrMachineTests
    {
        [Test]
        public void Spots_KeepTheSideCapOnly()
        {
            var spots = SafetyLensQrMachineCatalog.CreateSpots();
            Assert.AreEqual(1, spots.Length);
            Assert.AreEqual("Tampa lateral", spots[0].Name);
            Assert.AreEqual("Fecha a ponta do tanque, no lado sem a roda.", spots[0].WhatItIs);
            Assert.AreEqual("Com pressão, a tampa solta e sai com força.", spots[0].WhatCanHappen);
            Assert.AreEqual(Vector3.right, spots[0].LocalOutwardNormal);
        }

        [Test]
        public void Motor_SaysBurnRisk()
        {
            var spot = SafetyLensQrMachineCatalog.MotorSpot();
            Assert.AreEqual("Motor", spot.Name);
            Assert.AreEqual("Gira a bomba. A tampa cobre a carcaça.", spot.WhatItIs);
            Assert.AreEqual("A carcaça esquenta e queima a mão.", spot.WhatCanHappen);
        }

        [Test]
        public void Gauge_WarnsAboutMissedPressure()
        {
            var spot = SafetyLensQrMachineCatalog.GaugeSpot();
            Assert.AreEqual("Medidor de pressão", spot.Name);
            Assert.AreEqual("Mostra quanta pressão tem o ar no tanque.", spot.WhatItIs);
            Assert.AreEqual("Se a leitura falha, a pressão alta passa batida.", spot.WhatCanHappen);
        }

        [Test]
        public void AirOutlet_SaysWhereTheAirLeaves()
        {
            var spot = SafetyLensQrMachineCatalog.AirOutletSpot();
            Assert.AreEqual("Saída de ar", spot.Name);
            Assert.AreEqual("Por aqui o ar comprimido sai da bomba.", spot.WhatItIs);
            Assert.AreEqual("O jato no olho fere. A mangueira solta chicoteia.", spot.WhatCanHappen);
        }

        [Test]
        public void FaceTowardViewer_AcceptsFront_RejectsSideAndBack()
        {
            var toViewer = Vector3.back;
            var front = SafetyLensQrMachineCatalog.DotTowardViewer(Vector3.back, toViewer);
            var side = SafetyLensQrMachineCatalog.DotTowardViewer(Vector3.right, toViewer);
            var back = SafetyLensQrMachineCatalog.DotTowardViewer(Vector3.forward, toViewer);

            Assert.IsTrue(SafetyLensQrMachineCatalog.IsOnViewerFace(front, front));
            Assert.IsFalse(SafetyLensQrMachineCatalog.IsOnViewerFace(side, front));
            Assert.IsFalse(SafetyLensQrMachineCatalog.IsOnViewerFace(back, front));
        }

        [Test]
        public void SameFace_BothPointsStayOpen()
        {
            const float best = 1f;
            Assert.IsTrue(SafetyLensQrMachineCatalog.IsOnViewerFace(best, best));
            Assert.IsTrue(SafetyLensQrMachineCatalog.IsOnViewerFace(best - 0.05f, best));
        }

        [Test]
        public void EdgeOnView_OpensNothing()
        {
            const float weak = 0.2f;
            Assert.Less(weak, SafetyLensQrMachineCatalog.FacingMinDot);
            Assert.IsFalse(SafetyLensQrMachineCatalog.IsOnViewerFace(weak, weak));
        }

        [Test]
        public void ReadPoint_StaysGreenEvenFromTheSide()
        {
            Assert.AreEqual(SafetyLensTheme.HazardDone, SafetyLensQrMachinePoint.AppearanceColor(true, false));
            Assert.AreEqual(SafetyLensTheme.HazardDone, SafetyLensQrMachinePoint.AppearanceColor(true, true));
        }

        [Test]
        public void UnreadPoint_IsYellowInFront_GrayOnTheSide()
        {
            Assert.AreEqual(SafetyLensTheme.Brand, SafetyLensQrMachinePoint.AppearanceColor(false, true));
            Assert.AreEqual(SafetyLensTheme.HazardIdle, SafetyLensQrMachinePoint.AppearanceColor(false, false));
        }

        [Test]
        public void NameScale_KeepsOneLineSmallerThanThePoint()
        {
            const float glyphPixels = 48f;
            const float characterSize = 0.5f;
            const float parentScale = 0.336f;
            var desired = SafetyLensTheme.QrMachinePointSize * SafetyLensTheme.QrMachineLabelToPoint;
            var scale = SafetyLensQrMachinePoint.LabelLocalScale(glyphPixels, characterSize, parentScale, desired);
            var worldLine = glyphPixels * characterSize * SafetyLensQrMachinePoint.TextMeshPixelToLocal * scale * parentScale;

            Assert.AreEqual(desired, worldLine, 0.0001f);
            Assert.Less(worldLine, SafetyLensTheme.QrMachinePointSize);
            Assert.Greater(worldLine, SafetyLensTheme.QrMachinePointSize * 0.4f);
            Assert.Less(scale, 0.2f);
        }

        [Test]
        public void LongName_BreaksOnTheFirstSpace()
        {
            Assert.AreEqual("Motor", SafetyLensQrMachinePoint.FormatLabel("Motor"));
            Assert.AreEqual("Saída de ar", SafetyLensQrMachinePoint.FormatLabel("Saída de ar"));
            Assert.AreEqual("Medidor\nde pressão", SafetyLensQrMachinePoint.FormatLabel("Medidor de pressão"));
        }

        [Test]
        public void NullVectors_DoNotFace()
        {
            var dot = SafetyLensQrMachineCatalog.DotTowardViewer(Vector3.zero, Vector3.back);
            Assert.Less(dot, SafetyLensQrMachineCatalog.FacingMinDot);
        }
    }
}
