using Unity.Netcode;
using UnityEngine;

namespace ClubhousePC
{
    public sealed class MakerBuildNetwork : NetworkBehaviour
    {
        public void Place(PrimitiveType type, Vector3 position) => PlaceServerRpc((int)type, position);

        [ServerRpc]
        private void PlaceServerRpc(int shape, Vector3 position)
        {
            if (FindObjectsOfType<NetworkMakerBlock>().Length >= 250) return;
            var prefab = Resources.Load<GameObject>("NetworkMakerBlock");
            if (prefab == null) return;
            var block = Instantiate(prefab, position, Quaternion.identity);
            block.GetComponent<NetworkMakerBlock>().Shape.Value = shape;
            block.GetComponent<NetworkObject>().Spawn();
        }
    }
}
