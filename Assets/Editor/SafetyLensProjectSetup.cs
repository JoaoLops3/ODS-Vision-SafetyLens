#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.ARSubsystems;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
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
            var library = EnsureImageLibrary();
            EnsureScene(library, false);
            SafetyLensVisualAssetsSetup.EnsureAll(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SafetyLens] Projeto configurado. Use SafetyLens > Validar projeto e depois Build and Run Android.");
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

            var manager = UnityEngine.Object.FindFirstObjectByType<ARTrackedImageManager>();
            if (manager == null || manager.referenceLibrary == null || manager.referenceLibrary.count != 1)
            {
                Debug.LogError("[SafetyLens] Reference Image Library ausente ou invalida.");
                errors++;
            }

            var scenario = Resources.Load<TextAsset>("safetylens_scenario");
            if (scenario == null)
            {
                Debug.LogError("[SafetyLens] Resources/safetylens_scenario.json ausente.");
                errors++;
            }

            var xr = GetAndroidXRSettings();
            var arCore = false;
            if (xr?.AssignedSettings != null)
            {
                foreach (var loader in xr.AssignedSettings.activeLoaders)
                {
                    if (loader != null && loader.GetType().FullName == "UnityEngine.XR.ARCore.ARCoreLoader")
                        arCore = true;
                }
            }

            if (!arCore)
            {
                Debug.LogError("[SafetyLens] ARCore Loader nao habilitado.");
                errors++;
            }

            if (errors == 0)
                Debug.Log("[SafetyLens] VALIDACAO OK: baseline estrutural pronto para Android ARCore / iOS ARKit.");
            else
                Debug.LogError($"[SafetyLens] VALIDACAO FALHOU: {errors} problema(s).");
        }

        static int RequireObject<T>(string label) where T : UnityEngine.Object
        {
            if (UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include) != null) return 0;
            Debug.LogError($"[SafetyLens] Componente ausente: {label}");
            return 1;
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

            var perTarget = EnsureXRGeneralSettings();
            if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.iOS))
                perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.iOS);
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.iOS))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);

            var ios = perTarget.SettingsForBuildTarget(BuildTargetGroup.iOS);
            if (ios?.AssignedSettings == null) return;

            ios.InitManagerOnStart = true;
            EditorUtility.SetDirty(ios);
            XRPackageMetadataStore.AssignLoader(
                ios.AssignedSettings,
                "UnityEngine.XR.ARKit.ARKitLoader",
                BuildTargetGroup.iOS);
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

        static void EnsureScene(XRReferenceImageLibrary library, bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EnsureSceneInBuildSettings();
                EnsureArSessionStartsInactive();
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

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings();
            EditorSceneManager.OpenScene(ScenePath);
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
