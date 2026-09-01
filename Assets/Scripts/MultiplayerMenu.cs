using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClubhousePC
{
    public sealed class MultiplayerMenu : MonoBehaviour
    {
        private string joinCode = "";
        private string hostCode = "";
        private string status = "Relay mode — no port forwarding needed";
        private bool busy;
        private bool intentionalShutdown;
        private bool recoveringFromLostHost;
        private bool usernameMenuOpen;
        private bool settingsMenuOpen;
        private bool howToPlayOpen;
        private string usernameDraft = "";
        private string profilePlayerId = "Loading…";
        private bool loadingProfileId;
        private bool watchOpen;
        private GUIStyle heading;
        private GUIStyle settingsButton;
        private GUIStyle settingsText;
        private GUIStyle watchTabButton;
        private Font arcadeFont;

        private void Start()
        {
            watchOpen = false;
            usernameMenuOpen = false;
            settingsMenuOpen = false;
            howToPlayOpen = false;
            var manager = NetworkManager.Singleton;
            manager.OnClientConnectedCallback += OnClientConnected;
            manager.OnClientDisconnectCallback += OnClientDisconnected;
            SceneManager.sceneLoaded += CloseWatchAfterSceneLoad;
        }

        private void CloseWatchAfterSceneLoad(Scene scene, LoadSceneMode mode)
        {
            watchOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            var mobileWatch = MobileControls.Current != null && MobileControls.Current.ConsumeWatch();
            if (!Input.GetKeyDown(KeyCode.Tab) && !mobileWatch) return;
            watchOpen = !watchOpen;
            if (!watchOpen)
            {
                usernameMenuOpen = false;
                settingsMenuOpen = false;
                howToPlayOpen = false;
            }
            Cursor.lockState = watchOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = watchOpen;
        }

        private void OnDestroy()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null) return;
            manager.OnClientConnectedCallback -= OnClientConnected;
            manager.OnClientDisconnectCallback -= OnClientDisconnected;
            SceneManager.sceneLoaded -= CloseWatchAfterSceneLoad;
        }

        private void OnClientConnected(ulong clientId)
        {
            status = clientId == NetworkManager.Singleton.LocalClientId
                ? "Connected successfully"
                : "Player " + clientId + " joined";

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                var channel = NetworkManager.Singleton.IsHost ? hostCode : joinCode.Trim().ToUpperInvariant();
                GetComponent<VoiceChatManager>().JoinVoice(channel);
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            var reason = NetworkManager.Singleton.DisconnectReason;
            status = string.IsNullOrWhiteSpace(reason) ? "A player disconnected" : "Disconnected: " + reason;

            if (clientId == NetworkManager.Singleton.LocalClientId &&
                !intentionalShutdown && !NetworkManager.Singleton.IsServer)
                RecoverFromLostHost();
        }

        private async void RecoverFromLostHost()
        {
            if (recoveringFromLostHost) return;
            recoveringFromLostHost = true;
            busy = true;
            status = "Host left — returning safely to Blubhouse…";

            try
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    NetworkManager.Singleton.Shutdown();
                await System.Threading.Tasks.Task.Delay(150);
                if (SceneManager.GetActiveScene().name != "Main")
                    await LoadingScreen.LoadScene("Main");
                status = "Host left — Blubhouse offline";
            }
            catch (System.Exception exception)
            {
                status = "Disconnected safely — reopen Blubhouse if needed";
                Debug.LogException(exception);
            }
            finally
            {
                busy = false;
                recoveringFromLostHost = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void OnGUI()
        {
            if (!watchOpen) return;
            arcadeFont ??= Resources.Load<Font>("Fonts/PressStart2P-Regular");
            var previousFont = GUI.skin.font;
            var previousLabelSize = GUI.skin.label.fontSize;
            var previousButtonSize = GUI.skin.button.fontSize;
            var previousTextFieldSize = GUI.skin.textField.fontSize;
            if (arcadeFont != null) GUI.skin.font = arcadeFont;
            GUI.skin.label.fontSize = 9;
            GUI.skin.button.fontSize = 8;
            GUI.skin.textField.fontSize = 9;
            try { DrawWatchGUI(); }
            finally
            {
                GUI.skin.font = previousFont;
                GUI.skin.label.fontSize = previousLabelSize;
                GUI.skin.button.fontSize = previousButtonSize;
                GUI.skin.textField.fontSize = previousTextFieldSize;
            }
        }

        private void DrawWatchGUI()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null) return;

            heading ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                normal = { textColor = Color.white }
            };

            GUI.Box(new Rect(Screen.width - 370, 18, 352, 410), "");
            GUI.Label(new Rect(Screen.width - 352, 30, 315, 30), "BLUB WATCH", heading);
            GUI.Label(new Rect(Screen.width - 352, 62, 118, 24), "Map: " + SceneManager.GetActiveScene().name);
            watchTabButton ??= new GUIStyle(GUI.skin.button) { fontSize = 7 };
            if (GUI.Button(new Rect(Screen.width - 222, 58, 90, 30), "SETTINGS", watchTabButton))
            {
                settingsMenuOpen = true;
                howToPlayOpen = false;
                usernameMenuOpen = false;
            }
            if (GUI.Button(new Rect(Screen.width - 125, 58, 87, 30), "PROFILE", watchTabButton))
            {
                usernameMenuOpen = true;
                settingsMenuOpen = false;
                howToPlayOpen = false;
                usernameDraft = PlayerPrefs.GetString("BlubRoom.Username", "Player");
                LoadProfilePlayerId();
            }

            if (settingsMenuOpen)
            {
                DrawSettingsMenu();
                return;
            }

            if (usernameMenuOpen)
            {
                DrawUsernameMenu();
                return;
            }

            if (SceneManager.GetActiveScene().name == "MakerWorld")
            {
                GUI.Label(new Rect(Screen.width - 352, 92, 315, 24), "Press F to open the Maker palette");
                if (manager.IsHost)
                {
                    GUI.Label(new Rect(Screen.width - 352, 122, 250, 24), "Maker code: " + hostCode);
                    if (GUI.Button(new Rect(Screen.width - 105, 119, 67, 27), "COPY"))
                        GUIUtility.systemCopyBuffer = hostCode;
                }
                GUI.Label(new Rect(Screen.width - 352, 158, 78, 24), "Join code:");
                joinCode = GUI.TextField(new Rect(Screen.width - 270, 156, 232, 26), joinCode.ToUpperInvariant(), 12);
                GUI.enabled = !busy;
                if (GUI.Button(new Rect(Screen.width - 352, 192, 314, 34), "JOIN MAKERWORLD SERVER"))
                    StartClient();
                if (GUI.Button(new Rect(Screen.width - 352, 240, 314, 40), "RETURN TO BLUBHOUSE"))
                    ReturnToBlubhouse();
                if (GUI.Button(new Rect(Screen.width - 352, 290, 314, 40), "GO TO BLUBCENTER"))
                    EnterBlubCenter();
                GUI.enabled = true;
                GUI.Label(new Rect(Screen.width - 352, 342, 314, 32), status);
                return;
            }

            if (SceneManager.GetActiveScene().name == "Dodgeball")
            {
                GUI.Label(new Rect(Screen.width - 352, 96, 315, 24), "Red team vs blue team");
                if (manager.IsHost)
                {
                    GUI.Label(new Rect(Screen.width - 352, 126, 250, 24), "Match code: " + hostCode);
                    if (GUI.Button(new Rect(Screen.width - 105, 123, 67, 27), "COPY"))
                        GUIUtility.systemCopyBuffer = hostCode;
                }
                GUI.Label(new Rect(Screen.width - 352, 162, 78, 24), "Join code:");
                joinCode = GUI.TextField(new Rect(Screen.width - 270, 160, 232, 26), joinCode.ToUpperInvariant(), 12);
                GUI.enabled = !busy;
                if (GUI.Button(new Rect(Screen.width - 352, 196, 314, 36), "JOIN DODGEBALL MATCH"))
                    StartClient();
                if (GUI.Button(new Rect(Screen.width - 352, 244, 314, 40), "RETURN TO BLUBCENTER"))
                    EnterBlubCenter();
                if (GUI.Button(new Rect(Screen.width - 352, 294, 314, 40), "RETURN TO BLUBHOUSE"))
                    ReturnToBlubhouse();
                GUI.enabled = true;
                GUI.Label(new Rect(Screen.width - 352, 350, 314, 32), status);
                return;
            }

            if (SceneManager.GetActiveScene().name == "BlubCenter")
            {
                GUI.Label(new Rect(Screen.width - 352, 96, 315, 24), "Public BlubCenter session");
                if (manager.IsHost)
                {
                    GUI.Label(new Rect(Screen.width - 352, 126, 250, 24), "Join code: " + hostCode);
                    if (GUI.Button(new Rect(Screen.width - 105, 123, 67, 27), "COPY"))
                        GUIUtility.systemCopyBuffer = hostCode;
                }
                GUI.Label(new Rect(Screen.width - 352, 162, 78, 24), "Join code:");
                joinCode = GUI.TextField(new Rect(Screen.width - 270, 160, 232, 26), joinCode.ToUpperInvariant(), 12);
                GUI.enabled = !busy;
                if (GUI.Button(new Rect(Screen.width - 352, 196, 314, 34), "JOIN BLUBCENTER SERVER"))
                    StartClient();
                if (GUI.Button(new Rect(Screen.width - 352, 238, 314, 32), "GO TO MAKERWORLD"))
                    EnterMakerWorld();
                if (GUI.Button(new Rect(Screen.width - 352, 276, 314, 32), "GO TO DODGEBALL"))
                    EnterDodgeball();
                if (GUI.Button(new Rect(Screen.width - 352, 314, 314, 34), "RETURN TO BLUBHOUSE"))
                    ReturnToBlubhouse();
                GUI.enabled = true;
                GUI.Label(new Rect(Screen.width - 352, 356, 314, 32), status);
                return;
            }

            GUI.Label(new Rect(Screen.width - 352, 92, 315, 24), "Destination: BlubCenter");
            if (GUI.Button(new Rect(Screen.width - 352, 120, 314, 34), "GO TO BLUBCENTER"))
                EnterBlubCenter();
            if (GUI.Button(new Rect(Screen.width - 352, 160, 314, 34), "GO TO MAKERWORLD"))
                EnterMakerWorld();
            GUI.Label(new Rect(Screen.width - 352, 205, 315, 24), "PRIVATE BLUBHOUSE SERVER");

            if (!manager.IsListening)
            {
                GUI.enabled = !busy;
                if (GUI.Button(new Rect(Screen.width - 352, 234, 314, 34), "CREATE PRIVATE SERVER")) StartHost();
                GUI.Label(new Rect(Screen.width - 352, 280, 78, 24), "Join code:");
                joinCode = GUI.TextField(new Rect(Screen.width - 270, 278, 232, 26), joinCode.ToUpperInvariant(), 12);
                if (GUI.Button(new Rect(Screen.width - 352, 314, 314, 34), "JOIN PRIVATE SERVER")) StartClient();
                GUI.enabled = true;
                GUI.Label(new Rect(Screen.width - 352, 356, 314, 24), status);
            }
            else
            {
                var privateInfo = manager.IsHost ? "Private code: " + hostCode : status;
                GUI.Label(new Rect(Screen.width - 352, 204, 250, 24), privateInfo);
                if (manager.IsHost && GUI.Button(new Rect(Screen.width - 105, 201, 67, 27), "COPY"))
                    GUIUtility.systemCopyBuffer = hostCode;
                if (GUI.Button(new Rect(Screen.width - 352, 242, 314, 34), "LEAVE PRIVATE SERVER"))
                {
                    intentionalShutdown = true;
                    manager.Shutdown();
                    status = "Disconnected — restart Play mode for solo controls";
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        private void DrawSettingsMenu()
        {
            settingsButton ??= new GUIStyle(GUI.skin.button) { fontSize = 8 };
            settingsText ??= new GUIStyle(GUI.skin.label) { fontSize = 8 };
            GUI.Label(new Rect(Screen.width - 352, 102, 315, 30),
                howToPlayOpen ? "HOW TO PLAY" : "SETTINGS", heading);

            if (howToPlayOpen)
            {
                var controls = "WASD  MOVE\nMOUSE  LOOK\nSPACE  JUMP\nCTRL  CROUCH\nRIGHT SHIFT  THIRD PERSON\nTAB  WATCH\nC  CHAT\nV  VOICE\nLEFT CLICK  GRAB\nRIGHT CLICK  THROW";
                GUI.Label(new Rect(Screen.width - 352, 142, 314, 205), controls, settingsText);
                if (GUI.Button(new Rect(Screen.width - 352, 352, 314, 44), "BACK", settingsButton))
                    howToPlayOpen = false;
                return;
            }

            if (GUI.Button(new Rect(Screen.width - 352, 148, 314, 50), "HOW TO PLAY", settingsButton))
                howToPlayOpen = true;
            if (GUI.Button(new Rect(Screen.width - 352, 212, 314, 50), "QUIT GAME", settingsButton))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            if (GUI.Button(new Rect(Screen.width - 352, 276, 314, 50), "BACK", settingsButton))
                settingsMenuOpen = false;
        }

        private void DrawUsernameMenu()
        {
            GUI.Label(new Rect(Screen.width - 352, 102, 315, 26), "CHANGE USERNAME", heading);
            GUI.Label(new Rect(Screen.width - 352, 142, 315, 24), "This is visible to everyone:");
            usernameDraft = GUI.TextField(new Rect(Screen.width - 352, 174, 314, 32), usernameDraft, 20);
            GUI.Label(new Rect(Screen.width - 352, 214, 315, 24), "Unity Player ID:");
            GUI.Label(new Rect(Screen.width - 352, 238, 315, 24), profilePlayerId);
            var adminStatus = AdminAccess.IsAllowed(profilePlayerId) ? "Admin access: ENABLED" : "Admin access: not allowed";
            GUI.Label(new Rect(Screen.width - 352, 264, 315, 24), adminStatus);
            GUI.enabled = !loadingProfileId && !string.IsNullOrWhiteSpace(profilePlayerId) && profilePlayerId != "Loading…";
            if (GUI.Button(new Rect(Screen.width - 352, 292, 314, 30), "COPY PLAYER ID"))
            {
                GUIUtility.systemCopyBuffer = profilePlayerId;
                status = "Player ID copied";
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(Screen.width - 352, 324, 314, 34), "SAVE USERNAME"))
            {
                usernameDraft = string.IsNullOrWhiteSpace(usernameDraft) ? "Player" : usernameDraft.Trim();
                PlayerPrefs.SetString("BlubRoom.Username", usernameDraft);
                PlayerPrefs.Save();
                foreach (var player in FindObjectsOfType<NetworkPlayer>())
                    if (player.IsOwner) player.SetDisplayName(usernameDraft);
                status = "Username changed to " + usernameDraft;
                usernameMenuOpen = false;
            }
            if (GUI.Button(new Rect(Screen.width - 352, 366, 314, 32), "BACK"))
                usernameMenuOpen = false;
        }

        private async void LoadProfilePlayerId()
        {
            if (loadingProfileId) return;
            loadingProfileId = true;
            profilePlayerId = "Loading…";
            try
            {
                await SignIn();
                profilePlayerId = AuthenticationService.Instance.PlayerId;
            }
            catch (System.Exception exception)
            {
                profilePlayerId = "Could not load ID";
                Debug.LogException(exception);
            }
            finally { loadingProfileId = false; }
        }

        private void PrepareForNetwork()
        {
            foreach (var motor in FindObjectsOfType<PlayerMotor>()) Destroy(motor.gameObject);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public async void StartHost()
        {
            busy = true;
            status = "Creating Relay game…";
            try
            {
                await SignIn();
                var allocation = await RelayService.Instance.CreateAllocationAsync(7);
                hostCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                var relayData = new RelayServerData(allocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
                PrepareForNetwork();
                intentionalShutdown = false;
                if (NetworkManager.Singleton.StartHost())
                {
                    status = "Share code " + hostCode;
                    // Do not depend only on NGO's host connection callback; it can
                    // run during StartHost before other services finish their setup.
                    GetComponent<VoiceChatManager>().JoinVoice(hostCode);
                }
                else
                {
                    status = "Could not start host";
                }
            }
            catch (System.Exception exception)
            {
                status = "Relay failed: " + exception.Message;
                Debug.LogException(exception);
            }
            finally { busy = false; }
        }

        public async void EnterBlubCenter()
        {
            if (busy) return;
            busy = true;
            status = "Loading BlubCenter…";
            try
            {
                if (NetworkManager.Singleton.IsListening)
                {
                    intentionalShutdown = true;
                    NetworkManager.Singleton.Shutdown();
                    await System.Threading.Tasks.Task.Delay(150);
                    intentionalShutdown = false;
                }

                await LoadingScreen.LoadScene("BlubCenter");
            }
            finally { busy = false; }

            StartHost();
        }

        public async void EnterMakerWorld()
        {
            if (busy) return;
            busy = true;
            status = "Loading MakerWorld…";
            if (NetworkManager.Singleton.IsListening)
            {
                intentionalShutdown = true;
                NetworkManager.Singleton.Shutdown();
                await System.Threading.Tasks.Task.Delay(150);
            }
            await LoadingScreen.LoadScene("MakerWorld");
            intentionalShutdown = false;
            status = "MakerWorld — press F to build";
            busy = false;
            StartHost();
        }

        public async void EnterDodgeball()
        {
            if (busy) return;
            busy = true;
            status = "Loading Dodgeball…";
            if (NetworkManager.Singleton.IsListening)
            {
                intentionalShutdown = true;
                NetworkManager.Singleton.Shutdown();
                await System.Threading.Tasks.Task.Delay(150);
            }
            await LoadingScreen.LoadScene("Dodgeball");
            intentionalShutdown = false;
            status = "Dodgeball — share the match code";
            busy = false;
            StartHost();
        }

        public async void ReturnToBlubhouse()
        {
            if (busy) return;
            busy = true;
            status = "Returning to Blubhouse…";
            if (NetworkManager.Singleton.IsListening)
            {
                intentionalShutdown = true;
                NetworkManager.Singleton.Shutdown();
                await System.Threading.Tasks.Task.Delay(150);
            }
            await LoadingScreen.LoadScene("Main");
            status = "Blubhouse — offline";
            intentionalShutdown = false;
            busy = false;
        }

        public async void StartClient()
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                status = "Enter the host's join code";
                return;
            }

            busy = true;
            status = "Joining Relay game…";
            try
            {
                if (NetworkManager.Singleton.IsListening)
                {
                    intentionalShutdown = true;
                    NetworkManager.Singleton.Shutdown();
                    await System.Threading.Tasks.Task.Delay(150);
                    intentionalShutdown = false;
                }
                await SignIn();
                var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim().ToUpperInvariant());
                var relayData = new RelayServerData(allocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
                PrepareForNetwork();
                intentionalShutdown = false;
                status = NetworkManager.Singleton.StartClient() ? "Connecting…" : "Could not start client";
            }
            catch (System.Exception exception)
            {
                status = "Join failed: " + exception.Message;
                Debug.LogException(exception);
            }
            finally { busy = false; }
        }

        private static async System.Threading.Tasks.Task SignIn()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
