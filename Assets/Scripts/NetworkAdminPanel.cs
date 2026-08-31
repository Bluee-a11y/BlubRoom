using Unity.Netcode;
using UnityEngine;

namespace ClubhousePC
{
    public sealed class NetworkAdminPanel : NetworkBehaviour
    {
        private static float nextServerSpawn;
        private bool open;
        private GUIStyle heading;

        private void Update()
        {
            var player = GetComponent<NetworkPlayer>();
            if (!IsOwner || player == null || !player.IsAdmin.Value)
            {
                open = false;
                return;
            }
            var mobileAdmin = MobileControls.Current != null && MobileControls.Current.ConsumeAdmin();
            if (!Input.GetKeyDown(KeyCode.F2) && !Input.GetKeyDown(KeyCode.BackQuote) && !mobileAdmin) return;
            open = !open;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }

        private void OnGUI()
        {
            if (!IsOwner || !open || !GetComponent<NetworkPlayer>().IsAdmin.Value) return;
            heading ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            var panel = new Rect(20, Screen.height / 2f - 120, 300, 230);
            GUI.Box(panel, "");
            GUI.Label(new Rect(panel.x + 18, panel.y + 15, 260, 30), "NETWORK ADMIN", heading);
            GUI.Label(new Rect(panel.x + 18, panel.y + 48, 260, 25), "Everyone will see these balls");
            if (GUI.Button(new Rect(panel.x + 18, panel.y + 82, 264, 42), "SPAWN 10 BALLS ABOVE ME"))
                SpawnBallsServerRpc();
            if (GUI.Button(new Rect(panel.x + 18, panel.y + 136, 264, 42), "DESPAWN ALL BALLS"))
                DespawnBallsServerRpc();
        }

        [ServerRpc]
        private void SpawnBallsServerRpc()
        {
            if (!GetComponent<NetworkPlayer>().IsAdmin.Value) return;
            if (Time.unscaledTime < nextServerSpawn) return;
            nextServerSpawn = Time.unscaledTime + 1f;
            if (FindObjectsOfType<NetworkBall>().Length >= 50) return;

            var prefab = Resources.Load<GameObject>("NetworkBall");
            if (prefab == null)
            {
                Debug.LogError("NetworkBall prefab is missing.");
                return;
            }

            for (var i = 0; i < 10; i++)
            {
                var column = i % 5;
                var row = i / 5;
                var position = transform.position + new Vector3((column - 2) * 0.65f, 3.5f + row * 0.65f, 0);
                var ball = Instantiate(prefab, position, Quaternion.identity);
                ball.GetComponent<NetworkObject>().Spawn();
            }
        }

        [ServerRpc]
        private void DespawnBallsServerRpc()
        {
            if (!GetComponent<NetworkPlayer>().IsAdmin.Value) return;
            foreach (var ball in FindObjectsOfType<NetworkBall>())
            {
                var networkObject = ball.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                    networkObject.Despawn(true);
            }
        }
    }
}
