using UnityEngine;
using UnityEngine.Rendering;

namespace FerrariLab
{
    // Lightweight desktop backdrop: one image behind the live, interactive car.
    public sealed class FerrariLabBackdrop : MonoBehaviour
    {
        public Camera view;
        Transform backdrop;
        Material backdropMaterial, ringMaterial, haloMaterial, floorMaterial, plateMaterial;
        GameObject floorLight;
        float imageAspect;

        public void Initialize(FerrariInspectionController controller)
        {
            view = controller.viewCamera;
            var texture = Resources.Load<Texture2D>("Lab/Background");
            if (texture)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Lab background";
                Destroy(quad.GetComponent<Collider>());
                backdrop = quad.transform;
                backdrop.SetParent(view.transform, false);
                backdrop.localPosition = new Vector3(0, 0, 70);
                backdropMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                backdropMaterial.SetTexture("_BaseMap", texture);
                backdropMaterial.SetFloat("_Cull", 0);
                var renderer = quad.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = backdropMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                imageAspect = (float)texture.width / texture.height;
            }
            var plate = controller.displayRoot.Find("BasePlate");
            if (plate)
            {
                // Keep the top at 0.15 m and the wheels 0.01 m above it.
                plate.localPosition = new Vector3(0, .10f, 0);
                plate.localScale = new Vector3(5.5f, .05f, 5.5f);
                var marble = Resources.Load<Material>("Lab/MarbleSurface");
                plateMaterial = marble ? new Material(marble) : new Material(plate.GetComponent<Renderer>().sharedMaterial);
                plate.GetComponent<Renderer>().sharedMaterial = plateMaterial;
            }
            if (controller.parts.Length > 0)
            {
                // The source is unusually tall in this view. Compress its shared root
                // around the tyre contact plane so all part poses still agree.
                var car = controller.parts[0].transform.parent;
                car.localScale = new Vector3(car.localScale.x, .92f, car.localScale.z);
            }
            foreach (Transform child in controller.displayRoot)
            {
                if (!child.name.StartsWith("Plate marker ")) continue;
                var pos = child.localPosition;
                var factor = 2.55f / new Vector2(pos.x, pos.z).magnitude;
                child.localPosition = new Vector3(pos.x * factor, pos.y, pos.z * factor);
            }
            var glow = Resources.Load<Material>("Lab/CyanGlow");
            if (glow)
            {
                ringMaterial = new Material(glow);
                ringMaterial.SetColor("_Color", new Color(.40f, .90f, 1f, .94f));
                haloMaterial = new Material(glow);
                haloMaterial.SetColor("_Color", new Color(.10f, .61f, 1f, .28f));
                floorMaterial = new Material(glow);
                floorMaterial.SetColor("_Color", new Color(.08f, .50f, .85f, .20f));
                floorMaterial.SetFloat("_Radial", 1);
                floorLight = GameObject.CreatePrimitive(PrimitiveType.Quad);
                floorLight.name = "Turntable floor glow";
                Destroy(floorLight.GetComponent<Collider>());
                floorLight.transform.SetParent(controller.displayRoot, false);
                floorLight.transform.localPosition = new Vector3(0, .045f, 0);
                floorLight.transform.localRotation = Quaternion.Euler(90, 0, 0);
                floorLight.transform.localScale = new Vector3(7.8f, 7.8f, 1);
                floorLight.GetComponent<Renderer>().sharedMaterial = floorMaterial;
                Ring(controller.displayRoot, 2.70f, .12f, haloMaterial);
                Ring(controller.displayRoot, 2.70f, .026f, ringMaterial);
                Ring(controller.displayRoot, 2.48f, .010f, haloMaterial);
            }
        }

        void Ring(Transform parent, float radius, float width, Material material)
        {
            var line = new GameObject("Illuminated turntable ring").AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.useWorldSpace = false; line.loop = true; line.positionCount = 128;
            line.sharedMaterial = material; line.widthMultiplier = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 0; i < 128; i++)
            {
                float a = i * Mathf.PI * 2 / 128;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, .158f, Mathf.Sin(a) * radius));
            }
        }

        void LateUpdate()
        {
            if (!backdrop) return;
            float height = 140 * Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f);
            float width = height * view.aspect;
            if (imageAspect > view.aspect) width = height * imageAspect;
            else height = width / imageAspect;
            backdrop.localScale = new Vector3(width, height, 1);
        }
        void OnDestroy()
        {
            if (backdrop) Destroy(backdrop.gameObject);
            if (backdropMaterial) Destroy(backdropMaterial);
            if (ringMaterial) Destroy(ringMaterial);
            if (haloMaterial) Destroy(haloMaterial);
            if (floorMaterial) Destroy(floorMaterial);
            if (plateMaterial) Destroy(plateMaterial);
        }
    }
}
