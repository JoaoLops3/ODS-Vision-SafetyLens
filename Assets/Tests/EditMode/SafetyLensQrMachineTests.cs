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
            Assert.AreEqual("Tampa lateral", spots[0].WhatItIs);
            Assert.AreEqual("Uma peça pode soltar", spots[0].WhatCanHappen);
            Assert.AreEqual(Vector3.right, spots[0].LocalOutwardNormal);
        }

        [Test]
        public void Motor_SaysBurnRisk()
        {
            var spot = SafetyLensQrMachineCatalog.MotorSpot();
            Assert.AreEqual("Motor", spot.WhatItIs);
            Assert.AreEqual("Chance de queimadura", spot.WhatCanHappen);
        }

        [Test]
        public void AirOutlet_SaysWhereTheAirLeaves()
        {
            var spot = SafetyLensQrMachineCatalog.AirOutletSpot();
            Assert.AreEqual("Saída de ar", spot.WhatItIs);
            Assert.IsEmpty(spot.WhatCanHappen);
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
        public void NullVectors_DoNotFace()
        {
            var dot = SafetyLensQrMachineCatalog.DotTowardViewer(Vector3.zero, Vector3.back);
            Assert.Less(dot, SafetyLensQrMachineCatalog.FacingMinDot);
        }
    }
}
