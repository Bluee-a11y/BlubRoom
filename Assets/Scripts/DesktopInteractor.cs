using UnityEngine;

namespace ClubhousePC
{
    public sealed class DesktopInteractor : MonoBehaviour
    {
        public Camera View;
        public float Reach = 3f;
        public float ThrowStrength = 15f;
        private Grabbable held;
        private NetworkBall heldNetworkBall;
        private Vector3 lastPosition;

        private void Update()
        {
            var mobile = MobileControls.Current != null && MobileControls.Current.Enabled;
            var grabPressed = mobile ? MobileControls.Current.ConsumeGrab() : Input.GetMouseButtonDown(0);
            var grabReleased = !mobile && Input.GetMouseButtonUp(0);
            var throwPressed = mobile ? MobileControls.Current.ConsumeThrow() : Input.GetMouseButtonDown(1);

            if (heldNetworkBall != null)
            {
                var holdPoint = View.transform.position + View.transform.forward * 1.4f;
                heldNetworkBall.MoveHeld(holdPoint);
                if (grabReleased || (mobile && grabPressed)) DropNetwork(false);
                if (throwPressed) DropNetwork(true);
            }
            else if (held != null)
            {
                var holdPoint = View.transform.position + View.transform.forward * 1.4f;
                held.Body.MovePosition(Vector3.Lerp(held.Body.position, holdPoint, 18f * Time.deltaTime));
                if (grabReleased || (mobile && grabPressed)) Drop(false);
                if (throwPressed) Drop(true);
            }
            else if (grabPressed &&
                     Physics.Raycast(View.transform.position, View.transform.forward, out var hit, Reach) &&
                     hit.collider.GetComponentInParent<NetworkBall>() is { } networkTarget)
            {
                heldNetworkBall = networkTarget;
                heldNetworkBall.BeginGrab();
            }
            else if (grabPressed &&
                     Physics.Raycast(View.transform.position, View.transform.forward, out hit, Reach) &&
                     hit.collider.GetComponent<Grabbable>() is { } target)
            {
                held = target;
                held.Body.useGravity = false;
                held.Body.velocity = Vector3.zero;
                held.Body.angularVelocity = Vector3.zero;
                lastPosition = held.transform.position;
            }

            if (held != null) lastPosition = held.transform.position;
        }

        private void DropNetwork(bool throwIt)
        {
            heldNetworkBall.EndGrab(throwIt ? View.transform.forward * ThrowStrength : Vector3.zero);
            heldNetworkBall = null;
        }

        private void Drop(bool throwIt)
        {
            held.Body.useGravity = true;
            if (throwIt) held.Body.velocity = View.transform.forward * ThrowStrength;
            held = null;
        }
    }
}
