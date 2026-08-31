using Unity.Netcode;
using UnityEngine;

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
        }
    }
}
