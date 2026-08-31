using UnityEngine;

namespace ClubhousePC
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Grabbable : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        private void Awake() => Body = GetComponent<Rigidbody>();
    }
}
