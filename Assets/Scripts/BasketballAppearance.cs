using System.Collections;
using UnityEngine;

namespace ClubhousePC
{
    public sealed class BasketballAppearance : MonoBehaviour
    {
        private static Material basketballMaterial;
        private static PhysicMaterial bounceMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindObjectOfType<BasketballAppearance>() != null) return;
            var manager = new GameObject("Basketball Appearance").AddComponent<BasketballAppearance>();
            DontDestroyOnLoad(manager.gameObject);
        }

        private IEnumerator Start()
        {
            var texture = Resources.Load<Texture2D>("Textures/BasketballAlbedo");
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            basketballMaterial = new Material(shader)
            {
                name = "BlubRoom Basketball Material",
                mainTexture = texture,
                color = Color.white
            };
            basketballMaterial.SetFloat("_Smoothness", 0.18f);
            bounceMaterial = new PhysicMaterial("Basketball Bounce")
            {
                bounciness = 0.82f,
                bounceCombine = PhysicMaterialCombine.Maximum,
                dynamicFriction = 0.45f,
                staticFriction = 0.5f,
                frictionCombine = PhysicMaterialCombine.Average
            };

            while (true)
            {
                ApplyToAllBalls();
                yield return new WaitForSeconds(0.5f);
            }
        }

        private static void ApplyToAllBalls()
        {
            foreach (var ball in FindObjectsOfType<Grabbable>()) Apply(ball.gameObject);
            foreach (var ball in FindObjectsOfType<NetworkBall>()) Apply(ball.gameObject);
        }

        private static void Apply(GameObject ball)
        {
            if (basketballMaterial == null) return;
            var renderer = ball.GetComponentInChildren<Renderer>();
            if (renderer != null && renderer.sharedMaterial != basketballMaterial)
                renderer.sharedMaterial = basketballMaterial;
            var collider = ball.GetComponentInChildren<Collider>();
            if (collider != null && collider.sharedMaterial != bounceMaterial)
                collider.sharedMaterial = bounceMaterial;
        }
    }
}
