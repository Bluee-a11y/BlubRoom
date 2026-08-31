using Unity.Netcode;
using UnityEngine;

namespace ClubhousePC
{
    public sealed class MakerTool : MonoBehaviour
    {
        public Camera View;
        private bool menuOpen;
        private PrimitiveType? placingType;
        private bool choosingMoveTarget;
        private GameObject movingBlock;
        private NetworkMakerBlock movingNetworkBlock;
        private Collider movingCollider;
        private string hint = "F: Maker menu";

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F)) ToggleMenu();
            if (menuOpen || View == null) return;

            if ((movingBlock != null || movingNetworkBlock != null) && TryAimPoint(out var movePoint))
            {
                var target = Snap(movePoint + Vector3.up * 0.5f);
                if (movingBlock != null)
                    movingBlock.transform.position = Vector3.Lerp(movingBlock.transform.position, target, 18f * Time.deltaTime);
                else
                    movingNetworkBlock.MoveTo(target);
            }

            if (!Input.GetMouseButtonDown(0)) return;
            if (placingType.HasValue && TryAimPoint(out var point))
            {
                var position = Snap(point + Vector3.up * 0.5f);
                var networkBuilder = FindLocalNetworkBuilder();
                if (networkBuilder != null) networkBuilder.Place(placingType.Value, position);
                else CreateBlock(placingType.Value, point);
                placingType = null;
                hint = "Block placed — F: Maker menu";
            }
            else if (choosingMoveTarget && Physics.Raycast(View.transform.position, View.transform.forward, out var hit, 30f))
            {
                movingNetworkBlock = hit.collider.GetComponentInParent<NetworkMakerBlock>();
                movingBlock = movingNetworkBlock == null ? hit.collider.GetComponentInParent<MakerBlock>()?.gameObject : null;
                if (movingNetworkBlock != null) movingNetworkBlock.BeginMove();
                var selected = movingNetworkBlock != null ? movingNetworkBlock.gameObject : movingBlock;
                movingCollider = selected != null ? selected.GetComponent<Collider>() : null;
                if (movingCollider != null) movingCollider.enabled = false;
                choosingMoveTarget = false;
                hint = movingBlock == null ? "Aim at a placed block and try again" : "Move mouse, then left-click to place";
            }
            else if (movingBlock != null || movingNetworkBlock != null)
            {
                if (movingNetworkBlock != null) movingNetworkBlock.EndMove();
                if (movingCollider != null) movingCollider.enabled = true;
                movingBlock = null;
                movingNetworkBlock = null;
                movingCollider = null;
                hint = "Block moved — F: Maker menu";
            }
        }

        private static MakerBuildNetwork FindLocalNetworkBuilder()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return null;
            foreach (var builder in FindObjectsOfType<MakerBuildNetwork>())
                if (builder.IsOwner) return builder;
            return null;
        }

        private void ToggleMenu()
        {
            menuOpen = !menuOpen;
            Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = menuOpen;
        }

        private bool TryAimPoint(out Vector3 point)
        {
            if (Physics.Raycast(View.transform.position, View.transform.forward, out var hit, 40f))
            {
                point = hit.point;
                return true;
            }
            point = default;
            return false;
        }

        private void CreateBlock(PrimitiveType type, Vector3 point)
        {
            var block = GameObject.CreatePrimitive(type);
            block.name = "Maker " + type;
            block.transform.localScale = Vector3.one;
            block.transform.position = Snap(point + Vector3.up * 0.5f);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = type == PrimitiveType.Cube ? new Color(1f, 0.3f, 0.08f) :
                type == PrimitiveType.Sphere ? new Color(0.08f, 0.75f, 0.95f) : new Color(0.65f, 0.3f, 1f);
            block.GetComponent<Renderer>().material = material;
            block.AddComponent<MakerBlock>();
        }

        private static Vector3 Snap(Vector3 value) => new(
            Mathf.Round(value.x * 2f) * 0.5f,
            Mathf.Round(value.y * 2f) * 0.5f,
            Mathf.Round(value.z * 2f) * 0.5f);

        private void OnGUI()
        {
            GUI.Box(new Rect(18, 18, 330, menuOpen ? 230 : 44), "");
            GUI.Label(new Rect(30, 29, 300, 25), hint);
            if (!menuOpen) return;
            GUI.Label(new Rect(30, 60, 290, 24), "MAKER PALETTE");
            if (GUI.Button(new Rect(30, 90, 90, 38), "CUBE")) BeginPlace(PrimitiveType.Cube);
            if (GUI.Button(new Rect(130, 90, 90, 38), "SPHERE")) BeginPlace(PrimitiveType.Sphere);
            if (GUI.Button(new Rect(230, 90, 90, 38), "CYLINDER")) BeginPlace(PrimitiveType.Cylinder);
            if (GUI.Button(new Rect(30, 142, 290, 38), "MOVE A BLOCK"))
            {
                choosingMoveTarget = true;
                hint = "Aim at your block and left-click";
                ToggleMenu();
            }
            GUI.Label(new Rect(30, 192, 290, 28), "Blocks snap to a 0.5-unit grid");
        }

        private void BeginPlace(PrimitiveType type)
        {
            placingType = type;
            hint = "Aim at a surface and left-click to place";
            ToggleMenu();
        }
    }

    public sealed class MakerBlock : MonoBehaviour { }
}
