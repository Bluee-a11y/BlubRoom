using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClubhousePC
{
    public sealed class DodgeballGameController : MonoBehaviour
    {
        private GUIStyle title;
        private GUIStyle score;
        private GUIStyle message;
        private Font arcadeFont;
        private float nextBallSafetyCheck;

        private void Update()
        {
            if (SceneManager.GetActiveScene().name != "Dodgeball" ||
                NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer ||
                Time.unscaledTime < nextBallSafetyCheck) return;

            nextBallSafetyCheck = Time.unscaledTime + 0.5f;
            foreach (var ball in FindObjectsOfType<NetworkBall>())
                ball.ServerReturnToCourtIfLost();
        }

        private void OnGUI()
        {
            arcadeFont ??= Resources.Load<Font>("Fonts/PressStart2P-Regular");
            title ??= Style(15, Color.white);
            score ??= Style(11, Color.white);
            message ??= Style(9, new Color(1f, 0.82f, 0.2f));

            GUI.Label(new Rect(Screen.width * 0.5f - 170f, 16f, 340f, 28f), "DODGEBALL", title);

            var manager = NetworkManager.Singleton;
            if (manager == null || !manager.IsListening)
            {
                GUI.Label(new Rect(Screen.width * 0.5f - 230f, 48f, 460f, 28f),
                    "OPEN THE WATCH TO START OR JOIN", message);
                return;
            }

            var players = FindObjectsOfType<NetworkPlayer>();
            var blueTotal = players.Count(player => player.DodgeballTeam.Value == NetworkPlayer.BlueTeam);
            var redTotal = players.Count(player => player.DodgeballTeam.Value == NetworkPlayer.RedTeam);
            var blueAlive = players.Count(player => player.DodgeballTeam.Value == NetworkPlayer.BlueTeam &&
                                                    !player.DodgeballOut.Value);
            var redAlive = players.Count(player => player.DodgeballTeam.Value == NetworkPlayer.RedTeam &&
                                                   !player.DodgeballOut.Value);

            GUI.color = new Color(0.25f, 0.6f, 1f);
            GUI.Label(new Rect(24f, 50f, 280f, 28f), "BLUE  " + blueAlive + "/" + blueTotal, score);
            GUI.color = new Color(1f, 0.3f, 0.3f);
            GUI.Label(new Rect(Screen.width - 250f, 50f, 226f, 28f), "RED  " + redAlive + "/" + redTotal, score);
            GUI.color = Color.white;

            var winner = blueTotal > 0 && redTotal > 0
                ? blueAlive == 0 ? "RED TEAM WINS!" : redAlive == 0 ? "BLUE TEAM WINS!" : ""
                : "WAITING FOR BOTH TEAMS";
            if (string.IsNullOrEmpty(winner)) return;

            GUI.Label(new Rect(Screen.width * 0.5f - 230f, 50f, 460f, 28f), winner, message);
            if (manager.IsHost && blueTotal > 0 && redTotal > 0 &&
                GUI.Button(new Rect(Screen.width * 0.5f - 100f, 84f, 200f, 40f), "NEXT ROUND"))
                ResetRound();
        }

        private GUIStyle Style(int size, Color color)
        {
            return new GUIStyle(GUI.skin.label)
            {
                font = arcadeFont,
                fontSize = size,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = color }
            };
        }

        private static void ResetRound()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null || !manager.IsServer) return;

            foreach (var ball in FindObjectsOfType<NetworkBall>())
            {
                var networkObject = ball.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned) networkObject.Despawn(true);
            }

            foreach (var player in FindObjectsOfType<NetworkPlayer>()) player.ServerResetDodgeballPlayer();

            var prefab = Resources.Load<GameObject>("NetworkBall");
            if (prefab == null) return;
            for (var i = 0; i < 7; i++)
            {
                var ball = Instantiate(prefab, new Vector3(-9f + i * 3f, 0.65f, 0f), Quaternion.identity);
                var networkObject = ball.GetComponent<NetworkObject>();
                networkObject.Spawn();
                foreach (var clientId in manager.ConnectedClientsIds)
                    if (!networkObject.IsNetworkVisibleTo(clientId)) networkObject.NetworkShow(clientId);
            }
        }
    }
}
