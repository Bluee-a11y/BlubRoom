using UnityEngine;

namespace ClubhousePC
{
    public sealed class PrototypeHUD : MonoBehaviour
    {
        private GUIStyle title;
        private GUIStyle crosshair;
        private Font arcadeFont;

        private void OnGUI()
        {
            arcadeFont ??= Resources.Load<Font>("Fonts/PressStart2P-Regular");
            title ??= new GUIStyle(GUI.skin.label)
            {
                font = arcadeFont,
                fontSize = 15,
                fontStyle = FontStyle.Normal,
                normal = { textColor = Color.white }
            };
            crosshair ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(18, 14, 500, 32), "BLUBROOM — ALPHA a1.3.2_014", title);
            GUI.Label(new Rect(Screen.width / 2 - 8, Screen.height / 2 - 14, 20, 30), "+", crosshair);
        }
    }
}
