using ClubhousePC;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace ClubhousePC.Editor
{
    [InitializeOnLoad]
    public static class NetworkPrefabBuilder
    {
        static NetworkPrefabBuilder() => EditorApplication.delayCall += EnsurePrefab;

        private static void EnsurePrefab()
        {
            const string folder = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Resources");

            const string playerPath = folder + "/NetworkPlayer.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(playerPath) == null)
            {
                var player = new GameObject("NetworkPlayer");
                var controller = player.AddComponent<CharacterController>();
                controller.height = 1.8f;
                controller.radius = 0.35f;
                controller.center = new Vector3(0, 0.9f, 0);
                player.AddComponent<NetworkObject>();
                player.AddComponent<NetworkPlayer>();
                player.AddComponent<MakerBuildNetwork>();
                PrefabUtility.SaveAsPrefabAsset(player, playerPath);
                Object.DestroyImmediate(player);
            }
            var savedPlayer = PrefabUtility.LoadPrefabContents(playerPath);
            var networkAdmin = savedPlayer.GetComponent<NetworkAdminPanel>();
            if (networkAdmin != null)
                Object.DestroyImmediate(networkAdmin, true);
            if (savedPlayer.GetComponent<MakerBuildNetwork>() == null)
                savedPlayer.AddComponent<MakerBuildNetwork>();
            PrefabUtility.SaveAsPrefabAsset(savedPlayer, playerPath);
            PrefabUtility.UnloadPrefabContents(savedPlayer);

            const string ballPath = folder + "/NetworkBall.prefab";
            const string materialPath = folder + "/NetworkBallMaterial.mat";
            var ballMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (ballMaterial == null)
            {
                ballMaterial = new Material(Shader.Find("Standard")) { color = new Color(1f, 0.25f, 0.07f) };
                AssetDatabase.CreateAsset(ballMaterial, materialPath);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ballPath) == null)
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = "NetworkBall";
                ball.transform.localScale = Vector3.one * 0.5f;
                ball.GetComponent<Renderer>().sharedMaterial = ballMaterial;
                ball.AddComponent<Rigidbody>().mass = 0.5f;
                ball.AddComponent<NetworkObject>();
                ball.AddComponent<NetworkTransform>();
                ball.AddComponent<NetworkRigidbody>();
                ball.AddComponent<NetworkBall>();
                PrefabUtility.SaveAsPrefabAsset(ball, ballPath);
                Object.DestroyImmediate(ball);
            }
            else
            {
                var ballContents = PrefabUtility.LoadPrefabContents(ballPath);
                ballContents.GetComponent<Renderer>().sharedMaterial = ballMaterial;
                if (ballContents.GetComponent<NetworkBall>() == null) ballContents.AddComponent<NetworkBall>();
                var networkTransform = ballContents.GetComponent<NetworkTransform>();
                if (networkTransform == null || networkTransform.GetType() != typeof(NetworkTransform))
                {
                    if (networkTransform != null) Object.DestroyImmediate(networkTransform, true);
                    ballContents.AddComponent<NetworkTransform>();
                }
                if (ballContents.GetComponent<NetworkRigidbody>() == null) ballContents.AddComponent<NetworkRigidbody>();
                PrefabUtility.SaveAsPrefabAsset(ballContents, ballPath);
                PrefabUtility.UnloadPrefabContents(ballContents);
            }

            const string makerPath = folder + "/NetworkMakerBlock.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(makerPath) == null)
            {
                var maker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                maker.name = "NetworkMakerBlock";
                maker.AddComponent<NetworkObject>();
                maker.AddComponent<NetworkTransform>();
                maker.AddComponent<NetworkMakerBlock>();
                PrefabUtility.SaveAsPrefabAsset(maker, makerPath);
                Object.DestroyImmediate(maker);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Clubhouse PC: multiplayer prefabs are ready.");
        }
    }
}
