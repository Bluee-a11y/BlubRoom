using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace ClubhousePC
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        private readonly NetworkVariable<Vector3> syncedPosition = new();
        private readonly NetworkVariable<Quaternion> syncedRotation = new(Quaternion.identity);
        private readonly NetworkVariable<bool> syncedCrouching = new();
        private readonly NetworkVariable<FixedString64Bytes> syncedName = new();
        private readonly NetworkVariable<FixedString128Bytes> syncedPlayerId = new();
        public readonly NetworkVariable<bool> IsAdmin = new(false);

        private GameObject avatar;
        private PlayerMotor localMotor;
        private float nextNetworkUpdate;
        private TextMesh nameTag;
        private MeshRenderer nameTagRenderer;
        private TextMesh chatBubble;
        private MeshRenderer chatBubbleRenderer;
        private float chatExpiresAt;
        private float nextChatAllowedAt;
        private string localChatConfirmation = "";
        private float localChatConfirmationExpires;
        private Vector3 lastClientPosition;
        private float lastClientUpdateTime;
        private int airborneFlingStrikes;
        private float antiCheatGraceUntil;
        private bool adminOpen;
        private GUIStyle adminHeading;
        private static float nextAdminSpawn;
        private GameObject localAvatarHead;

        public override void OnNetworkSpawn()
        {
            transform.position = new Vector3((int)OwnerClientId * 1.5f, 1.1f, -3.5f);
            if (IsServer)
            {
                lastClientPosition = transform.position;
                lastClientUpdateTime = Time.unscaledTime;
                antiCheatGraceUntil = Time.unscaledTime + 3f;
            }
            CreateAvatar();
            CreateNameTag();
            CreateChatBubble();
            syncedName.OnValueChanged += OnNameChanged;
            OnNameChanged(default, syncedName.Value);

            if (!IsOwner) return;

            var savedName = PlayerPrefs.GetString("BlubRoom.Username", "Player-" + OwnerClientId);
            SetDisplayName(savedName);
            RegisterPlayerIdentity();

            var localHead = avatar.transform.Find("Fallback/Head");
            if (localHead != null)
            {
                localAvatarHead = localHead.gameObject;
                localAvatarHead.SetActive(false);
            }
            foreach (var existingListener in FindObjectsOfType<AudioListener>())
                existingListener.enabled = false;

            var cameraObject = new GameObject("Player Camera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.6f, 0);
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();

            localMotor = gameObject.AddComponent<PlayerMotor>();
            localMotor.View = cameraObject.transform;
            var interaction = gameObject.AddComponent<DesktopInteractor>();
            interaction.View = camera;
            gameObject.AddComponent<PlayerChatInput>().Player = this;
            if (SceneManager.GetActiveScene().name == "MakerWorld")
                gameObject.AddComponent<MakerTool>().View = camera;
            gameObject.AddComponent<PrototypeHUD>();
            gameObject.AddComponent<MobileControls>();
        }

        private async void RegisterPlayerIdentity()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                    await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();

                while (!IsSpawned) await System.Threading.Tasks.Task.Yield();
                SubmitPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("Could not register Player ID for admin access: " + exception.Message);
            }
        }

        [ServerRpc]
        private void SubmitPlayerIdServerRpc(string playerId)
        {
            playerId = string.IsNullOrWhiteSpace(playerId) ? "" : playerId.Trim();
            if (playerId.Length > 120) playerId = playerId.Substring(0, 120);
            syncedPlayerId.Value = playerId;
            IsAdmin.Value = AdminAccess.IsAllowed(playerId);
            Debug.Log("Admin access for client " + OwnerClientId + ": " +
                (IsAdmin.Value ? "ENABLED" : "denied"));
        }

        private void Update()
        {
            if (!IsSpawned) return;
            HandleAdminInput();
            if (IsOwner && localMotor != null && localAvatarHead != null)
                localAvatarHead.SetActive(localMotor.IsThirdPerson);

            if (IsOwner)
            {
                if (Time.unscaledTime < nextNetworkUpdate) return;
                nextNetworkUpdate = Time.unscaledTime + 0.05f;

                if (IsServer)
                {
                    syncedPosition.Value = transform.position;
                    syncedRotation.Value = transform.rotation;
                    syncedCrouching.Value = localMotor != null && localMotor.IsCrouching;
                }
                else
                {
                    SubmitTransformServerRpc(transform.position, transform.rotation,
                        localMotor != null && localMotor.IsCrouching);
                }
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, syncedPosition.Value, 15f * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, syncedRotation.Value, 15f * Time.deltaTime);
            }

            if (avatar != null)
            {
                var targetScale = new Vector3(1, syncedCrouching.Value ? 0.65f : 1f, 1);
                avatar.transform.localScale = Vector3.Lerp(avatar.transform.localScale, targetScale, 14f * Time.deltaTime);
            }

            if (nameTag != null)
            {
                nameTag.transform.localPosition = new Vector3(0, syncedCrouching.Value ? 1.55f : 2.15f, 0);
                var camera = Camera.main;
                if (camera != null)
                {
                    nameTag.transform.rotation = Quaternion.LookRotation(nameTag.transform.position - camera.transform.position);
                    if (!IsOwner && nameTagRenderer != null)
                    {
                        var blocked = Physics.Linecast(camera.transform.position, nameTag.transform.position,
                            out var obstruction, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                            !obstruction.transform.IsChildOf(transform);
                        nameTagRenderer.enabled = !blocked &&
                            Vector3.Distance(camera.transform.position, nameTag.transform.position) <= 35f;
                    }
                }
            }

            UpdateChatBubble();
        }

        private void HandleAdminInput()
        {
            if (!IsOwner || !IsAdmin.Value)
            {
                adminOpen = false;
                return;
            }

            var mobileAdmin = MobileControls.Current != null && MobileControls.Current.ConsumeAdmin();
            if (!Input.GetKeyDown(KeyCode.F2) && !mobileAdmin) return;
            adminOpen = !adminOpen;
            Cursor.lockState = adminOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = adminOpen;
        }

        private void OnGUI()
        {
            if (IsOwner && Time.unscaledTime < localChatConfirmationExpires)
                GUI.Label(new Rect(Screen.width * 0.5f - 210f, Screen.height - 145f, 420f, 30f),
                    localChatConfirmation);
            if (!IsOwner || !IsAdmin.Value || !adminOpen) return;
            adminHeading ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            var panel = new Rect(20, Screen.height / 2f - 120, 300, 230);
            GUI.Box(panel, "");
            GUI.Label(new Rect(panel.x + 18, panel.y + 15, 260, 30), "NETWORK ADMIN", adminHeading);
            GUI.Label(new Rect(panel.x + 18, panel.y + 48, 260, 25), "Authorized Player ID");
            if (GUI.Button(new Rect(panel.x + 18, panel.y + 82, 264, 42), "SPAWN 10 BALLS ABOVE ME"))
                AdminSpawnBallsServerRpc();
            if (GUI.Button(new Rect(panel.x + 18, panel.y + 136, 264, 42), "DESPAWN ALL BALLS"))
                AdminDespawnBallsServerRpc();
        }

        [ServerRpc]
        private void AdminSpawnBallsServerRpc()
        {
            if (!IsAdmin.Value || Time.unscaledTime < nextAdminSpawn) return;
            nextAdminSpawn = Time.unscaledTime + 1f;
            if (FindObjectsOfType<NetworkBall>().Length >= 50) return;
            var prefab = Resources.Load<GameObject>("NetworkBall");
            if (prefab == null) return;
            for (var i = 0; i < 10; i++)
            {
                var position = transform.position + new Vector3((i % 5 - 2) * 0.65f, 3.5f + i / 5 * 0.65f, 0);
                var ball = Instantiate(prefab, position, Quaternion.identity);
                var networkObject = ball.GetComponent<NetworkObject>();
                networkObject.Spawn();
                foreach (var clientId in NetworkManager.ConnectedClientsIds)
                    if (!networkObject.IsNetworkVisibleTo(clientId)) networkObject.NetworkShow(clientId);
            }
        }

        [ServerRpc]
        private void AdminDespawnBallsServerRpc()
        {
            if (!IsAdmin.Value) return;
            foreach (var ball in FindObjectsOfType<NetworkBall>())
            {
                var networkObject = ball.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned) networkObject.Despawn(true);
            }
            CleanupLocalBallCopiesClientRpc();
        }

        [ClientRpc]
        private void CleanupLocalBallCopiesClientRpc()
        {
            foreach (var localBall in FindObjectsOfType<Grabbable>())
                if (localBall.GetComponent<NetworkBall>() == null)
                    Destroy(localBall.gameObject);
        }

        public void SendChatMessage(string message)
        {
            if (!IsOwner) return;
            SendChatMessageServerRpc(message);
        }

        [ServerRpc]
        private void SendChatMessageServerRpc(string message)
        {
            if (Time.unscaledTime < nextChatAllowedAt) return;
            nextChatAllowedAt = Time.unscaledTime + 0.75f;
            message = string.IsNullOrWhiteSpace(message) ? "" : message.Trim();
            if (message.Length > 80) message = message.Substring(0, 80);
            if (message.Length > 0) ShowChatMessageClientRpc(message);
        }

        [ClientRpc]
        private void ShowChatMessageClientRpc(string message)
        {
            if (chatBubble == null) return;
            chatBubble.text = message;
            chatExpiresAt = Time.unscaledTime + 5f;
            if (IsOwner)
            {
                localChatConfirmation = "You: " + message;
                localChatConfirmationExpires = Time.unscaledTime + 5f;
            }
        }

        private void CreateChatBubble()
        {
            var bubbleObject = new GameObject("Player Chat Bubble");
            bubbleObject.transform.SetParent(transform, false);
            chatBubble = bubbleObject.AddComponent<TextMesh>();
            chatBubble.anchor = TextAnchor.LowerCenter;
            chatBubble.alignment = TextAlignment.Center;
            chatBubble.fontSize = 48;
            chatBubble.characterSize = 0.022f;
            chatBubble.color = new Color(0.3f, 0.9f, 1f);
            chatBubble.richText = false;
            chatBubbleRenderer = chatBubble.GetComponent<MeshRenderer>();
            chatBubbleRenderer.enabled = false;
        }

        private void UpdateChatBubble()
        {
            if (chatBubble == null || chatBubbleRenderer == null) return;
            var camera = Camera.main;
            var ownerInThirdPerson = IsOwner && localMotor != null && localMotor.IsThirdPerson;
            var visible = (!IsOwner || ownerInThirdPerson) && Time.unscaledTime < chatExpiresAt && camera != null;
            chatBubble.transform.localPosition = new Vector3(0, syncedCrouching.Value ? 1.85f : 2.45f, 0);
            if (visible)
            {
                chatBubble.transform.rotation = Quaternion.LookRotation(chatBubble.transform.position - camera.transform.position);
                var blocked = Physics.Linecast(camera.transform.position, chatBubble.transform.position,
                    out var obstruction, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                    !obstruction.transform.IsChildOf(transform);
                visible = !blocked && Vector3.Distance(camera.transform.position, chatBubble.transform.position) <= 35f;
            }
            chatBubbleRenderer.enabled = visible;
        }

        public override void OnNetworkDespawn()
        {
            syncedName.OnValueChanged -= OnNameChanged;
        }

        public void SetDisplayName(string requestedName)
        {
            if (!IsOwner) return;
            requestedName = CleanName(requestedName);
            PlayerPrefs.SetString("BlubRoom.Username", requestedName);
            PlayerPrefs.Save();
            if (IsServer) syncedName.Value = requestedName;
            else SetDisplayNameServerRpc(requestedName);
        }

        [ServerRpc]
        private void SetDisplayNameServerRpc(string requestedName)
        {
            syncedName.Value = CleanName(requestedName);
        }

        private static string CleanName(string value)
        {
            value = string.IsNullOrWhiteSpace(value) ? "Player" : value.Trim();
            return value.Length > 20 ? value.Substring(0, 20) : value;
        }

        private void CreateNameTag()
        {
            var tagObject = new GameObject("Player Name Tag");
            tagObject.transform.SetParent(transform, false);
            nameTag = tagObject.AddComponent<TextMesh>();
            nameTag.anchor = TextAnchor.MiddleCenter;
            nameTag.alignment = TextAlignment.Center;
            nameTag.fontSize = 64;
            nameTag.characterSize = 0.025f;
            nameTag.color = Color.white;
            nameTag.text = "Player";
            nameTagRenderer = nameTag.GetComponent<MeshRenderer>();
            nameTagRenderer.enabled = !IsOwner;
        }

        private void OnNameChanged(FixedString64Bytes previous, FixedString64Bytes current)
        {
            if (nameTag != null)
                nameTag.text = current.Length == 0 ? "Player" : current.ToString();
        }

        [ServerRpc]
        private void SubmitTransformServerRpc(Vector3 position, Quaternion rotation, bool crouching)
        {
            var now = Time.unscaledTime;
            var elapsed = Mathf.Max(now - lastClientUpdateTime, 0.02f);
            var delta = position - lastClientPosition;
            var velocity = delta / elapsed;
            var horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            var hasGroundBelow = Physics.Raycast(position + Vector3.up * 0.2f, Vector3.down,
                4.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var validBlubCenterRespawn = SceneManager.GetActiveScene().name == "BlubCenter" &&
                lastClientPosition.y < -8f && Vector3.Distance(position, new Vector3(0f, 4f, -3.5f)) < 3f;
            if (validBlubCenterRespawn) antiCheatGraceUntil = now + 1f;
            var impossibleTeleport = delta.magnitude > 8f && !validBlubCenterRespawn;
            var airborneFling = !hasGroundBelow && (horizontalSpeed > 18f || velocity.y > 14f);

            if (now >= antiCheatGraceUntil && (airborneFling || impossibleTeleport))
            {
                airborneFlingStrikes += impossibleTeleport ? 2 : 1;
                if (airborneFlingStrikes >= 3)
                {
                    Debug.LogWarning("Anti-cheat kicked client " + OwnerClientId +
                        " for impossible airborne movement.");
                    NetworkManager.DisconnectClient(OwnerClientId,
                        "Anti-cheat: impossible airborne movement detected");
                }
                return;
            }

            airborneFlingStrikes = Mathf.Max(0, airborneFlingStrikes - 1);
            lastClientPosition = position;
            lastClientUpdateTime = now;

            syncedPosition.Value = position;
            syncedRotation.Value = rotation;
            syncedCrouching.Value = crouching;
        }

        private void CreateAvatar()
        {
            avatar = new GameObject("Avatar Visual");
            avatar.transform.SetParent(transform, false);
            var fallback = new GameObject("Fallback");
            fallback.transform.SetParent(avatar.transform, false);
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.HSVToRGB((OwnerClientId * 0.23f) % 1f, 0.75f, 1f);

            var body = MakePart("Body", PrimitiveType.Capsule, new Vector3(0, 0.72f, 0), new Vector3(0.62f, 0.58f, 0.62f), material);
            var head = MakePart("Head", PrimitiveType.Sphere, new Vector3(0, 1.55f, 0), Vector3.one * 0.48f, material);
            var leftArm = MakePart("Left Arm", PrimitiveType.Capsule, new Vector3(-0.48f, 0.93f, 0), new Vector3(0.2f, 0.48f, 0.2f), material);
            var rightArm = MakePart("Right Arm", PrimitiveType.Capsule, new Vector3(0.48f, 0.93f, 0), new Vector3(0.2f, 0.48f, 0.2f), material);
            leftArm.transform.localRotation = Quaternion.Euler(0, 0, -12);
            rightArm.transform.localRotation = Quaternion.Euler(0, 0, 12);
            // Every player uses this same lightweight multiplayer avatar.
            // The first-person owner only hides the head to keep it out of the camera.
        }

        private GameObject MakePart(string partName, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(avatar.transform.Find("Fallback"), false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Destroy(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().material = material;
            return part;
        }
    }
}
