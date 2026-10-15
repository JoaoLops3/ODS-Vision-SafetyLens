#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.ARKit;
using UnityEditor.XR.ARSubsystems;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using Facens.SafetyLens;

namespace Facens.SafetyLens.Editor
{
    [InitializeOnLoad]
    public static class SafetyLensProjectSetup
    {
        const string ScenePath = "Assets/Scenes/SafetyLensAR.unity";
        const string MarkerPath = "Assets/Markers/safetylens-equipment-target.png";
        const string LibraryPath = "Assets/ReferenceImages/SafetyLensReferenceImageLibrary.asset";
        const string TargetName = "SafetyLensEquipmentTarget";
        const string SetupFlag = "Facens.SafetyLens.SetupComplete.v1";

        static readonly string[] ArBackgroundShaders =
        {
            "Unlit/ARKitBackground",
            "Unlit/ARKitBackground/AfterOpaques",
            "Unlit/ARCoreBackground",
            "Unlit/ARCoreBackground/AfterOpaques",
            "SafetyLens/SignUnlit",
            "Sprites/Default",
            "Unlit/Transparent Cutout",
        };

        static SafetyLensProjectSetup() => EditorApplication.delayCall += AutoSetup;

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(SetupFlag, false)) return;
            try
            {
                ConfigureProject();
                SessionState.SetBool(SetupFlag, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SafetyLens] Falha na configuracao automatica: {ex}");
            }
        }

        [MenuItem("SafetyLens/Configurar projeto completo")]
        public static void ConfigureProject()
        {
            ConfigureAndroid();
            ConfigureIos();
            EnsureArBackgroundShadersIncluded();
            var library = EnsureImageLibrary();
            EnsureScene(library, false);
            SafetyLensVisualAssetsSetup.EnsureAll(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SafetyLens] Projeto configurado. Use SafetyLens > Validar projeto; depois Build iOS (Xcode) ou Build and Run Android.");
        }

        [MenuItem("SafetyLens/Validar projeto")]
        public static void ValidateProject()
        {
            ConfigureProject();
            EditorSceneManager.OpenScene(ScenePath);
            var errors = 0;
            errors += RequireObject<ARSession>("ARSession");
            errors += RequireObject<XROrigin>("XROrigin");
            errors += RequireObject<ARTrackedImageManager>("ARTrackedImageManager");
            errors += RequireObject<SafetyLensSessionController>("SafetyLensSessionController");
            errors += RequireObject<ARCameraManager>("ARCameraManager");
            errors += RequireObject<ARCameraBackground>("ARCameraBackground");
            errors += RequireObject<SafetyLensStatusOverlay>("SafetyLensStatusOverlay");
            errors += RequireCameraClearFlags();
            errors += RequireSingleArCamera();

            var manager = UnityEngine.Object.FindFirstObjectByType<ARTrackedImageManager>();
            if (manager == null || manager.referenceLibrary == null || manager.referenceLibrary.count != 1)
            {
                Debug.LogError("[SafetyLens] Reference Image Library ausente ou invalida.");
                errors++;
            }
            else if (manager.referenceLibrary[0].name != TargetName)
            {
                Debug.LogError(
                    $"[SafetyLens] Nome do Image Target na library ('{manager.referenceLibrary[0].name}') " +
                    $"diferente de '{TargetName}'.");
                errors++;
            }

            var scenario = Resources.Load<TextAsset>("safetylens_scenario");
            if (scenario == null)
            {
                Debug.LogError("[SafetyLens] Resources/safetylens_scenario.json ausente.");
                errors++;
            }
            else
            {
                try
                {
                    var parsed = JsonUtility.FromJson<SafetyLensScenario>(scenario.text);
                    var schemaErrors = SafetyLensScenarioValidator.Validate(parsed, TargetName);
                    foreach (var err in schemaErrors)
                    {
                        Debug.LogError($"[SafetyLens] {err}");
                        errors++;
                    }

                    if (schemaErrors.Count == 0)
                        errors += ValidateHazardOverlays(parsed);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SafetyLens] Falha ao ler safetylens_scenario.json: {ex.Message}");
                    errors++;
                }
            }

            // Android: ARCore obrigatorio para Build and Run Android.
            if (!HasXrLoader(BuildTargetGroup.Android, "UnityEngine.XR.ARCore.ARCoreLoader"))
            {
                Debug.LogError("[SafetyLens] [Android] ARCore Loader nao habilitado.");
                errors++;
            }

            // iOS: ARKit + Metal + privacidade da camera — sem isso o iPhone falha mesmo com "VALIDACAO OK" antiga.
            if (!HasXrLoader(BuildTargetGroup.iOS, "UnityEngine.XR.ARKit.ARKitLoader"))
            {
                Debug.LogError("[SafetyLens] [iOS] ARKit Loader nao habilitado.");
                errors++;
            }

            var iosApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);
            var metalOnly = iosApis != null && iosApis.Length == 1 && iosApis[0] == GraphicsDeviceType.Metal;
            if (!metalOnly)
            {
                Debug.LogError("[SafetyLens] [iOS] Graphics API deve ser apenas Metal (ARKitBackground).");
                errors++;
            }

            if (string.IsNullOrWhiteSpace(PlayerSettings.iOS.cameraUsageDescription))
            {
                Debug.LogError("[SafetyLens] [iOS] NSCameraUsageDescription / cameraUsageDescription ausente.");
                errors++;
            }

            var arKit = ARKitSettings.GetOrCreateSettings();
            if (arKit.requirement != ARKitSettings.Requirement.Required)
            {
                Debug.LogError(
                    "[SafetyLens] [iOS] ARKitSettings.requirement esta Optional. " +
                    "Deve ser Required (app so instala em devices com ARKit).");
                errors++;
            }

            if (!ReadIosRequiresArKitSupport())
            {
                Debug.LogError(
                    "[SafetyLens] [iOS] PlayerSettings.iOS.requiresARKitSupport esta false. " +
                    "Deve ser true.");
                errors++;
            }

            if (errors == 0)
            {
                Debug.Log(
                    "[SafetyLens] VALIDACAO OK: cena + library + cenario UV; " +
                    "Android=ARCore; iOS=ARKit+Required+Metal+camera privacy.");
            }
            else
                Debug.LogError($"[SafetyLens] VALIDACAO FALHOU: {errors} problema(s).");
        }

        static int RequireObject<T>(string label) where T : UnityEngine.Object
        {
            if (UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include) != null) return 0;
            Debug.LogError($"[SafetyLens] Componente ausente: {label}");
            return 1;
        }

        /// <summary>
        /// EnsureScene só configura o Clear Flags da Main Camera na PRIMEIRA vez que a cena é criada.
        /// Se ele for alterado depois, a câmera mostra o Skybox padrão (céu degradê) quando o
        /// ARCameraBackground não cobre o frame. Corrige automaticamente aqui.
        /// </summary>
        static int RequireCameraClearFlags()
        {
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            if (camera == null)
            {
                Debug.LogError("[SafetyLens] Main Camera ausente na cena.");
                return 1;
            }

            var dirty = false;
            if (camera.clearFlags != CameraClearFlags.SolidColor || camera.backgroundColor != Color.black)
            {
                Debug.LogWarning(
                    $"[SafetyLens] Main Camera.clearFlags estava '{camera.clearFlags}' (esperado SolidColor + preto). " +
                    "Isso deixa o Skybox padrão visível sempre que o ARCameraBackground não cobre o frame. Corrigindo.");
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                EditorUtility.SetDirty(camera);
                dirty = true;
            }

            // Skybox no RenderSettings ainda “vaza” se algo voltar Clear Flags para Skybox.
            if (RenderSettings.skybox != null)
            {
                Debug.LogWarning("[SafetyLens] Removendo Skybox do RenderSettings (evita céu degradê no iPhone).");
                RenderSettings.skybox = null;
                dirty = true;
            }

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
                EditorSceneManager.SaveScene(camera.gameObject.scene);
            }
            return 0;
        }

        /// <summary>
        /// Sem esses shaders no Always Included, o IL2CPP stripa o material do ARCameraBackground
        /// e o feed da câmera some — sobra o clear/skybox, exatamente o "degradê" no reconhecimento.
        /// </summary>
        static void EnsureArBackgroundShadersIncluded()
        {
            var graphics = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            if (graphics == null) return;

            var so = new SerializedObject(graphics);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) return;

            var present = new HashSet<string>();
            for (var i = 0; i < list.arraySize; i++)
            {
                var shader = list.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (shader != null) present.Add(shader.name);
            }

            var added = 0;
            foreach (var name in ArBackgroundShaders)
            {
                if (present.Contains(name)) continue;
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning($"[SafetyLens] Shader AR de fundo nao encontrado: {name}");
                    continue;
                }

                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                added++;
            }

            if (added > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(graphics);
                Debug.Log($"[SafetyLens] {added} shader(s) de fundo AR incluidos em Always Included Shaders.");
            }
        }

        [MenuItem("SafetyLens/Build Android APK")]
        public static void BuildAndroidApk() => BuildAndroid(false);

        [MenuItem("SafetyLens/Build and Run Android")]
        public static void BuildAndRunAndroid() => BuildAndroid(true);

        [MenuItem("SafetyLens/Build iOS (Xcode)")]
        public static void BuildIosXcode() => BuildIos();

        [MenuItem("SafetyLens/Abrir cena principal")]
        public static void OpenMainScene()
        {
            ConfigureProject();
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("SafetyLens/Recriar cena baseline (sobrescreve)")]
        public static void RecreateScene()
        {
            if (!EditorUtility.DisplayDialog("SafetyLens - Recriar cena", "Sobrescrever a cena baseline?", "Recriar", "Cancelar"))
                return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                AssetDatabase.DeleteAsset(ScenePath);
            EnsureScene(EnsureImageLibrary(), true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static XRReferenceImageLibrary EnsureImageLibrary()
        {
            var existing = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
            if (existing != null && existing.count == 1) return existing;

            Directory.CreateDirectory("Assets/ReferenceImages");
            if (existing != null) AssetDatabase.DeleteAsset(LibraryPath);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerPath);
            if (texture == null)
                throw new InvalidOperationException($"Marcador nao encontrado: {MarkerPath}");

            var library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
            library.name = "SafetyLensReferenceImageLibrary";
            AssetDatabase.CreateAsset(library, LibraryPath);
            XRReferenceImageLibraryExtensions.Add(library);
            XRReferenceImageLibraryExtensions.SetName(library, 0, TargetName);
            XRReferenceImageLibraryExtensions.SetSpecifySize(library, 0, true);
            XRReferenceImageLibraryExtensions.SetSize(library, 0, new Vector2(0.20f, 0.133333f));
            XRReferenceImageLibraryExtensions.SetTexture(library, 0, texture, false);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            return library;
        }

        static void BuildAndroid(bool autoRun)
        {
            ConfigureProject();
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Nao foi possivel selecionar Android.");

            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/SafetyLens.apk",
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = autoRun ? BuildOptions.AutoRunPlayer : BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException($"Build Android falhou: {report.summary.result}");

            Debug.Log($"[SafetyLens] APK gerado em {options.locationPathName}");
        }

        /// <summary>
        /// Gera o projeto Xcode em Builds/ (mesmo destino usado no teste no iPhone).
        /// Pode ser chamado pelo menu ou via -executeMethod em batchmode.
        /// </summary>
        public static void BuildIos()
        {
            ConfigureProject();
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new InvalidOperationException("Nao foi possivel selecionar iOS.");

            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds",
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException($"Build iOS falhou: {report.summary.result}");

            Debug.Log("[SafetyLens] Projeto Xcode gerado em Builds/. Abra Unity-iPhone.xcodeproj e rode no iPhone.");
        }

        static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "Facens";
            PlayerSettings.productName = "SafetyLens";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "edu.facens.safetylens");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

            var perTarget = EnsureXRGeneralSettings();
            var android = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (android == null || android.AssignedSettings == null)
                throw new InvalidOperationException("Falha nas configuracoes XR Android.");

            android.InitManagerOnStart = true;
            EditorUtility.SetDirty(android);

            var assigned = XRPackageMetadataStore.AssignLoader(
                android.AssignedSettings,
                "UnityEngine.XR.ARCore.ARCoreLoader",
                BuildTargetGroup.Android);

            var present = false;
            foreach (var loader in android.AssignedSettings.activeLoaders)
            {
                if (loader != null && loader.GetType().FullName == "UnityEngine.XR.ARCore.ARCoreLoader")
                    present = true;
            }

            if (!assigned && !present)
                throw new InvalidOperationException("Nao foi possivel habilitar ARCore Loader.");

            var arCore = ARCoreSettings.GetOrCreateSettings();
            arCore.requirement = ARCoreSettings.Requirement.Required;
            EditorUtility.SetDirty(arCore);
        }

        static void ConfigureIos()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "edu.facens.safetylens");
            PlayerSettings.iOS.targetOSVersionString = "12.0";
            // Obrigatorio para ARKit / App Store privacy (camera).
            PlayerSettings.iOS.cameraUsageDescription =
                "O SafetyLens usa a camera para Realidade Aumentada e identificacao de riscos industriais.";
            // ARKitBackground exige Metal; OpenGL no iPhone quebra o feed da câmera.
            EnsureIosMetalOnly();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);

            var perTarget = EnsureXRGeneralSettings();
            if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.iOS))
                perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.iOS);
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.iOS))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);

            var ios = perTarget.SettingsForBuildTarget(BuildTargetGroup.iOS);
            if (ios?.AssignedSettings == null) return;

            ios.InitManagerOnStart = true;
            // Sem automatic load/run o ARKit sobe pela metade: sessão “ativa” (ponto verde)
            // mas ARCameraBackground fica preto.
            ios.AssignedSettings.automaticLoading = true;
            ios.AssignedSettings.automaticRunning = true;
            EditorUtility.SetDirty(ios);
            EditorUtility.SetDirty(ios.AssignedSettings);
            XRPackageMetadataStore.AssignLoader(
                ios.AssignedSettings,
                "UnityEngine.XR.ARKit.ARKitLoader",
                BuildTargetGroup.iOS);

            // Paridade com ConfigureAndroid (ARCore Required): Info.plist ganha UIRequiredDeviceCapabilities=arkit.
            var arKit = ARKitSettings.GetOrCreateSettings();
            if (ARKitSettings.currentSettings == null)
                ARKitSettings.currentSettings = arKit;
            arKit.requirement = ARKitSettings.Requirement.Required;
            EditorUtility.SetDirty(arKit);

            // API Unity e internal em algumas builds — reflection mantém ProjectSettings.iOSRequireARKit=1.
            SetIosRequiresArKitSupport(true);
        }

        /// <summary>
        /// PlayerSettings.iOS.requiresARKitSupport e internal no Editor; lemos via reflection.
        /// </summary>
        static bool ReadIosRequiresArKitSupport()
        {
            var prop = typeof(PlayerSettings.iOS).GetProperty(
                "requiresARKitSupport",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop == null) return false;
            return prop.GetValue(null) is true;
        }

        static void SetIosRequiresArKitSupport(bool value)
        {
            var prop = typeof(PlayerSettings.iOS).GetProperty(
                "requiresARKitSupport",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop == null || !prop.CanWrite)
            {
                Debug.LogWarning(
                    "[SafetyLens] PlayerSettings.iOS.requiresARKitSupport indisponivel; " +
                    "marque Requires ARKit support no Inspector do Player Settings.");
                return;
            }

            prop.SetValue(null, value);
        }

        static XRGeneralSettingsPerBuildTarget EnsureXRGeneralSettings()
        {
            XRGeneralSettingsPerBuildTarget perTarget = null;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget);
            if (perTarget == null)
            {
                Directory.CreateDirectory("Assets/XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                perTarget.name = "XRGeneralSettingsPerBuildTarget";
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }

            if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android))
                perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

            EditorUtility.SetDirty(perTarget);
            AssetDatabase.SaveAssets();
            return perTarget;
        }

        static XRGeneralSettings GetAndroidXRSettings()
        {
            XRGeneralSettingsPerBuildTarget p = null;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out p);
            return p?.SettingsForBuildTarget(BuildTargetGroup.Android);
        }

        static bool HasXrLoader(BuildTargetGroup group, string loaderTypeName)
        {
            XRGeneralSettingsPerBuildTarget perTarget = null;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget);
            var settings = perTarget?.SettingsForBuildTarget(group);
            if (settings?.AssignedSettings == null) return false;

            foreach (var loader in settings.AssignedSettings.activeLoaders)
            {
                if (loader != null && loader.GetType().FullName == loaderTypeName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Metal-only no iOS. Evita SetUseDefault(false) antes da lista — isso zera as APIs
        /// e o Editor loga "empty graphics device types list".
        /// </summary>
        static void EnsureIosMetalOnly()
        {
            var current = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);
            var usingDefault = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.iOS);
            var metalOnly = !usingDefault
                            && current != null
                            && current.Length == 1
                            && current[0] == GraphicsDeviceType.Metal;
            if (metalOnly) return;

            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.iOS))
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
        }

        static void EnsureScene(XRReferenceImageLibrary library, bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EnsureSceneInBuildSettings();
                EnsureArSessionStartsInactive();
                EnsureCameraClearFlagsInScene();
                EnsureSceneHasSingleArCamera();
                return;
            }

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sessionObject = new GameObject("AR Session");
            sessionObject.AddComponent<ARSession>();
            sessionObject.AddComponent<ARInputManager>();
            // A câmera só liga no botão "Abrir câmera" (SafetyLensSessionController.SetCameraActive).
            sessionObject.SetActive(false);

            var originObject = new GameObject("XR Origin (Mobile AR)");
            var origin = originObject.AddComponent<XROrigin>();

            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(originObject.transform, false);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(cameraOffset.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<ARCameraManager>();
            cameraObject.AddComponent<ARCameraBackground>();
#pragma warning disable CS0618
            cameraObject.AddComponent<ARPoseDriver>();
#pragma warning restore CS0618

            SetXROriginReference(origin, "Camera", "m_Camera", camera);
            SetXROriginReference(origin, "CameraFloorOffsetObject", "m_CameraFloorOffsetObject", cameraOffset);

            var imageManager = originObject.AddComponent<ARTrackedImageManager>();
            imageManager.referenceLibrary = library;
            imageManager.requestedMaxNumberOfMovingImages = 1;
            originObject.AddComponent<SafetyLensSessionController>();
            new GameObject("SafetyLens Status Overlay").AddComponent<SafetyLensStatusOverlay>();

            RenderSettings.skybox = null;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings();
            EditorSceneManager.OpenScene(ScenePath);
        }

        /// <summary>
        /// Mesma correção de RequireCameraClearFlags, mas abre a cena se necessário
        /// (usada em ConfigureProject / Build, não só em Validar).
        /// </summary>
        static void EnsureCameraClearFlagsInScene()
        {
            var active = EditorSceneManager.GetActiveScene();
            var openedHere = active.path != ScenePath;
            var scene = openedHere
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : active;

            if (openedHere) SceneManager.SetActiveScene(scene);
            RequireCameraClearFlags();
            if (openedHere)
            {
                if (active.IsValid()) SceneManager.SetActiveScene(active);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// O FBX do capacete (e qualquer modelo arrastado pra cena) traz Camera do Blender.
        /// Uma segunda Camera com SolidColor preto pinta por cima do ARCameraBackground —
        /// sintoma: ponto verde iOS + tela preta + só o mesh do capacete.
        /// </summary>
        static void EnsureSceneHasSingleArCamera()
        {
            var active = EditorSceneManager.GetActiveScene();
            var openedHere = active.path != ScenePath;
            var scene = openedHere
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : active;

            if (openedHere) SceneManager.SetActiveScene(scene);
            RequireSingleArCamera();
            if (openedHere)
            {
                if (active.IsValid()) SceneManager.SetActiveScene(active);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static int RequireSingleArCamera()
        {
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var removed = 0;
            foreach (var cam in cameras)
            {
                if (cam == null) continue;
                if (cam.CompareTag("MainCamera") || cam.GetComponent<ARCameraManager>() != null)
                    continue;

                Debug.LogWarning(
                    $"[SafetyLens] Removendo Camera estranha '{GetPath(cam.transform)}' " +
                    "(FBX/Blender na cena quebra o feed ARKit).");
                UnityEngine.Object.DestroyImmediate(cam.gameObject);
                removed++;
            }

            // Prefab HardHat solto na raiz (sem Camera se importCameras=0) ainda polui a vista.
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root == null) continue;
                var name = root.name;
                if (!name.StartsWith("HardHat", StringComparison.OrdinalIgnoreCase)
                    && !name.Equals("Hard Hat", StringComparison.OrdinalIgnoreCase))
                    continue;

                Debug.LogWarning($"[SafetyLens] Removendo '{name}' da cena — capacete só existe via Resources em runtime.");
                UnityEngine.Object.DestroyImmediate(root);
                removed++;
            }

            if (removed > 0)
            {
                var scene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            cameras = UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (cameras.Length != 1)
            {
                Debug.LogError(
                    $"[SafetyLens] Esperada 1 Camera (Main Camera AR); encontradas {cameras.Length}.");
                return 1;
            }
            return 0;
        }

        static string GetPath(Transform t)
        {
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        /// <summary>
        /// Placa/anel: meia-altura ativa + deslocamento de v na direção do rótulo (abaixo do círculo)
        /// ≤ distância centro→topo do texto. Capacete: footprint ativo &lt; diâmetro do C.
        /// </summary>
        static int ValidateHazardOverlays(SafetyLensScenario scenario)
        {
            var errors = 0;
            var pulse = 1f + SafetyLensTheme.ArPulseAmplitude;
            foreach (var h in scenario.hazards)
            {
                var dropTowardLabel = (SafetyLensTheme.ArLetterCenterV - h.v) * SafetyLensTheme.ArMarkerHeight;
                if (h.IsAlert)
                {
                    errors += ValidateOverlayFitsLetter(
                        $"Placa {h.id}",
                        SafetyLensTheme.ArSignWidth * SafetyLensTheme.ArActiveScale,
                        dropTowardLabel);
                    errors += ValidateOverlayFitsLetter(
                        $"Anel {h.id}",
                        SafetyLensTheme.ArSignRingDiameter * SafetyLensTheme.ArActiveScale * pulse,
                        dropTowardLabel);
                    // Zona não escala com o estado — valida o diâmetro cru.
                    errors += ValidateOverlayFitsLetter($"Zona {h.id}", SafetyLensTheme.ArZoneDiameter, dropTowardLabel);
                    if (SafetyLensTheme.ArZoneDiameter > SafetyLensTheme.ArLetterDiameter + 1e-4f)
                    {
                        Debug.LogError(
                            $"[SafetyLens] Zona {h.id} ({SafetyLensTheme.ArZoneDiameter * 100f:0.0} cm) maior que o " +
                            $"círculo da letra ({SafetyLensTheme.ArLetterDiameter * 100f:0.0} cm). Reduza ArZoneDiameter.");
                        errors++;
                    }
                }
                else if (h.IsEpi)
                {
                    var helmet = SafetyLensTheme.ArHelmetWidth * SafetyLensTheme.ArActiveScale;
                    errors += ValidateOverlayFitsLetter($"Capacete {h.id}", helmet, dropTowardLabel);
                    if (helmet > SafetyLensTheme.ArLetterDiameter)
                    {
                        Debug.LogError(
                            $"[SafetyLens] Capacete {h.id} ({helmet * 100f:0.0} cm) cobre o círculo C " +
                            $"({SafetyLensTheme.ArLetterDiameter * 100f:0.0} cm). Reduza ArHelmetWidth.");
                        errors++;
                    }

                    errors += ValidateOverlayFitsLetter(
                        $"Anel {h.id}",
                        SafetyLensTheme.ArHelmetRingDiameter * SafetyLensTheme.ArActiveScale * pulse,
                        dropTowardLabel);
                }
            }

            return errors;
        }

        static int ValidateOverlayFitsLetter(string name, float sizeMeters, float displacementTowardLabelMeters)
        {
            var reach = sizeMeters * 0.5f + Mathf.Max(0f, displacementTowardLabelMeters);
            var budget = SafetyLensTheme.ArLetterToLabel;
            if (reach > budget - 0.001f)
            {
                Debug.LogError(
                    $"[SafetyLens] {name}: meia-altura {sizeMeters * 50f:0.00} cm + deslocamento " +
                    $"{displacementTowardLabelMeters * 100f:0.00} cm = {reach * 100f:0.00} cm " +
                    $"ultrapassa o rótulo ({budget * 100f:0.00} cm do centro da letra).");
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// ARSession roda com ordem de execução int.MinValue, então nenhum script consegue
        /// desligá-lo antes de abrir a câmera. Por isso o objeto precisa ser salvo inativo na cena.
        /// </summary>
        static void EnsureArSessionStartsInactive()
        {
            var active = EditorSceneManager.GetActiveScene();
            var openedHere = active.path != ScenePath;
            var scene = openedHere
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : active;

            ARSession session = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                session = root.GetComponentInChildren<ARSession>(true);
                if (session != null) break;
            }

            if (session != null && session.gameObject.activeSelf)
            {
                session.gameObject.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[SafetyLens] AR Session agora inicia desligado (camera sob demanda).");
            }

            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }

        static void SetXROriginReference(XROrigin origin, string propertyName, string serializedFieldName, UnityEngine.Object value)
        {
            var property = typeof(XROrigin).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.CanWrite)
            {
                property.SetValue(origin, value);
                return;
            }

            var serialized = new SerializedObject(origin);
            var field = serialized.FindProperty(serializedFieldName);
            if (field == null)
                throw new InvalidOperationException($"Nao foi possivel configurar XROrigin.{propertyName}.");
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureSceneInBuildSettings()
        {
            foreach (var entry in EditorBuildSettings.scenes)
            {
                if (entry.path == ScenePath && entry.enabled) return;
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
#endif
