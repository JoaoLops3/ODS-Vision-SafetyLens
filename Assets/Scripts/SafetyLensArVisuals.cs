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
        /// <summary>Zona vermelha de alerta (artigo): disco deitado no plano do marcador, sob a placa.</summary>
        public Transform Zone;
        public Material ZoneMaterial;
        public GameObject Label;
        /// <summary>Diâmetro do anel (placa vs capacete) em metros, sem escala de estado.</summary>
        public float RingDiameter;
        /// <summary>Modelo 3D (capacete) — precisa ser "levantado" quando o marcador está em pé.</summary>
        public bool IsModel;
        /// <summary>Tamanho do modelo em espaço local, medido na criação (sem rotação).</summary>
        public Vector3 ModelSize;

        public void DestroyMaterials()
        {
            if (Material != null) Object.Destroy(Material);
            if (RingMaterial != null) Object.Destroy(RingMaterial);
            if (ZoneMaterial != null) Object.Destroy(ZoneMaterial);
            Material = null;
            RingMaterial = null;
            ZoneMaterial = null;
        }
    }

    /// <summary>
    /// Fábrica dos visuais de RA. Posição vem das UVs A/B/C no marcador; orientação billboard
    /// fica a cargo do SessionController. Materiais em Resources.
    /// </summary>
    public static class SafetyLensArVisuals
    {
        public const string ModelHelmet = "helmet";

        public const string SignShader = "SafetyLens/SignUnlit";
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
            root.transform.localPosition = hazard.LocalPositionOnMarker(
                new Vector2(SafetyLensTheme.ArMarkerWidth, SafetyLensTheme.ArMarkerHeight));
            visual.Root = root.transform;

            if (hazard.IsEpi)
                BuildModel(hazard, visual);
            else
                BuildSign(hazard, visual);

            var tag = visual.Body.gameObject.AddComponent<SafetyLensHazardTag>();
            tag.hazardId = hazard.id;

            BuildRing(hazard, visual);
            BuildLabel(hazard, visual);
            if (hazard.IsAlert) BuildZone(hazard, visual, parent);
            return visual;
        }

        /// <summary>
        /// Zona vermelha de alerta (interface do artigo): disco translúcido deitado no plano do
        /// marcador, sob a placa ISO. Fica fora do Root porque o Root billboarda para a câmera
        /// e a zona precisa permanecer na arte. A posição é sincronizada pelo SessionController.
        /// </summary>
        static void BuildZone(SafetyLensHazard hazard, SafetyLensHazardVisual visual, Transform parent)
        {
            var zone = GameObject.CreatePrimitive(PrimitiveType.Quad);
            zone.name = $"Zone_{hazard.id}";
            Object.Destroy(zone.GetComponent<Collider>());
            zone.transform.SetParent(parent, false);

            var pos = hazard.LocalPositionOnMarker(
                new Vector2(SafetyLensTheme.ArMarkerWidth, SafetyLensTheme.ArMarkerHeight));
            pos.y = SafetyLensTheme.ArZoneLift;
            zone.transform.localPosition = pos;
            // Quad olha +Z; -90° em X deixa o disco deitado olhando +Y (para fora da arte).
            zone.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            zone.transform.localScale = Vector3.one * SafetyLensTheme.ArZoneDiameter;

            var material = Instantiate(RingMaterialPath, TransparentShader);
            material.mainTexture = SafetyLensPictograms.GetZone();
            var color = SafetyLensTheme.HazardZone;
            color.a = SafetyLensTheme.ArZoneIdleAlpha;
            material.color = color;
            var renderer = zone.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            visual.Zone = zone.transform;
            visual.ZoneMaterial = material;
        }

        /// <summary>
        /// Capacete no espaço do marcador: +Y local = normal da arte (sai do plano).
        /// Base do AABB em y=0 → assenta à frente da letra C, sem flutuar “acima” na tela.
        /// </summary>
        public static void ApplyMounting(SafetyLensHazardVisual visual, bool markerIsVertical = false)
        {
            if (!visual.IsModel || visual.Body == null) return;

            visual.Body.localRotation = Quaternion.identity;
            visual.Body.localPosition = Vector3.zero;
            SeatBodyOnMarkerPlane(visual);

            // Anel no plano do marcador, olhando para fora (+Y), não de perfil.
            if (visual.Ring != null)
            {
                visual.Ring.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                visual.Ring.localPosition = new Vector3(0f, SafetyLensTheme.ArRingLift, 0f);
            }
        }

        /// <summary>Recentra XZ e apoia a base do mesh no pivot da âncora UV.</summary>
        static void SeatBodyOnMarkerPlane(SafetyLensHazardVisual visual)
        {
            var renderers = visual.Body.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var local = new Bounds();
            var init = false;
            foreach (var r in renderers)
            {
                var b = r.bounds;
                var min = b.min;
                var max = b.max;
                for (var i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? min.x : max.x,
                        (i & 2) == 0 ? min.y : max.y,
                        (i & 4) == 0 ? min.z : max.z);
                    var lp = visual.Root.InverseTransformPoint(corner);
                    if (!init)
                    {
                        local = new Bounds(lp, Vector3.zero);
                        init = true;
                    }
                    else local.Encapsulate(lp);
                }
            }

            if (!init) return;
            visual.Body.localPosition -= new Vector3(local.center.x, local.min.y, local.center.z);
        }

        /// <summary>
        /// Placa: quad nativo (XY, olha +Z). O root billboard aponta +Z para a câmera;
        /// a posição local vem da UV A/B/C no marcador.
        /// </summary>
        static void BuildSign(SafetyLensHazard hazard, SafetyLensHazardVisual visual)
        {
            var width = SafetyLensTheme.ArSignWidth;

            var body = new GameObject("Sign");
            body.transform.SetParent(visual.Root, false);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Face";
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(body.transform, false);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localScale = new Vector3(width, width, 1f);

            var collider = body.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(width, width, Mathf.Max(0.008f, SafetyLensTheme.ArSignLift * 2f));

            // Cutout double-sided: Sprites/Default em MeshRenderer some com frequência no Metal/ARKit.
            var material = Instantiate(SignMaterialPath, SignShader);
            material.mainTexture = SafetyLensPictograms.GetSign(hazard.ResolvedPictogram);
            if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", 0.15f);
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
            // FBX do capacete traz Camera/Light do Blender; se ficarem ativos, passam a
            // renderizar a cena (fundo azul + vista torta) por cima da Main Camera AR.
            StripImportedSceneGadgets(body);

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

            // Token ArHelmetWidth é a fonte da verdade em runtime — não depende de regenerar o prefab.
            if (hasBounds)
            {
                var footprint = Mathf.Max(bounds.size.x, bounds.size.z);
                if (footprint > 1e-5f)
                    body.transform.localScale *= SafetyLensTheme.ArHelmetWidth / footprint;
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

        /// <summary>Anel no mesmo plano da placa (de frente para a câmera).</summary>
        static void BuildRing(SafetyLensHazard hazard, SafetyLensHazardVisual visual)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "Ring";
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(visual.Root, false);
            ring.transform.localRotation = Quaternion.identity;
            ring.transform.localPosition = new Vector3(0f, 0f, -SafetyLensTheme.ArRingLift);
            visual.RingDiameter = SafetyLensTheme.RingDiameterFor(hazard.IsEpi);
            ring.transform.localScale = Vector3.one * visual.RingDiameter;

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
            if (template != null)
            {
                var copy = new Material(template);
                // Garante o shader cutout mesmo se o .mat no Resources ainda apontar para Sprites/Default.
                if (fallbackShader == SignShader)
                {
                    var sign = Shader.Find(SignShader);
                    if (sign != null) copy.shader = sign;
                }
                return copy;
            }

            Debug.LogWarning($"[SafetyLens] Material '{resourcePath}' ausente; rode SafetyLens > Configurar projeto completo.");
            var shader = Shader.Find(fallbackShader);
            if (shader == null) shader = Shader.Find("Unlit/Transparent Cutout");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");
            return new Material(shader);
        }

        /// <summary>
        /// Remove componentes de cena embutidos no FBX (Camera, Light, AudioListener).
        /// DestroyImmediate no Editor; Destroy em runtime.
        /// </summary>
        public static void StripImportedSceneGadgets(GameObject root)
        {
            if (root == null) return;

            foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            {
                Debug.LogWarning($"[SafetyLens] Removendo Camera importada '{cam.name}' do modelo 3D.");
                // Destroy é adiado — desliga já para não renderizar nem um frame.
                cam.enabled = false;
                var go = cam.gameObject;
                var onlyGadget = go.GetComponent<Renderer>() == null && go.GetComponent<MeshFilter>() == null;
                if (onlyGadget) DestroyObject(go);
                else DestroyObject(cam);
            }

            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
                DestroyObject(light);
            }
        }

        static void DestroyObject(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
