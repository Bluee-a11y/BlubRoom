using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace ClubhousePC.Editor
{
    public sealed class BuildSceneGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report) => ForceMainFirst();

        [MenuItem("Tools/Clubhouse PC/Open BlubCenter")]
        private static void OpenBlubCenter()
        {
            ForceMainFirst();
            EditorSceneManager.OpenScene("Assets/Scenes/BlubCenter.unity");
        }

        private static void ForceMainFirst()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/BlubCenter.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/MakerWorld.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Dodgeball.unity", true)
            };
        }
    }
}
