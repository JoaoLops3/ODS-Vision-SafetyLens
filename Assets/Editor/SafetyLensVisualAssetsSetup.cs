#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Facens.SafetyLens.Editor
{
    /// <summary>
    /// Cria os assets visuais da RA que precisam existir antes do build:
    /// materiais em Resources (para os shaders entrarem no player), o prefab do capacete
    /// normalizado a partir do FBX em Assets/Models/EPI e a iluminação da cena.
    /// </summary>
    public static class SafetyLensVisualAssetsSetup
    {
        const string MaterialsFolder = "Assets/Resources/SafetyLens/Materials";
        const string PrefabsFolder = "Assets/Resources/SafetyLens/Prefabs";
        const string HelmetFbxPath = "Assets/Models/EPI/HardHat.fbx";
        const string HelmetPrefabAssetPath = PrefabsFolder + "/Helmet.prefab";
        const string CompressorFbxPath = "Assets/Models/Motor/air-compressor.fbx";
        const string CompressorPrefabAssetPath = PrefabsFolder + "/AirCompressor.prefab";
        const string CompressorTextureFolder = "Assets/Models/Motor/Textures";
        const string LightName = "SafetyLens Key Light";

        public static void EnsureAll(string scenePath)
        {
            EnsureMaterials();
            // Regenera: ArHelmetWidth muda com o layout HUD e o FBX não pode ficar com tamanho velho.
            EnsureHelmetPrefab(true);
            EnsureCompressorPrefab();
            EnsureSceneLighting(scenePath);
        }

        [MenuItem("SafetyLens/Regerar prefab do capacete (FBX)")]
        public static void RegenerateHelmetPrefab()
        {
            EnsureMaterials();
            EnsureHelmetPrefab(true);
            AssetDatabase.SaveAssets();
        }

        public static void EnsureMaterials()
        {
            Directory.CreateDirectory(MaterialsFolder);

            EnsureMaterial(SafetyLensArVisuals.SignMaterialPath, SafetyLensArVisuals.SignShader, m =>
            {
                m.color = Color.white;
                if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.15f);
            });
            EnsureMaterial(SafetyLensArVisuals.RingMaterialPath, SafetyLensArVisuals.TransparentShader, m =>
            {
                m.color = SafetyLensTheme.HazardAlertActive;
            });
            EnsureMaterial(SafetyLensArVisuals.HelmetMaterialPath, SafetyLensArVisuals.LitShader, m =>
            {
                m.color = SafetyLensTheme.HelmetBase;
                m.SetFloat("_Glossiness", 0.55f);
                m.SetFloat("_Metallic", 0.05f);
            });
            EnsureMaterial(SafetyLensArVisuals.FallbackMaterialPath, SafetyLensArVisuals.FallbackShader, m =>
            {
                m.color = SafetyLensTheme.HazardIdle;
            });
        }

        static void EnsureMaterial(string resourcePath, string shaderName, System.Action<Material> configure)
        {
            var assetPath = $"Assets/Resources/{resourcePath}.mat";
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[SafetyLens] Shader '{shaderName}' nao encontrado para {assetPath}.");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null && material.shader == shader) return;

            if (material == null)
            {
                material = new Material(shader);
                configure(material);
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else
            {
                material.shader = shader;
                configure(material);
                EditorUtility.SetDirty(material);
            }
            Debug.Log($"[SafetyLens] Material criado: {assetPath} ({shaderName}).");
        }

        /// <summary>
        /// O FBX baixado vem em escala/origem arbitrárias. Normalizamos por bounds para que o capacete
        /// tenha ArHelmetWidth de largura e apoie a base em y = 0, e salvamos como prefab em Resources.
        /// </summary>
        public static void EnsureHelmetPrefab(bool force)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(HelmetPrefabAssetPath);
            if (!force && existing != null)
            {
                // Prefab antigo pode ter vindo com Camera do Blender — limpa sem regenerar o mesh.
                if (existing.GetComponentInChildren<Camera>(true) != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(existing);
                    SafetyLensArVisuals.StripImportedSceneGadgets(instance);
                    PrefabUtility.SaveAsPrefabAsset(instance, HelmetPrefabAssetPath);
                    Object.DestroyImmediate(instance);
                    Debug.Log("[SafetyLens] Camera/Light importados removidos do prefab do capacete.");
                }
                return;
            }

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(HelmetFbxPath);
            if (fbx == null)
            {
                Debug.LogWarning($"[SafetyLens] {HelmetFbxPath} nao encontrado; o EPI usara o primitivo de fallback.");
                return;
            }

            Directory.CreateDirectory(PrefabsFolder);

            var root = new GameObject("Helmet");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                model.name = "Mesh";
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                // Camera/Light do Blender no FBX quebram a vista AR se entrarem no prefab.
                SafetyLensArVisuals.StripImportedSceneGadgets(model);

                var renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    Debug.LogWarning("[SafetyLens] FBX do capacete sem renderers; prefab nao gerado.");
                    return;
                }

                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);

                var footprint = Mathf.Max(bounds.size.x, bounds.size.z);
                if (footprint <= 1e-6f)
                {
                    Debug.LogWarning("[SafetyLens] FBX do capacete com bounds nulos; prefab nao gerado.");
                    return;
                }

                var scale = SafetyLensTheme.ArHelmetWidth / footprint;
                model.transform.localScale = Vector3.one * scale;
                // Após escalar, recentra no X/Z e apoia a base no plano do marcador.
                var center = bounds.center * scale;
                var bottom = bounds.min.y * scale;
                model.transform.localPosition = new Vector3(-center.x, -bottom, -center.z);

                var helmetMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    $"Assets/Resources/{SafetyLensArVisuals.HelmetMaterialPath}.mat");
                foreach (var r in renderers)
                {
                    if (helmetMaterial != null)
                    {
                        var mats = new Material[r.sharedMaterials.Length];
                        for (var i = 0; i < mats.Length; i++) mats[i] = helmetMaterial;
                        r.sharedMaterials = mats;
                    }
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    r.lightProbeUsage = LightProbeUsage.Off;
                    r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }

                foreach (var oldCollider in root.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(oldCollider);

                var size = bounds.size * scale;
                var collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, size.y * 0.5f, 0f);
                collider.size = size;

                PrefabUtility.SaveAsPrefabAsset(root, HelmetPrefabAssetPath, out var ok);
                if (ok)
                    Debug.Log($"[SafetyLens] Prefab do capacete gerado em {HelmetPrefabAssetPath} " +
                              $"({size.x * 100f:F1} x {size.y * 100f:F1} x {size.z * 100f:F1} cm).");
                else
                    Debug.LogError("[SafetyLens] Falha ao salvar o prefab do capacete.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Gera o prefab do compressor a partir do FBX, mantendo a textura importada.
        /// Trocar o material por uma cor chapada jogaria fora o modelo realista.
        /// </summary>
        public static void EnsureCompressorPrefab()
        {
            var fbx = PrepareCompressorImport();
            if (fbx == null)
            {
                Debug.LogWarning($"[SafetyLens] {CompressorFbxPath} nao encontrado; o QR usa o compressor de primitivas.");
                return;
            }

            Directory.CreateDirectory(PrefabsFolder);
            var root = new GameObject("AirCompressor");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                model.name = "Mesh";
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                SafetyLensArVisuals.StripImportedSceneGadgets(model);

                var renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    Debug.LogWarning("[SafetyLens] FBX do compressor sem renderers; prefab nao gerado.");
                    return;
                }

                for (var i = 0; i < renderers.Length; i++)
                {
                    renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                    renderers[i].receiveShadows = false;
                    renderers[i].lightProbeUsage = LightProbeUsage.Off;
                    renderers[i].reflectionProbeUsage = ReflectionProbeUsage.Off;
                    var shaderName = renderers[i].sharedMaterial != null ? renderers[i].sharedMaterial.shader.name : "(sem material)";
                    Debug.Log($"[SafetyLens] Compressor renderer '{renderers[i].name}' shader '{shaderName}'.");
                }

                foreach (var oldCollider in root.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(oldCollider);

                PrefabUtility.SaveAsPrefabAsset(root, CompressorPrefabAssetPath, out var ok);
                if (ok)
                    Debug.Log($"[SafetyLens] Prefab do compressor gerado em {CompressorPrefabAssetPath}.");
                else
                    Debug.LogError("[SafetyLens] Falha ao salvar o prefab do compressor.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Extrai a textura embutida e importa o material para fora do FBX,
        /// senão o player iOS não leva a cor do tanque.
        /// </summary>
        static GameObject PrepareCompressorImport()
        {
            var importer = AssetImporter.GetAtPath(CompressorFbxPath) as ModelImporter;
            if (importer == null) return null;

            Directory.CreateDirectory(CompressorTextureFolder);
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.None;
            // As marcas leem os vértices para sentar na casca. Sem isto o player devolve a malha vazia.
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.ExtractTextures(CompressorTextureFolder);
            importer.SaveAndReimport();
            importer = AssetImporter.GetAtPath(CompressorFbxPath) as ModelImporter;
            if (importer == null) return null;
            importer.SearchAndRemapMaterials(
                ModelImporterMaterialName.BasedOnMaterialName,
                ModelImporterMaterialSearch.Everywhere);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(CompressorFbxPath);
        }

        /// <summary>
        /// A cena nasceu sem luz (primitivos unlit). O capacete usa Standard, então precisa de uma
        /// direcional suave + ambiente plano para ler como objeto sólido sobre a imagem da câmera.
        /// </summary>
        public static void EnsureSceneLighting(string scenePath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null) return;

            var active = EditorSceneManager.GetActiveScene();
            var openedHere = active.path != scenePath;
            var scene = openedHere
                ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive)
                : active;

            var hasLight = false;
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.GetComponentInChildren<Light>(true) != null) { hasLight = true; break; }
            }

            if (!hasLight)
            {
                // RenderSettings grava na cena ativa: ativa a nossa enquanto configura o ambiente.
                var previousActive = SceneManager.GetActiveScene();
                if (previousActive != scene) SceneManager.SetActiveScene(scene);

                var lightObject = new GameObject(LightName);
                if (lightObject.scene != scene) SceneManager.MoveGameObjectToScene(lightObject, scene);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = 1.05f;
                light.shadows = LightShadows.None;
                lightObject.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.58f, 0.60f, 0.64f, 1f);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (previousActive != scene && previousActive.IsValid()) SceneManager.SetActiveScene(previousActive);
                Debug.Log("[SafetyLens] Luz direcional adicionada a cena para os modelos 3D.");
            }

            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }

    }
}
#endif
