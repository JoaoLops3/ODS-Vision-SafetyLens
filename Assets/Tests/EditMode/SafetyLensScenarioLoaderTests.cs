using NUnit.Framework;

namespace Facens.SafetyLens.Tests
{
    public sealed class SafetyLensScenarioLoaderTests
    {
        [Test]
        public void LoadFromResources_ReturnsValidDemoScenario()
        {
            var scenario = SafetyLensScenarioLoader.LoadFromResources(
                SafetyLensAppConstants.ExpectedTargetName);

            Assert.IsNotNull(scenario);
            Assert.AreEqual(SafetyLensAppConstants.ExpectedTargetName, scenario.targetName);
            Assert.GreaterOrEqual(scenario.hazards.Length, 1);
            Assert.GreaterOrEqual(scenario.quiz.Length, 1);
            Assert.AreEqual(0, SafetyLensScenarioValidator.Validate(scenario, scenario.targetName).Count);
        }
    }
}
