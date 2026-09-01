using UnityEngine;

namespace ClubhousePC
{
    public static class DodgeballBootstrap
    {
        private static Material blue;
        private static Material red;
        private static Material white;
        private static Material dark;
        private static Material yellow;

        public static void Build()
        {
            blue = MakeMaterial(new Color(0.04f, 0.28f, 0.86f));
            red = MakeMaterial(new Color(0.88f, 0.08f, 0.08f));
            white = MakeMaterial(new Color(0.92f, 0.94f, 0.98f));
            dark = MakeMaterial(new Color(0.025f, 0.035f, 0.07f));
            yellow = MakeMaterial(new Color(1f, 0.72f, 0.05f));

            BuildArena();
            BuildPlayer();
            BuildBalls();
            BuildLighting();
            new GameObject("Dodgeball Game Controller").AddComponent<DodgeballGameController>();
        }

        private static Material MakeMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(shader) { color = color };
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().material = material;
            return box;
        }

        private static void BuildArena()
        {
            Box("Blue Court", new Vector3(0f, -0.3f, -4f), new Vector3(24f, 0.6f, 8f), blue);
            Box("Red Court", new Vector3(0f, -0.3f, 4f), new Vector3(24f, 0.6f, 8f), red);
            Box("Center Line", new Vector3(0f, 0.015f, 0f), new Vector3(24f, 0.04f, 0.28f), white);
            Box("Blue Back Wall", new Vector3(0f, 2.4f, -8.2f), new Vector3(24.8f, 4.8f, 0.4f), blue);
            Box("Red Back Wall", new Vector3(0f, 2.4f, 8.2f), new Vector3(24.8f, 4.8f, 0.4f), red);
            Box("West Arena Wall", new Vector3(-12.2f, 2.4f, 0f), new Vector3(0.4f, 4.8f, 16.8f), dark);
            Box("East Arena Wall", new Vector3(12.2f, 2.4f, 0f), new Vector3(0.4f, 4.8f, 16.8f), dark);
            Box("Blue Spectator Deck", new Vector3(0f, 0.25f, -10.5f), new Vector3(10f, 0.5f, 2.5f), dark);
            Box("Red Spectator Deck", new Vector3(0f, 0.25f, 10.5f), new Vector3(10f, 0.5f, 2.5f), dark);

            Box("Blue Team Sign", new Vector3(0f, 4.2f, -7.9f), new Vector3(7f, 1.2f, 0.22f), white);
            Box("Red Team Sign", new Vector3(0f, 4.2f, 7.9f), new Vector3(7f, 1.2f, 0.22f), white);

            for (var x = -9f; x <= 9f; x += 3f)
                Box("Center Ball Marker", new Vector3(x, 0.025f, 0f), new Vector3(0.7f, 0.05f, 0.7f), yellow);
        }

        private static void BuildPlayer()
        {
            var player = new GameObject("Dodgeball Desktop Player");
            player.transform.position = new Vector3(0f, 1.1f, -5.5f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            var cameraObject = new GameObject("Player Camera");
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();
            player.AddComponent<PlayerMotor>().View = cameraObject.transform;
            player.AddComponent<DesktopInteractor>().View = camera;
            player.AddComponent<PrototypeHUD>();
            player.AddComponent<MobileControls>();
        }

        private static void BuildBalls()
        {
            for (var i = 0; i < 7; i++)
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = "Dodgeball " + (i + 1);
                ball.transform.position = new Vector3(-9f + i * 3f, 0.65f, 0f);
                ball.transform.localScale = Vector3.one * 0.62f;
                ball.GetComponent<Renderer>().material = yellow;
                var body = ball.AddComponent<Rigidbody>();
                body.mass = 0.55f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                ball.AddComponent<Grabbable>();
            }
        }

        private static void BuildLighting()
        {
            RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.68f);
            var light = new GameObject("Dodgeball Arena Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        }
    }
}
