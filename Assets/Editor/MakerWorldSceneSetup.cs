using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ClubhousePC.Editor
{
    [InitializeOnLoad]
    public static class MakerWorldSceneSetup
    {
        static MakerWorldSceneSetup() => EditorApplication.delayCall += EnsureScene;

        private static void EnsureScene()
        {
            const string path = "Assets/Scenes/MakerWorld.unity";
            if (File.Exists(path) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "MakerWorld";
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();
        }
    }
}
