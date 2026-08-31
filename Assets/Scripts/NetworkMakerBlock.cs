using Unity.Netcode;
using UnityEngine;

namespace ClubhousePC
{
    public sealed class NetworkMakerBlock : NetworkBehaviour
    {
        public readonly NetworkVariable<int> Shape = new();
        private ulong mover = ulong.MaxValue;
        private Vector3 moveTarget;

        public override void OnNetworkSpawn()
        {
            ApplyShape(Shape.Value);
            Shape.OnValueChanged += OnShapeChanged;
        }

        public override void OnNetworkDespawn() => Shape.OnValueChanged -= OnShapeChanged;
        private void OnShapeChanged(int previous, int current) => ApplyShape(current);

        private void ApplyShape(int shape)
        {
            var primitive = GameObject.CreatePrimitive((PrimitiveType)Mathf.Clamp(shape, 0, 5));
            GetComponent<MeshFilter>().sharedMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            var oldCollider = GetComponent<Collider>();
            if (oldCollider != null) Destroy(oldCollider);
            var meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = GetComponent<MeshFilter>().sharedMesh;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = shape == (int)PrimitiveType.Cube ? new Color(1f, 0.3f, 0.08f) :
                shape == (int)PrimitiveType.Sphere ? new Color(0.08f, 0.75f, 0.95f) : new Color(0.65f, 0.3f, 1f);
            GetComponent<Renderer>().material = material;
            Destroy(primitive);
        }

        public void BeginMove() => BeginMoveServerRpc();
        public void MoveTo(Vector3 position) => MoveServerRpc(position);
        public void EndMove() => EndMoveServerRpc();

        [ServerRpc(RequireOwnership = false)]
        private void BeginMoveServerRpc(ServerRpcParams rpc = default)
        {
            if (mover != ulong.MaxValue && mover != rpc.Receive.SenderClientId) return;
            mover = rpc.Receive.SenderClientId;
            moveTarget = transform.position;
        }

        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
        private void MoveServerRpc(Vector3 position, ServerRpcParams rpc = default)
        {
            if (mover != rpc.Receive.SenderClientId || Vector3.Distance(moveTarget, position) > 6f) return;
            moveTarget = position;
        }

        [ServerRpc(RequireOwnership = false)]
        private void EndMoveServerRpc(ServerRpcParams rpc = default)
        {
            if (mover == rpc.Receive.SenderClientId) mover = ulong.MaxValue;
        }

        private void Update()
        {
            if (!IsServer || mover == ulong.MaxValue) return;
            transform.position = Vector3.Lerp(transform.position, moveTarget,
                1f - Mathf.Exp(-24f * Time.deltaTime));
        }
    }
}
