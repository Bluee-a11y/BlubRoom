using UnityEngine;

namespace ClubhousePC
{
    public sealed class MobileControls : MonoBehaviour
    {
        public static MobileControls Current { get; private set; }
        public Vector2 Move { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public bool PushToTalkHeld { get; private set; }
        public bool Crouching { get; private set; }

        private int moveFinger = -1;
        private int lookFinger = -1;
        private Vector2 moveOrigin;
        private Vector2 previousLook;
        private bool jump;
        private bool grab;
        private bool throwBall;
        private bool watch;
        private bool admin;

        public bool Enabled => Application.isMobilePlatform;

        private void Awake()
        {
            if (Current != null && Current != this) Destroy(Current);
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            if (!Enabled) return;
            LookDelta = Vector2.zero;
            PushToTalkHeld = false;

            foreach (var touch in Input.touches)
            {
                var position = new Vector2(touch.position.x, touch.position.y);
                if (IsOverButton(position)) continue;

                if (touch.phase == TouchPhase.Began)
                {
                    if (position.x < Screen.width * 0.45f && position.y < Screen.height * 0.55f && moveFinger < 0)
                    {
                        moveFinger = touch.fingerId;
                        moveOrigin = position;
                    }
                    else if (lookFinger < 0)
                    {
                        lookFinger = touch.fingerId;
                        previousLook = position;
                    }
                }

                if (touch.fingerId == moveFinger)
                {
                    Move = Vector2.ClampMagnitude((position - moveOrigin) / 90f, 1f);
                    if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled)
                    {
                        moveFinger = -1;
                        Move = Vector2.zero;
                    }
                }
                else if (touch.fingerId == lookFinger)
                {
                    LookDelta += position - previousLook;
                    previousLook = position;
                    if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled) lookFinger = -1;
                }
            }
        }

        private bool IsOverButton(Vector2 p)
        {
            var fromTop = Screen.height - p.y;
            return p.x > Screen.width - 310 && fromTop > Screen.height - 290 ||
                   p.x > Screen.width - 190 && fromTop < 240;
        }

        private void OnGUI()
        {
            if (!Enabled) return;

            GUI.Box(new Rect(28, Screen.height - 218, 180, 180), "MOVE");
            var knob = new Vector2(118, Screen.height - 128) + Move * 65f;
            GUI.Box(new Rect(knob.x - 24, knob.y - 24, 48, 48), "●");

            if (GUI.Button(new Rect(Screen.width - 142, Screen.height - 152, 120, 55), "JUMP")) jump = true;
            if (GUI.Button(new Rect(Screen.width - 142, Screen.height - 217, 120, 55), Crouching ? "STAND" : "CROUCH"))
                Crouching = !Crouching;
            if (GUI.Button(new Rect(Screen.width - 278, Screen.height - 152, 120, 55), "GRAB")) grab = true;
            if (GUI.Button(new Rect(Screen.width - 142, Screen.height - 87, 120, 55), "THROW")) throwBall = true;
            PushToTalkHeld = GUI.RepeatButton(new Rect(Screen.width - 278, Screen.height - 87, 120, 55), "HOLD TO TALK");
            if (GUI.Button(new Rect(Screen.width - 142, 18, 120, 48), "WATCH")) watch = true;
            foreach (var player in FindObjectsOfType<NetworkPlayer>())
                if (player.IsOwner && player.IsAdmin.Value &&
                    GUI.Button(new Rect(Screen.width - 278, 18, 120, 48), "ADMIN")) admin = true;
        }

        public Vector2 ConsumeLook() { var value = LookDelta; LookDelta = Vector2.zero; return value; }
        public bool ConsumeJump() => Consume(ref jump);
        public bool ConsumeGrab() => Consume(ref grab);
        public bool ConsumeThrow() => Consume(ref throwBall);
        public bool ConsumeWatch() => Consume(ref watch);
        public bool ConsumeAdmin() => Consume(ref admin);

        private static bool Consume(ref bool value)
        {
            var result = value;
            value = false;
            return result;
        }
    }
}
