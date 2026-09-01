using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClubhousePC
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class NetworkBall : NetworkBehaviour
    {
        private Rigidbody body;
        private ulong holder = ulong.MaxValue;
        private Vector3 localHoldPosition;
        private Vector3 serverHoldPosition;
        private readonly NetworkVariable<Vector3> syncedPosition = new();
        private readonly NetworkVariable<Quaternion> syncedRotation = new(Quaternion.identity);
        private float nextStateSync;
        private ulong lastThrower = ulong.MaxValue;
        private bool dodgeballThrowArmed;

        private void Awake() => body = GetComponent<Rigidbody>();

        public override void OnNetworkSpawn()
        {
            body.isKinematic = !IsServer;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = IsServer
                ? CollisionDetectionMode.ContinuousDynamic
                : CollisionDetectionMode.ContinuousSpeculative;
            if (IsServer)
            {
                syncedPosition.Value = transform.position;
                syncedRotation.Value = transform.rotation;
                foreach (var clientId in NetworkManager.ConnectedClientsIds)
                    if (!NetworkObject.IsNetworkVisibleTo(clientId)) NetworkObject.NetworkShow(clientId);
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                if (Time.unscaledTime < nextStateSync) return;
                nextStateSync = Time.unscaledTime + 0.05f;
                syncedPosition.Value = transform.position;
                syncedRotation.Value = transform.rotation;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, syncedPosition.Value,
                    1f - Mathf.Exp(-20f * Time.deltaTime));
                transform.rotation = Quaternion.Slerp(transform.rotation, syncedRotation.Value,
                    1f - Mathf.Exp(-20f * Time.deltaTime));
            }
        }

        public void BeginGrab()
        {
            localHoldPosition = transform.position;
            BeginGrabServerRpc();
        }

        public void MoveHeld(Vector3 position)
        {
            localHoldPosition = position;
            MoveHeldServerRpc(position);
        }

        public void EndGrab(Vector3 throwVelocity)
        {
            EndGrabServerRpc(localHoldPosition, throwVelocity);
        }

        private void FixedUpdate()
        {
            if (!IsServer || holder == ulong.MaxValue) return;
            body.MovePosition(Vector3.Lerp(body.position, serverHoldPosition,
                1f - Mathf.Exp(-30f * Time.fixedDeltaTime)));
        }

        [ServerRpc(RequireOwnership = false)]
        private void BeginGrabServerRpc(ServerRpcParams rpc = default)
        {
            if (holder != ulong.MaxValue && holder != rpc.Receive.SenderClientId) return;
            holder = rpc.Receive.SenderClientId;
            dodgeballThrowArmed = false;
            serverHoldPosition = transform.position;
            body.isKinematic = true;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
        private void MoveHeldServerRpc(Vector3 position, ServerRpcParams rpc = default)
        {
            if (holder != rpc.Receive.SenderClientId) return;
            if (Vector3.Distance(serverHoldPosition, position) > 4f) return;
            serverHoldPosition = position;
        }

        [ServerRpc(RequireOwnership = false)]
        private void EndGrabServerRpc(Vector3 releasePosition, Vector3 throwVelocity, ServerRpcParams rpc = default)
        {
            if (holder != rpc.Receive.SenderClientId) return;
            if (Vector3.Distance(transform.position, releasePosition) <= 4f)
                transform.position = releasePosition;
            holder = ulong.MaxValue;
            body.isKinematic = false;
            body.velocity = Vector3.ClampMagnitude(throwVelocity, 15f);
            lastThrower = rpc.Receive.SenderClientId;
            dodgeballThrowArmed = SceneManager.GetActiveScene().name == "Dodgeball" &&
                                  throwVelocity.magnitude >= 3.5f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer || !dodgeballThrowArmed ||
                SceneManager.GetActiveScene().name != "Dodgeball") return;

            var player = collision.collider.GetComponentInParent<NetworkPlayer>();
            if (player != null)
            {
                if (player.OwnerClientId == lastThrower) return;
                player.ServerHitByDodgeball(lastThrower);
            }
            dodgeballThrowArmed = false;
        }

        public void ServerReturnToCourtIfLost()
        {
            if (!IsServer || holder != ulong.MaxValue ||
                SceneManager.GetActiveScene().name != "Dodgeball") return;
            if (transform.position.y >= -3f && Mathf.Abs(transform.position.x) <= 15f &&
                Mathf.Abs(transform.position.z) <= 13f) return;

            var slot = (int)(NetworkObjectId % 7);
            body.isKinematic = false;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(new Vector3(-9f + slot * 3f, 1f, 0f), Quaternion.identity);
            syncedPosition.Value = transform.position;
            syncedRotation.Value = transform.rotation;
            dodgeballThrowArmed = false;
        }
    }
}
