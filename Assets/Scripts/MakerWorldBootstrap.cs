using UnityEngine;

namespace ClubhousePC
{
    public static class MakerWorldBootstrap
    {
        public static void Build()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var floorMaterial = new Material(shader) { color = new Color(0.12f, 0.42f, 0.72f) };
            var gridMaterial = new Material(shader) { color = new Color(0.86f, 0.9f, 0.96f) };

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "MakerWorld Building Platform";
            floor.transform.position = new Vector3(0, -0.25f, 0);
            floor.transform.localScale = new Vector3(50, 0.5f, 50);
            floor.GetComponent<Renderer>().material = floorMaterial;

            for (var i = -20; i <= 20; i += 2)
            {
                MakeGridLine(new Vector3(i, 0.015f, 0), new Vector3(0.025f, 0.025f, 40), gridMaterial);
                MakeGridLine(new Vector3(0, 0.016f, i), new Vector3(40, 0.025f, 0.025f), gridMaterial);
            }

            var player = new GameObject("MakerWorld Player");
            player.transform.position = new Vector3(0, 1.1f, -8f);
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
            player.AddComponent<MakerTool>().View = camera;
            player.AddComponent<MobileControls>();

            RenderSettings.ambientLight = new Color(0.55f, 0.62f, 0.74f);
            var sun = new GameObject("MakerWorld Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(50, -35, 0);
        }

        private static void MakeGridLine(Vector3 position, Vector3 scale, Material material)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "Grid Line";
            line.transform.position = position;
            line.transform.localScale = scale;
            line.GetComponent<Renderer>().material = material;
            Object.Destroy(line.GetComponent<Collider>());
        }
    }
}
