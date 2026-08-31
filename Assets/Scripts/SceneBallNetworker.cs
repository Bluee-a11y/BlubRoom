using Unity.Netcode;
using UnityEngine;

namespace ClubhousePC
{
    public sealed class SceneBallNetworker : MonoBehaviour
    {
        private void Start()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId) return;
            ConvertSceneBalls();
        }

        private void ConvertSceneBalls()
        {
            var localBalls = FindObjectsOfType<Grabbable>();
            if (NetworkManager.Singleton.IsServer)
            {
                var prefab = Resources.Load<GameObject>("NetworkBall");
                if (prefab == null)
                {
                    Debug.LogError("Cannot network scene balls: NetworkBall prefab is missing.");
                    return;
                }

                foreach (var localBall in localBalls)
                {
                    if (localBall.GetComponent<NetworkBall>() != null) continue;
                    var networkBall = Instantiate(prefab, localBall.transform.position, localBall.transform.rotation);
                    networkBall.GetComponent<NetworkObject>().Spawn();
                    Destroy(localBall.gameObject);
                }
            }
            else
            {
                foreach (var localBall in localBalls)
                {
                    if (localBall.GetComponent<NetworkBall>() == null)
                        Destroy(localBall.gameObject);
                }
            }
        }
    }
}
