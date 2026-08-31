using UnityEngine;

namespace ClubhousePC
{
    public static class BlubCenterBootstrap
    {
        private static Material blue;
        private static Material cyan;
        private static Material white;
        private static Material orange;
        private static Material dark;

        public static void Build()
        {
            blue = MakeMaterial(new Color(0.04f, 0.24f, 0.62f));
            cyan = MakeMaterial(new Color(0.05f, 0.72f, 0.88f));
            white = MakeMaterial(new Color(0.9f, 0.94f, 0.98f));
            orange = MakeMaterial(new Color(1f, 0.28f, 0.05f));
            dark = MakeMaterial(new Color(0.04f, 0.06f, 0.11f));

            BuildPlaza();
            BuildPlayer();
            BuildLighting();
        }

        private static Material MakeMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(shader) { color = color };
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = material;
            return go;
        }

        private static void BuildPlaza()
        {
            Box("BlubCenter Plaza", new Vector3(0, -0.3f, 0), new Vector3(32, 0.6f, 32), white);
            Box("North Hall", new Vector3(0, 3, 14.5f), new Vector3(18, 6, 2), blue);
            Box("West Games Wing", new Vector3(-14.5f, 3, 0), new Vector3(2, 6, 18), blue);
            Box("East Creation Wing", new Vector3(14.5f, 3, 0), new Vector3(2, 6, 18), blue);
            Box("South Social Wing", new Vector3(0, 3, -14.5f), new Vector3(18, 6, 2), blue);

            var fountainBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fountainBase.name = "Blub Fountain";
            fountainBase.transform.position = new Vector3(0, 0.35f, 0);
            fountainBase.transform.localScale = new Vector3(4.5f, 0.35f, 4.5f);
            fountainBase.GetComponent<Renderer>().material = cyan;

            var fountainTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fountainTop.name = "Blub Sculpture";
            fountainTop.transform.position = new Vector3(0, 2.25f, 0);
            fountainTop.transform.localScale = Vector3.one * 2.8f;
            fountainTop.GetComponent<Renderer>().material = blue;

            Box("Welcome Board", new Vector3(0, 3.2f, 13.35f), new Vector3(8, 2.5f, 0.3f), orange);
            Box("Games Sign", new Vector3(-13.35f, 2.8f, 0), new Vector3(0.3f, 2, 6), cyan);
            Box("Create Sign", new Vector3(13.35f, 2.8f, 0), new Vector3(0.3f, 2, 6), cyan);

            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI * 0.25f;
                var position = new Vector3(Mathf.Cos(angle) * 7.5f, 0.6f, Mathf.Sin(angle) * 7.5f);
                var bench = Box("Plaza Bench " + (i + 1), position, new Vector3(2.8f, 0.7f, 0.8f), dark);
                bench.transform.LookAt(new Vector3(0, 0.6f, 0));
            }

            for (var i = 0; i < 6; i++)
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = "BlubCenter Practice Ball " + (i + 1);
                ball.transform.position = new Vector3(-3.5f + i * 1.4f, 1f, 8f);
                ball.transform.localScale = Vector3.one * 0.55f;
                ball.GetComponent<Renderer>().material = i % 2 == 0 ? orange : cyan;
                ball.AddComponent<Rigidbody>().mass = 0.5f;
                ball.AddComponent<Grabbable>();
            }
        }

        private static void BuildPlayer()
        {
            var player = new GameObject("BlubCenter Desktop Player");
            player.transform.position = new Vector3(0, 1.1f, -9f);
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

        private static void BuildLighting()
        {
            RenderSettings.ambientLight = new Color(0.55f, 0.62f, 0.75f);
            var sun = new GameObject("BlubCenter Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
        }
    }
}
