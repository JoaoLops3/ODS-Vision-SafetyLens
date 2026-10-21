using NUnit.Framework;
using UnityEngine;

namespace Facens.SafetyLens.Tests
{
    public sealed class SafetyLensQrMachineTests
    {
        [Test]
        public void Spots_SitOnThreeDifferentFaces()
        {
            var spots = SafetyLensQrMachineCatalog.CreateSpots();
            Assert.AreEqual(3, spots.Length);

            var front = spots[0].LocalOutwardNormal;
            var side = spots[1].LocalOutwardNormal;
            var back = spots[2].LocalOutwardNormal;

            Assert.AreEqual(-1f, Vector3.Dot(front.normalized, back.normalized), 1e-4f);
            Assert.AreEqual(0f, Vector3.Dot(front.normalized, side.normalized), 1e-4f);
            Assert.AreEqual("Saída de ar quente", spots[0].WhatItIs);
            Assert.AreEqual("Queimar a mão", spots[0].WhatCanHappen);
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
