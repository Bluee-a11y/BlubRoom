using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace ClubhousePC
{
    public static class MultiplayerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateNetworkManager()
        {
            foreach (var motor in Object.FindObjectsOfType<PlayerMotor>())
            {
                var admin = motor.GetComponent<AdminPanel>();
                if (admin != null) Object.Destroy(admin);
                if (motor.GetComponent<MobileControls>() == null)
                    motor.gameObject.AddComponent<MobileControls>();
            }

            if (NetworkManager.Singleton != null) return;

            var prefab = Resources.Load<GameObject>("NetworkPlayer");
            var ballPrefab = Resources.Load<GameObject>("NetworkBall");
            var makerPrefab = Resources.Load<GameObject>("NetworkMakerBlock");
            if (prefab == null || ballPrefab == null || makerPrefab == null)
            {
                Debug.LogError("NetworkPlayer prefab is missing. Let Unity finish compiling, then reopen the project.");
                return;
            }

            var go = new GameObject("Multiplayer");
            Object.DontDestroyOnLoad(go);
            var transport = go.AddComponent<UnityTransport>();
            var manager = go.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                PlayerPrefab = prefab,
                EnableSceneManagement = false
            };
            manager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = ballPrefab });
            manager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = makerPrefab });
            go.AddComponent<MultiplayerMenu>();
            go.AddComponent<VoiceChatManager>();
            go.AddComponent<SceneBallNetworker>();
        }
    }
}
