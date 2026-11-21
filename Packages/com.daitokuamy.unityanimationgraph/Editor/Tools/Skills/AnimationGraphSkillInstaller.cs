using System;
using System.IO;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// 同梱の AI Skill を現在の Unity プロジェクトへ追加する
    /// </summary>
    internal static class AnimationGraphSkillInstaller {
        private const string PackageName = "com.daitokuamy.unityanimationgraph";
        private const string SkillName = "animationgraph-tuning";

        /// <summary>
        /// Codex 用の Skill をプロジェクトに追加する
        /// </summary>
        [MenuItem("Tools/Unity Animation Graph/AI/Install Codex Skill")]
        private static void InstallForCodex() => Install(".agents");

        /// <summary>
        /// Claude Code 用の Skill をプロジェクトに追加する
        /// </summary>
        [MenuItem("Tools/Unity Animation Graph/AI/Install Claude Code Skill")]
        private static void InstallForClaudeCode() => Install(".claude");

        /// <summary>
        /// 指定したクライアント用のフォルダに同梱 Skill を追加する
        /// </summary>
        /// <param name="clientDirectory">プロジェクト内のクライアント設定フォルダ名</param>
        private static void Install(string clientDirectory) {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var directory = Path.Combine(projectRoot, clientDirectory, "skills", SkillName);
            var target = Path.Combine(directory, "SKILL.md");
            var package = PackageInfo.FindForPackageName(PackageName);
            const string title = "AnimationGraph Skill";
            if (package == null) {
                EditorUtility.DisplayDialog(title, "The AnimationGraph package could not be resolved.", "OK");
                return;
            }

            var source = Path.Combine(package.resolvedPath, "Skills~", SkillName, "SKILL.md");
            try {
                if (Directory.Exists(directory) || File.Exists(directory)) {
                    EditorUtility.DisplayDialog(title, $"A skill with this name already exists:\n{directory}", "OK");
                    return;
                }

                if (!File.Exists(source)) {
                    EditorUtility.DisplayDialog(title, $"The bundled skill was not found:\n{source}", "OK");
                    return;
                }

                Directory.CreateDirectory(directory);
                File.Copy(source, target, overwrite: false);
                EditorUtility.DisplayDialog(title, $"Installed for this project:\n{target}", "OK");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
                EditorUtility.DisplayDialog(title, $"Could not install the skill:\n{exception.Message}", "OK");
            }
        }
    }
}
