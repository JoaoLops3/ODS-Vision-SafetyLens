using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Conjunto de objetos de cena que representa um item do cenário na RA:
    /// o corpo (placa ou modelo), o anel de destaque no plano do marcador e o rótulo.
    /// </summary>
    public sealed class SafetyLensHazardVisual
    {
        public Transform Root;
        public Transform Body;
        public Renderer Renderer;
        public Material Material;
        public Transform Ring;
        public Material RingMaterial;
        public Color ActiveRingColor;
        public GameObject Label;
        /// <summary>Modelo 3D (capacete) — precisa ser "levantado" quando o marcador está em pé.</summary>
        public bool IsModel;
        /// <summary>Tamanho do modelo em espaço local, medido na criação (sem rotação).</summary>
        public Vector3 ModelSize;

        public void DestroyMaterials()
        {
            if (Material != null) Object.Destroy(Material);
            if (RingMaterial != null) Object.Destroy(RingMaterial);
            Material = null;
            RingMaterial = null;
        }
    }

    /// <summary>
    /// Fábrica dos visuais de RA. Placas ficam como adesivos no plano do marcador (sempre
    /// legíveis de frente); o capacete é o volume 3D. Materiais em Resources para o build.
    /// </summary>
    public static class SafetyLensArVisuals
    {
        public const string ModelHelmet = "helmet";

        public const string TransparentShader = "Sprites/Default";
        public const string LitShader = "Standard";
        public const string FallbackShader = "Unlit/Color";

        public const string SignMaterialPath = "SafetyLens/Materials/SignUnlit";
        public const string RingMaterialPath = "SafetyLens/Materials/RingUnlit";
        public const string HelmetMaterialPath = "SafetyLens/Materials/HelmetLit";
        public const string FallbackMaterialPath = "SafetyLens/Materials/FallbackUnlit";
        public const string HelmetPrefabPath = "SafetyLens/Prefabs/Helmet";

        public static SafetyLensHazardVisual Create(SafetyLensHazard hazard, Transform parent)
        {
            var visual = new SafetyLensHazardVisual();

            var root = new GameObject($"SafetyLens_{hazard.kind}_{hazard.id}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = hazard.LocalPosition;
            visual.Root = root.transform;

            if (hazard.IsEpi)
                BuildModel(hazard, visual);
            else
                BuildSign(hazard, visual);

            var tag = visual.Body.gameObject.AddComponent<SafetyLensHazardTag>();
            tag.hazardId = hazard.id;

            BuildRing(hazard, visual);
            BuildLabel(hazard, visual);
            return visual;
        }

        /// <summary>
        /// O conteúdo usa Y = normal do marcador e Z = "para cima" visível. Marcador deitado: o capacete
        /// fica apoiado (Y para cima). Marcador em pé (tela/parede): o capacete gira para ficar de pé no
        /// mundo, com a frente para quem olha, e sai do plano o suficiente para não "afundar" na tela.
        /// </summary>
        public static void ApplyMounting(SafetyLensHazardVisual visual, bool markerIsVertical)
        {
            if (!visual.IsModel || visual.Body == null) return;

            if (markerIsVertical)
            {
                visual.Body.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                visual.Body.localPosition = new Vector3(0f, visual.ModelSize.z * 0.5f, -visual.ModelSize.y * 0.5f);
            }
            else
            {
                visual.Body.localRotation = Quaternion.identity;
                visual.Body.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// Placa colada no plano do marcador (eixo Y do tracked image = normal).
        /// Sem billboard: de frente para o equipamento a placa é um adesivo legível;
        /// o modo "em pé + yawOnly" degenerava e deixava tudo de lado.
        /// </summary>
        static void BuildSign(SafetyLensHazard hazard, SafetyLensHazardVisual visual)
        {
            var width = SafetyLensTheme.ArSignWidth;
            var lift = SafetyLensTheme.ArSignLift;

            var body = new GameObject("Sign");
            body.transform.SetParent(visual.Root, false);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Face";
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(body.transform, false);
            // Quad nativo está no plano XY olhando +Z; +90° em X deita no plano XZ olhando +Y (normal do marcador).
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localPosition = new Vector3(0f, lift, 0f);
            quad.transform.localScale = new Vector3(width, width, 1f);

            var collider = body.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, lift, 0f);
            collider.size = new Vector3(width, Mathf.Max(0.008f, lift * 2f), width);

            var material = Instantiate(SignMaterialPath, TransparentShader);
            material.mainTexture = SafetyLensPictograms.GetSign(hazard.ResolvedPictogram);
            var renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            visual.Body = body.transform;
            visual.Renderer = renderer;
            visual.Material = material;
        }

        /// <summary>Modelo do EPI (prefab normalizado no Editor). Sem prefab, usa um cilindro.</summary>
        static void BuildModel(SafetyLensHazard hazard, SafetyLensHazardVisual visual)
        {
            GameObject body = null;
            if (hazard.ResolvedModel == ModelHelmet)
            {
                var prefab = Resources.Load<GameObject>(HelmetPrefabPath);
                if (prefab != null) body = Object.Instantiate(prefab);
            }

            if (body == null)
            {
                Debug.LogWarning($"[SafetyLens] Prefab '{hazard.ResolvedModel}' ausente em Resources; usando primitivo.");
                body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var w = SafetyLensTheme.ArHelmetWidth;
                body.transform.localScale = new Vector3(w, w * 0.3f, w);
                body.transform.localPosition = new Vector3(0f, w * 0.3f, 0f);
            }

            body.name = "Model";
            body.transform.SetParent(visual.Root, false);

            var material = Instantiate(HelmetMaterialPath, LitShader);
            material.color = SafetyLensTheme.HelmetBase;
            var renderers = body.GetComponentsInChildren<Renderer>(true);
            Renderer main = null;
            var bounds = new Bounds(body.transform.position, Vector3.zero);
            var hasBounds = false;
            foreach (var r in renderers)
            {
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                if (main == null || r.bounds.size.sqrMagnitude > main.bounds.size.sqrMagnitude) main = r;
                if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
                else bounds.Encapsulate(r.bounds);
            }

            if (body.GetComponent<Collider>() == null && hasBounds)
            {
                var collider = body.AddComponent<BoxCollider>();
                collider.center = body.transform.InverseTransformPoint(bounds.center);
                var size = body.transform.InverseTransformVector(bounds.size);
                collider.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            }

            visual.Body = body.transform;
            visual.Renderer = main;
            visual.Material = material;
            visual.IsModel = true;
            visual.ModelSize = hasBounds
                ? bounds.size
                : Vector3.one * SafetyLensTheme.ArHelmetWidth * 0.6f;
        }

        /// <summary>Anel no plano do marcador: indica "é este" sem cobrir o pictograma.</summary>
        static void BuildRing(SafetyLensHazard hazard, SafetyLensHazardVisual visual)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "Ring";
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(visual.Root, false);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localPosition = new Vector3(0f, SafetyLensTheme.ArRingLift, 0f);
            ring.transform.localScale = Vector3.one * SafetyLensTheme.ArRingDiameter;

            var material = Instantiate(RingMaterialPath, TransparentShader);
            material.mainTexture = SafetyLensPictograms.GetRing();
            visual.ActiveRingColor = hazard.IsEpi ? SafetyLensTheme.HazardEpiActive : SafetyLensTheme.HazardAlertActive;
            material.color = visual.ActiveRingColor;
            var renderer = ring.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            visual.Ring = ring.transform;
            visual.RingMaterial = material;
            ring.SetActive(false);
        }

        static void BuildLabel(SafetyLensHazard hazard, SafetyLensHazardVisual visual)
        {
            var label = new GameObject($"Label_{hazard.id}");
            label.transform.SetParent(visual.Root, false);
            label.transform.localScale = Vector3.one * SafetyLensTheme.ArLabelScale;
            var billboard = label.AddComponent<SafetyLensBillboard>();
            billboard.anchor = visual.Renderer;
            billboard.gap = SafetyLensTheme.ArLabelGap;

            var text = ShortLabel(hazard);
            CreateLabelText(label.transform, "Shadow", text, SafetyLensTheme.ArLabelShadow,
                new Vector3(0.06f, -0.06f, 0.02f));
            CreateLabelText(label.transform, "Text", text, SafetyLensTheme.ArLabel, Vector3.zero);

            visual.Label = label;
            label.SetActive(false);
        }

        static void CreateLabelText(Transform parent, string name, string text, Color color, Vector3 offset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 64;
            mesh.characterSize = 0.5f;
            mesh.fontStyle = FontStyle.Bold;
            mesh.color = color;
        }

        static string ShortLabel(SafetyLensHazard hazard)
        {
            if (hazard == null) return string.Empty;
            if (!string.IsNullOrWhiteSpace(hazard.label) && hazard.label.Length <= 20)
                return hazard.label;
            return hazard.id ?? string.Empty;
        }

        static Material Instantiate(string resourcePath, string fallbackShader)
        {
            var template = Resources.Load<Material>(resourcePath);
            if (template != null) return new Material(template);

            Debug.LogWarning($"[SafetyLens] Material '{resourcePath}' ausente; rode SafetyLens > Configurar projeto completo.");
            var shader = Shader.Find(fallbackShader);
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");
            return new Material(shader);
        }
    }
}
