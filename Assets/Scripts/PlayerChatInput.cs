using UnityEngine;

namespace ClubhousePC
{
    public sealed class PlayerChatInput : MonoBehaviour
    {
        public NetworkPlayer Player;
        private bool open;
        private bool focusNextFrame;
        private string message = "";

        private void Update()
        {
            if (!open && Input.GetKeyDown(KeyCode.C))
            {
                open = true;
                focusNextFrame = true;
                SetCursor(true);
            }

            if (!open) return;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!string.IsNullOrWhiteSpace(message)) Player.SendChatMessage(message);
                message = "";
                open = false;
                SetCursor(false);
            }
        }

        private static void SetCursor(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void OnGUI()
        {
            if (!open) return;
            var width = Mathf.Min(620f, Screen.width - 40f);
            var area = new Rect((Screen.width - width) * 0.5f, Screen.height - 95f, width, 58f);
            GUI.Box(area, "");
            GUI.SetNextControlName("BlubChatField");
            message = GUI.TextField(new Rect(area.x + 12f, area.y + 13f, area.width - 24f, 32f), message, 80);
            if (focusNextFrame)
            {
                GUI.FocusControl("BlubChatField");
                if (GUI.GetNameOfFocusedControl() == "BlubChatField")
                    focusNextFrame = false;
            }
            GUI.Label(new Rect(area.x + 12f, area.y - 22f, area.width - 24f, 20f),
                "Chat — press Enter to send");
        }
    }
}
