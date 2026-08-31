using UnityEngine;

namespace ClubhousePC
{
    public sealed class AdminPanel : MonoBehaviour
    {
        private bool open;
        private GUIStyle heading;

        private void Update()
        {
            var mobileAdmin = MobileControls.Current != null && MobileControls.Current.ConsumeAdmin();
            if (!Input.GetKeyDown(KeyCode.F2) && !Input.GetKeyDown(KeyCode.BackQuote) && !mobileAdmin) return;
            open = !open;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }

        private void OnGUI()
        {
            if (!open) return;

            heading ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            var panel = new Rect(20, Screen.height / 2f - 100, 300, 180);
            GUI.Box(panel, "");
            GUI.Label(new Rect(panel.x + 18, panel.y + 15, 260, 30), "ADMIN PANEL", heading);
            GUI.Label(new Rect(panel.x + 18, panel.y + 48, 260, 25), "F2 closes this panel");

            if (GUI.Button(new Rect(panel.x + 18, panel.y + 82, 264, 42), "SPAWN 10 BALLS ABOVE ME"))
                SpawnBalls(10);
        }

        private void SpawnBalls(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = "Admin Ball";
                var column = i % 5;
                var row = i / 5;
                ball.transform.position = transform.position +
                    new Vector3((column - 2) * 0.65f, 3.5f + row * 0.65f, Random.Range(-0.25f, 0.25f));
                ball.transform.localScale = Vector3.one * 0.5f;
                var material = new Material(Shader.Find("Standard"));
                material.color = Color.HSVToRGB(i / (float)count, 0.8f, 1f);
                ball.GetComponent<Renderer>().material = material;
                ball.AddComponent<Rigidbody>().mass = 0.5f;
                ball.AddComponent<Grabbable>();
            }
        }
    }
}
