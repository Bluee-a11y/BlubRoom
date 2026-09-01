using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClubhousePC
{
    public static class RevivalBootstrap
    {
        private static Material warmWhite;
        private static Material orange;
        private static Material blue;
        private static Material dark;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void WatchSceneChanges()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Build();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Build()
        {
            if (Object.FindFirstObjectByType<PlayerMotor>() != null) return;
            // MakerWorld is intentionally stored as an empty scene and generated
            // when entered, so it must be handled before the root-count guard.
            if (SceneManager.GetActiveScene().name == "MakerWorld")
            {
                MakerWorldBootstrap.Build();
                return;
            }
            if (SceneManager.GetActiveScene().name == "Dodgeball")
            {
                DodgeballBootstrap.Build();
                return;
            }
            // Once a scene contains user-created objects it is never regenerated.
            // This protects customized maps and deleted furniture.
            if (SceneManager.GetActiveScene().rootCount > 0) return;
            if (SceneManager.GetActiveScene().name == "BlubCenter")
            {
                BlubCenterBootstrap.Build();
                return;
            }

            warmWhite = Material(new Color(0.88f, 0.84f, 0.74f));
            orange = Material(new Color(1f, 0.25f, 0.07f));
            blue = Material(new Color(0.08f, 0.38f, 0.72f));
            dark = Material(new Color(0.08f, 0.10f, 0.13f));

            MakeRoom();
            MakePlayer();
            MakePlayObjects();
            MakeLight();
        }

        private static Material Material(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            return material;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetPositionAndRotation(position, Quaternion.identity);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = material;
            return go;
        }

        private static void MakeRoom()
        {
            Box("Floor", new Vector3(0, -0.25f, 0), new Vector3(18, 0.5f, 14), warmWhite);
            Box("Back Wall", new Vector3(0, 3, 7), new Vector3(18, 6.5f, 0.35f), blue);
            Box("Left Wall", new Vector3(-9, 3, 0), new Vector3(0.35f, 6.5f, 14), blue);
            Box("Right Wall", new Vector3(9, 3, 0), new Vector3(0.35f, 6.5f, 14), blue);
            Box("Front Wall Left", new Vector3(-5.5f, 3, -7), new Vector3(7, 6.5f, 0.35f), blue);
            Box("Front Wall Right", new Vector3(5.5f, 3, -7), new Vector3(7, 6.5f, 0.35f), blue);

            Box("Loft", new Vector3(-5.5f, 2.7f, 3.9f), new Vector3(6, 0.35f, 4), dark);
            Box("Loft Support A", new Vector3(-8, 1.25f, 2.2f), new Vector3(0.35f, 2.5f, 0.35f), dark);
            Box("Loft Support B", new Vector3(-3, 1.25f, 2.2f), new Vector3(0.35f, 2.5f, 0.35f), dark);
            Box("Sofa", new Vector3(4.8f, 0.55f, 4.8f), new Vector3(4, 1.1f, 1.4f), orange);
            Box("Table", new Vector3(4.8f, 0.7f, 1.9f), new Vector3(3, 0.2f, 1.8f), dark);
            Box("Table Leg", new Vector3(4.8f, 0.3f, 1.9f), new Vector3(0.35f, 0.8f, 0.35f), dark);
            Box("Welcome Sign", new Vector3(0, 3.2f, 6.75f), new Vector3(5, 1.4f, 0.15f), orange);
        }

        private static void MakePlayer()
        {
            var player = new GameObject("Desktop Player");
            player.transform.position = new Vector3(0, 1.1f, -3.5f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0, 0.9f, 0);

            var cameraObject = new GameObject("Player Camera");
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.6f, 0);
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();
            player.AddComponent<PlayerMotor>().View = cameraObject.transform;
            player.AddComponent<DesktopInteractor>().View = camera;
            player.AddComponent<PrototypeHUD>();
            player.AddComponent<MobileControls>();
        }

        private static void MakePlayObjects()
        {
            for (var i = 0; i < 5; i++)
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = "Practice Ball " + (i + 1);
                ball.transform.position = new Vector3(-2 + i, 1.1f, 0.5f);
                ball.transform.localScale = Vector3.one * 0.55f;
                ball.GetComponent<Renderer>().material = i % 2 == 0 ? orange : blue;
                ball.AddComponent<Rigidbody>().mass = 0.5f;
                ball.AddComponent<Grabbable>();
            }

            Box("Target Stand", new Vector3(0, 1.8f, 6.4f), new Vector3(0.25f, 3.6f, 0.25f), dark);
            Box("Target", new Vector3(0, 3.5f, 6.15f), new Vector3(3, 2, 0.25f), warmWhite);
            Box("Target Center", new Vector3(0, 3.5f, 5.95f), new Vector3(1.2f, 1.2f, 0.15f), orange);
        }

        private static void MakeLight()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.color = new Color(1f, 0.92f, 0.78f);
            sun.transform.rotation = Quaternion.Euler(45, -35, 0);
        }
    }
}
