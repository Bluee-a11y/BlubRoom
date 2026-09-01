using UnityEditor;
using UnityEngine;

namespace ClubhousePC.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        static ProjectSetup()
        {
            EditorApplication.delayCall += CreateVisibleScene;
        }

        private static void CreateVisibleScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
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
