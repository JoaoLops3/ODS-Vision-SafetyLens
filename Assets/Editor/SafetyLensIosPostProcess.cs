#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Facens.SafetyLens.Editor
{
    /// <summary>
    /// Desliga Module Verifier no Xcode gerado. O ▶ Run usa ReleaseForRunning; com
    /// ENABLE_MODULE_VERIFIER=YES no UnityFramework o Xcode falha em "double-quoted include".
    /// O Xcode reescreve YES ao salvar o projeto aberto — por isso, além do patch, trava o
    /// project.pbxproj (uchg) depois de corrigir para o Run não desfazer o NO.
    /// </summary>
    public static class SafetyLensIosPostProcess
    {
        static readonly string[] ConfigNames =
        {
            "Debug", "Release", "ReleaseForRunning", "ReleaseForProfiling"
        };

        static readonly (string Key, string Value)[] SafeFlags =
        {
            ("ENABLE_MODULE_VERIFIER", "NO"),
            ("ENABLE_USER_SCRIPT_SANDBOXING", "NO"),
            ("CLANG_WARN_QUOTED_INCLUDE_IN_FRAMEWORK_HEADER", "NO"),
            ("CLANG_ALLOW_NON_MODULAR_INCLUDES_IN_FRAMEWORK_MODULES", "YES"),
        };

        [PostProcessBuild(999)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            var pbxPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            UnlockPbx(pbxPath);
            ApplyViaApi(pbxPath);

            var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString(
                "NSCameraUsageDescription",
                "O SafetyLens usa a camera para Realidade Aumentada e identificacao de riscos industriais.");
            plist.WriteToFile(plistPath);

            var report = ForcePbxFlagsOff(pbxPath);
            LockPbx(pbxPath);

            // Segunda passada via shell: mata qualquer YES residual e reforça uchg+a-w.
            var fixScript = Path.GetFullPath("Tools/fix-xcode-module-verifier.sh");
            if (File.Exists(fixScript))
                RunShell("NO_OPEN=1", fixScript);

            Debug.Log($"[SafetyLens] PostProcess iOS: {report} (pbxproj travado contra rewrite do Xcode)");
        }

        [MenuItem("SafetyLens/Corrigir Module Verifier (Xcode)")]
        public static void FixModuleVerifierMenu()
        {
            var pbxPath = Path.GetFullPath("Builds/Unity-iPhone.xcodeproj/project.pbxproj");
            if (!File.Exists(pbxPath))
            {
                EditorUtility.DisplayDialog(
                    "SafetyLens",
                    "Projeto Xcode nao encontrado em Builds/. Rode SafetyLens > Build iOS (Xcode) primeiro.",
                    "OK");
                return;
            }

            UnlockPbx(pbxPath);
            ApplyViaApi(pbxPath);
            var report = ForcePbxFlagsOff(pbxPath);
            LockPbx(pbxPath);
            Debug.Log($"[SafetyLens] Fix manual: {report}");
            EditorUtility.DisplayDialog(
                "SafetyLens",
                report +
                "\n\n1) Feche o Xcode completamente\n" +
                "2) Rode este menu de novo (pbxproj fica travado)\n" +
                "3) Abra o .xcodeproj\n" +
                "4) Clean (⇧⌘K) e Run\n\n" +
                "Se o Xcode pedir para salvar o projeto, escolha Don't Save / Revert.",
                "OK");
        }

        static void ApplyViaApi(string pbxPath)
        {
            var project = new PBXProject();
            project.ReadFromFile(pbxPath);

            foreach (var targetGuid in new[]
                     {
                         project.GetUnityFrameworkTargetGuid(),
                         project.GetUnityMainTargetGuid(),
                         project.TargetGuidByName("GameAssembly"),
                         project.TargetGuidByName("Unity-iPhone Tests")
                     })
            {
                if (string.IsNullOrEmpty(targetGuid)) continue;
                ApplySafeFlags(project, targetGuid);
                foreach (var configName in ConfigNames)
                {
                    var configGuid = project.BuildConfigByName(targetGuid, configName);
                    if (string.IsNullOrEmpty(configGuid)) continue;
                    ApplySafeFlagsForConfig(project, configGuid);
                }
            }

            project.WriteToFile(pbxPath);
        }

        static void ApplySafeFlags(PBXProject project, string targetGuid)
        {
            foreach (var (key, value) in SafeFlags)
                project.SetBuildProperty(targetGuid, key, value);
        }

        static void ApplySafeFlagsForConfig(PBXProject project, string configGuid)
        {
            foreach (var (key, value) in SafeFlags)
                project.SetBuildPropertyForConfig(configGuid, key, value);
        }

        public static string ForcePbxFlagsOff(string pbxPath)
        {
            if (!File.Exists(pbxPath)) return "arquivo ausente";

            UnlockPbx(pbxPath);
            var text = File.ReadAllText(pbxPath);
            var beforeYes = Regex.Matches(text, @"ENABLE_MODULE_VERIFIER\s*=\s*YES").Count;

            foreach (var (key, value) in SafeFlags)
                text = Regex.Replace(text, $@"{key}\s*=\s*[^;]+;", $"{key} = {value};");

            text = Regex.Replace(
                text,
                @"buildSettings = \{.*?\n\t\t\t\};",
                InjectMissingFlags,
                RegexOptions.Singleline);

            File.WriteAllText(pbxPath, text);

            var after = File.ReadAllText(pbxPath);
            var residualYes = Regex.Matches(after, @"ENABLE_MODULE_VERIFIER\s*=\s*YES").Count;
            var noCount = Regex.Matches(after, @"ENABLE_MODULE_VERIFIER\s*=\s*NO").Count;

            if (residualYes > 0)
                throw new InvalidOperationException(
                    $"[SafetyLens] Falha ao desligar Module Verifier: ainda ha {residualYes} YES no pbxproj.");

            return $"Module Verifier OFF (YES removidos={beforeYes}, NO={noCount}).";
        }

        static string InjectMissingFlags(Match match)
        {
            var block = match.Value;
            foreach (var (key, value) in SafeFlags)
            {
                if (Regex.IsMatch(block, $@"{key}\s*=")) continue;
                block = block.Replace(
                    "buildSettings = {",
                    $"buildSettings = {{\n\t\t\t\t{key} = {value};",
                    StringComparison.Ordinal);
            }
            return block;
        }

        /// <summary>Permite o Unity/PostProcess reescrever o pbxproj no próximo build.</summary>
        public static void UnlockPbx(string pbxPath)
        {
            if (!File.Exists(pbxPath)) return;
            RunChflags("nouchg", pbxPath);
            try { File.SetAttributes(pbxPath, File.GetAttributes(pbxPath) & ~FileAttributes.ReadOnly); }
            catch (Exception ex) { Debug.LogWarning($"[SafetyLens] unlock attrs: {ex.Message}"); }
        }

        /// <summary>
        /// Impede o Xcode de salvar ENABLE_MODULE_VERIFIER=YES de volta no disco.
        /// O próximo Build iOS do Unity chama UnlockPbx antes de regenerar.
        /// </summary>
        public static void LockPbx(string pbxPath)
        {
            if (!File.Exists(pbxPath)) return;
            try { File.SetAttributes(pbxPath, File.GetAttributes(pbxPath) | FileAttributes.ReadOnly); }
            catch (Exception ex) { Debug.LogWarning($"[SafetyLens] lock attrs: {ex.Message}"); }
            RunChflags("uchg", pbxPath);
        }

        static void RunChflags(string flag, string path)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "/usr/bin/chflags",
                    Arguments = $"{flag} \"{path}\"",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });
                p?.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SafetyLens] chflags {flag} falhou: {ex.Message}");
            }
        }

        static void RunShell(string envPrefix, string scriptPath)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-lc '{envPrefix} \"{scriptPath}\"'",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });
                if (p == null) return;
                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(60000);
                if (!string.IsNullOrWhiteSpace(stdout)) Debug.Log($"[SafetyLens] fix-script: {stdout.Trim()}");
                if (p.ExitCode != 0)
                    Debug.LogWarning($"[SafetyLens] fix-script exit={p.ExitCode}: {stderr}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SafetyLens] fix-script falhou: {ex.Message}");
            }
        }
    }
}
#endif
