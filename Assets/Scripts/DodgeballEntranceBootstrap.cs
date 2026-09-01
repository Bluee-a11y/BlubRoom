using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClubhousePC
{
    public static class DodgeballEntranceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Build()
        {
            if (SceneManager.GetActiveScene().name != "BlubCenter" ||
                GameObject.Find("Dodgeball Launch Area") != null) return;

            var root = new GameObject("Dodgeball Launch Area");
            var blue = Material(new Color(0.04f, 0.28f, 0.86f));
            var red = Material(new Color(0.88f, 0.08f, 0.08f));
            var white = Material(new Color(0.92f, 0.94f, 0.98f));
            var yellow = Material(new Color(1f, 0.72f, 0.05f));

            Box(root.transform, "Blue Preview Half", new Vector3(-11.25f, 0.03f, 8.5f),
                new Vector3(4.5f, 0.08f, 5.5f), blue);
            Box(root.transform, "Red Preview Half", new Vector3(-6.75f, 0.03f, 8.5f),
                new Vector3(4.5f, 0.08f, 5.5f), red);
            Box(root.transform, "Dodgeball Area Sign", new Vector3(-9f, 2.5f, 11.05f),
                new Vector3(7.5f, 1.4f, 0.25f), white);
            Box(root.transform, "Ball Rack", new Vector3(-9f, 0.45f, 8.25f),
                new Vector3(7f, 0.18f, 1.2f), white);
            var start = Box(root.transform, "START DODGEBALL", new Vector3(-9f, 0.55f, 6.5f),
                new Vector3(3.8f, 0.65f, 1.2f), yellow);
            start.AddComponent<DodgeballEntrance>();

            var balls = Object.FindObjectsOfType<Grabbable>();
            var moved = 0;
            foreach (var ball in balls)
            {
                if (!ball.name.Contains("BlubCenter Practice Ball")) continue;
                var row = moved / 3;
                var column = moved % 3;
                ball.transform.position = new Vector3(-11f + column * 2f, 1.05f + row * 0.7f, 8.25f);
                var body = ball.GetComponent<Rigidbody>();
                if (body != null)
                {
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                moved++;
            }
        }

        private static Material Material(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(shader) { color = color };
        }

        private static GameObject Box(Transform parent, string name, Vector3 position,
            Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().material = material;
            return box;
        }
    }

    public sealed class DodgeballEntrance : MonoBehaviour
    {
        private bool loading;
        private GUIStyle buttonStyle;

        private void Update()
        {
            if (loading || !PlayerIsClose()) return;
            if (Input.GetKeyDown(KeyCode.E))
            {
                Enter();
                return;
            }
            if (!Application.isMobilePlatform) return;
            foreach (var touch in Input.touches)
            {
                var point = new Vector2(touch.position.x, Screen.height - touch.position.y);
                if (touch.phase == TouchPhase.Began && StartButtonRect().Contains(point))
                {
                    Enter();
                    return;
                }
            }
        }

        private void OnGUI()
        {
            if (loading || !PlayerIsClose()) return;
            buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 14 };
            var area = StartButtonRect();
            if (Application.isMobilePlatform)
                GUI.Box(area, "START DODGEBALL", buttonStyle);
            else if (GUI.Button(area, "E  START DODGEBALL", buttonStyle))
                Enter();
        }

        private static Rect StartButtonRect()
        {
            var width = Mathf.Min(330f, Screen.width - 40f);
            return new Rect((Screen.width - width) * 0.5f, Screen.height - 105f, width, 62f);
        }

        private bool PlayerIsClose()
        {
            foreach (var motor in FindObjectsOfType<PlayerMotor>())
                if (Vector3.Distance(motor.transform.position, transform.position) <= 4.2f) return true;
            return false;
        }

        private void Enter()
        {
            var menu = FindObjectOfType<MultiplayerMenu>();
            if (menu == null) return;
            loading = true;
            menu.EnterDodgeball();
        }
    }
}
