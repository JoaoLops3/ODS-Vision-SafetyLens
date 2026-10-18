using System;
using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Carrega e valida o cenário em Resources. Falha cedo com mensagem acionável —
    /// JSON inválido ou schema quebrado não sobe sessão silenciosa.
    /// </summary>
    public static class SafetyLensScenarioLoader
    {
        /// <summary>
        /// Lê <see cref="SafetyLensAppConstants.ScenarioResourceName"/>, desserializa e valida.
        /// </summary>
        /// <param name="expectedTargetName">Nome obrigatório do Image Target principal.</param>
        /// <exception cref="InvalidOperationException">Asset ausente, JSON nulo/ilegível ou schema inválido.</exception>
        public static SafetyLensScenario LoadFromResources(string expectedTargetName)
        {
            TextAsset asset;
            try
            {
                asset = Resources.Load<TextAsset>(SafetyLensAppConstants.ScenarioResourceName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Falha ao ler o cenario em Resources/{SafetyLensAppConstants.ScenarioResourceName}.",
                    ex);
            }

            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                throw new InvalidOperationException(
                    $"Cenario nao encontrado ou vazio: {SafetyLensAppConstants.ScenarioAssetPath} " +
                    $"(Resources/{SafetyLensAppConstants.ScenarioResourceName}).");
            }

            SafetyLensScenario scenario;
            try
            {
                scenario = JsonUtility.FromJson<SafetyLensScenario>(asset.text);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"JSON do cenario invalido em {SafetyLensAppConstants.ScenarioAssetPath}: {ex.Message}",
                    ex);
            }

            if (scenario == null)
            {
                throw new InvalidOperationException(
                    $"JSON do cenario desserializou nulo: {SafetyLensAppConstants.ScenarioAssetPath}.");
            }

            var errors = SafetyLensScenarioValidator.Validate(scenario, expectedTargetName);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Cenario SafetyLens invalido: " + string.Join(" ", errors));
            }

            return scenario;
        }
    }
}
