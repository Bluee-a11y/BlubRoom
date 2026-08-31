using System;
using System.IO;
using GLTFast;
using UnityEngine;

namespace ClubhousePC
{
    public sealed class ImportedAvatarVisual : MonoBehaviour
    {
        public GameObject Fallback;
        public bool HideHeadForFirstPerson;

        private async void Start()
        {
            var filePath = Path.Combine(Application.streamingAssetsPath, "Avatar", "source", "Asfandyar.glb");
            var uri = filePath.Contains("://") ? filePath : new Uri(filePath).AbsoluteUri;
            var import = new GltfImport();
            if (!await import.Load(uri))
            {
                Debug.LogError("BlubRoom avatar failed to load from " + uri);
                return;
            }

            var modelRoot = new GameObject("Asfandyar Avatar");
            modelRoot.transform.SetParent(transform, false);
            if (!await import.InstantiateMainSceneAsync(modelRoot.transform))
            {
                Destroy(modelRoot);
                return;
            }

            FitToPlayer(modelRoot);
            if (HideHeadForFirstPerson) HideHeadMeshes(modelRoot);
            if (Fallback != null) Fallback.SetActive(false);
        }

        private static void FitToPlayer(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y < 0.01f) return;

            var scale = 1.75f / bounds.size.y;
            root.transform.localScale = Vector3.one * scale;
            var localMinimum = root.transform.parent.InverseTransformPoint(bounds.min).y;
            root.transform.localPosition = new Vector3(0, -localMinimum * scale, 0);
            root.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }

        private static void HideHeadMeshes(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var name = renderer.gameObject.name.ToLowerInvariant();
                if (name.Contains("head") || name.Contains("face") || name.Contains("eye") ||
                    name.Contains("teeth") || name.Contains("hair"))
                    renderer.enabled = false;
            }
        }
    }
}
