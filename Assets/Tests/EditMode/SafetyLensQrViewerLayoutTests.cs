using NUnit.Framework;
using UnityEngine;

namespace Facens.SafetyLens.Tests
{
    /// <summary>Layout do viewer QR (models fora do Image Target).</summary>
    public sealed class SafetyLensQrViewerLayoutTests
    {
        [Test]
        public void LocalPosition_SingleHazard_CenteredWithLift()
        {
            var pos = SafetyLensQrViewerLayout.LocalPositionForHazard(0, 1);
            Assert.AreEqual(0f, pos.x, 1e-5f);
            Assert.AreEqual(SafetyLensTheme.QrViewerLift, pos.y, 1e-5f);
            Assert.AreEqual(0f, pos.z, 1e-5f);
        }

        [Test]
        public void LocalPosition_ThreeHazards_SpreadLeftCenterRight()
        {
            var left = SafetyLensQrViewerLayout.LocalPositionForHazard(0, 3);
            var mid = SafetyLensQrViewerLayout.LocalPositionForHazard(1, 3);
            var right = SafetyLensQrViewerLayout.LocalPositionForHazard(2, 3);

            Assert.AreEqual(-SafetyLensTheme.QrViewerSpreadX, left.x, 1e-5f);
            Assert.AreEqual(0f, mid.x, 1e-5f);
            Assert.AreEqual(SafetyLensTheme.QrViewerSpreadX, right.x, 1e-5f);
            Assert.AreEqual(SafetyLensTheme.QrViewerLift, left.y, 1e-5f);
        }

        [Test]
        public void LocalPosition_EmptyCount_ReturnsZero()
        {
            var pos = SafetyLensQrViewerLayout.LocalPositionForHazard(0, 0);
            Assert.AreEqual(Vector3.zero, pos);
        }
    }
}
