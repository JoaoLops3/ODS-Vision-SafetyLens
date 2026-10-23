using System.Collections.Generic;
using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Compressor do caminho QR: nasce depois do scan, no fundo escuro, e gira só na horizontal.
    /// O marcador WL03 não usa esta classe.
    /// </summary>
    public sealed class SafetyLensQrMachineView
    {
        const string UnlitMaterialPath = "SafetyLens/Materials/FallbackUnlit";

        readonly List<SafetyLensQrMachinePoint> _points = new List<SafetyLensQrMachinePoint>();
        readonly List<Material> _materials = new List<Material>();

        Transform _pivot;
        Transform _yaw;
        Camera _camera;
        SafetyLensQrMachinePoint _selected;

        public bool IsActive => _pivot != null;
        public SafetyLensQrMachinePoint Selected => _selected;

        /// <summary>Coloca o compressor na frente da câmera. Um scan basta: o caller desliga o feed.</summary>
        public void Show(Camera camera)
        {
            Clear();
            if (camera == null) return;

            _camera = camera;
            var pivotGo = new GameObject("SafetyLens_QrMachinePivot");
            _pivot = pivotGo.transform;
            _pivot.SetParent(camera.transform, false);
            _pivot.localPosition = new Vector3(0f, 0f, SafetyLensTheme.QrMachineDistance);
            _pivot.localRotation = Quaternion.identity;

            var yawGo = new GameObject("SafetyLens_QrMachineYaw");
            _yaw = yawGo.transform;
            _yaw.SetParent(_pivot, false);

            if (TryBuildImportedCompressor())
            {
                FitYawToScreen();
                AddPointsOnSurface();
            }
            else
            {
                _yaw.localScale = Vector3.one * SafetyLensTheme.QrMachineScale;
                BuildCompressor();
                foreach (var spot in SafetyLensQrMachineCatalog.CreateSpots())
                    AddPoint(spot, spot.LocalPosition, spot.LocalOutwardNormal);
            }

            // A origem da malha é o chão da base. Sem este passo o corpo fica acima do meio da tela.
            CenterOnCameraAxis();
            RefreshFacing();
        }

        /// <summary>Giro horizontal. Pitch fica em zero: a máquina não tomba.</summary>
        public void ApplyYaw(float deltaDegrees)
        {
            if (_yaw == null) return;
            var yaw = _yaw.localEulerAngles.y + deltaDegrees;
            _yaw.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>Fecha a tabela se a face do ponto saiu de frente. Atualiza o destaque das marcas.</summary>
        public void RefreshFacing()
        {
            if (_yaw == null || _camera == null) return;

            var best = float.NegativeInfinity;
            for (var i = 0; i < _points.Count; i++)
            {
                var dot = DotTowardViewer(_points[i]);
                if (dot > best) best = dot;
            }

            if (_selected != null && !SafetyLensQrMachineCatalog.IsOnViewerFace(DotTowardViewer(_selected), best))
                _selected = null;

            for (var i = 0; i < _points.Count; i++)
            {
                var point = _points[i];
                var facing = SafetyLensQrMachineCatalog.IsOnViewerFace(DotTowardViewer(point), best);
                var selected = point == _selected;
                point.SetAppearance(facing ? SafetyLensTheme.Brand : SafetyLensTheme.HazardIdle, PointLocalDiameter(selected));
            }
        }

        /// <summary>
        /// Toque curto. Só abre a tabela se a marca estiver na face virada para a pessoa.
        /// Toque numa face de lado ou de trás não troca a tabela aberta.
        /// </summary>
        public bool TrySelect(Vector2 screenPosition)
        {
            if (_camera == null || _yaw == null) return false;

            var hitPoint = RaycastPoint(screenPosition);
            if (hitPoint != null)
            {
                if (!IsOnViewerFace(hitPoint)) return false;
                _selected = hitPoint;
                RefreshFacing();
                return true;
            }

            var fallback = NearestFacingPoint(screenPosition);
            if (fallback == null) return false;
            _selected = fallback;
            RefreshFacing();
            return true;
        }

        public void Clear()
        {
            _selected = null;
            _points.Clear();
            _camera = null;
            _yaw = null;

            for (var i = 0; i < _materials.Count; i++)
            {
                if (_materials[i] != null) Object.Destroy(_materials[i]);
            }
            _materials.Clear();

            if (_pivot != null)
            {
                Object.Destroy(_pivot.gameObject);
                _pivot = null;
            }
        }

        bool IsOnViewerFace(SafetyLensQrMachinePoint point)
        {
            var best = float.NegativeInfinity;
            for (var i = 0; i < _points.Count; i++)
            {
                var dot = DotTowardViewer(_points[i]);
                if (dot > best) best = dot;
            }

            return SafetyLensQrMachineCatalog.IsOnViewerFace(DotTowardViewer(point), best);
        }

        float DotTowardViewer(SafetyLensQrMachinePoint point)
        {
            var normal = _yaw.TransformDirection(point.LocalOutwardNormal);
            var toViewer = _camera.transform.position - point.transform.position;
            return SafetyLensQrMachineCatalog.DotTowardViewer(normal, toViewer);
        }

        SafetyLensQrMachinePoint RaycastPoint(Vector2 screenPosition)
        {
            if (!Physics.Raycast(_camera.ScreenPointToRay(screenPosition), out var hit, 8f))
                return null;
            return hit.collider.GetComponent<SafetyLensQrMachinePoint>();
        }

        SafetyLensQrMachinePoint NearestFacingPoint(Vector2 screenPosition)
        {
            var pointsToPixels = Mathf.Min(Screen.width, Screen.height) / SafetyLensTheme.ReferenceWidth;
            var minRadius = SafetyLensTheme.TouchMin * pointsToPixels;
            SafetyLensQrMachinePoint best = null;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < _points.Count; i++)
            {
                var point = _points[i];
                if (!IsOnViewerFace(point)) continue;

                var center = _camera.WorldToScreenPoint(point.transform.position);
                if (center.z <= 0f) continue;

                var distance = Vector2.Distance(screenPosition, center);
                if (distance > minRadius || distance >= bestDistance) continue;
                bestDistance = distance;
                best = point;
            }

            return best;
        }

        /// <summary>
        /// Leva o centro do volume (peças + marcas) para o eixo da câmera, que é o meio da tela.
        /// </summary>
        void CenterOnCameraAxis()
        {
            var renderers = _yaw.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var center = _pivot.InverseTransformPoint(bounds.center);
            _yaw.localPosition = new Vector3(-center.x, -center.y, -center.z);
        }

        /// <summary>
        /// FBX em Resources. Sem o prefab (build antigo) volta para as primitivas.
        /// </summary>
        bool TryBuildImportedCompressor()
        {
            var prefab = Resources.Load<GameObject>(SafetyLensArVisuals.CompressorPrefabPath);
            if (prefab == null) return false;

            var instance = Object.Instantiate(prefab, _yaw, false);
            instance.name = "AirCompressor";
            SafetyLensArVisuals.StripImportedSceneGadgets(instance);
            // O arquivo apoia o chão no Z=0. O giro é em Y; sem este alinhamento a base vira para a câmera.
            Upright(instance);

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderers[i].receiveShadows = false;
            }

            return renderers.Length > 0;
        }

        /// <summary>
        /// Põe o eixo em que o arquivo cresce a partir do chão no +Y da tela.
        /// O dedo só gira em Y, então a base não vem para a frente.
        /// </summary>
        void Upright(GameObject instance)
        {
            var filter = instance.GetComponentInChildren<MeshFilter>();
            var up = filter != null ? FileUpAxis(filter.sharedMesh) : Vector3.forward;
            if (up.sqrMagnitude < 1e-8f) up = Vector3.forward;
            instance.transform.localRotation = Quaternion.FromToRotation(up, Vector3.up);
        }

        /// <summary>
        /// Eixo cujo mínimo encosta no zero e o volume fica todo para um lado. No FBX este é o Z, o alto da máquina.
        /// As tampas do tanque têm mais normais, mas são simétricas em volta da origem — não são o chão.
        /// </summary>
        static Vector3 FileUpAxis(Mesh mesh)
        {
            if (mesh == null) return Vector3.forward;

            var bounds = mesh.bounds;
            var bestScore = -1f;
            var best = Vector3.forward;
            for (var axis = 0; axis < 3; axis++)
            {
                var span = bounds.size[axis];
                if (span < 1e-5f) continue;

                var floor = 1f - Mathf.Clamp01(Mathf.Abs(bounds.min[axis]) / span);
                var score = floor * Mathf.Abs(bounds.center[axis]);
                if (score <= bestScore) continue;

                bestScore = score;
                var dir = Vector3.zero;
                dir[axis] = bounds.center[axis] >= 0f ? 1f : -1f;
                best = dir;
            }

            return best;
        }

        /// <summary>
        /// O FBX está em metros reais. Esta escala deixa o maior lado em QrMachineFitSize.
        /// </summary>
        void FitYawToScreen()
        {
            var bounds = BoundsInYawSpace();
            var longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (longest < 1e-4f) return;
            _yaw.localScale = Vector3.one * (SafetyLensTheme.QrMachineFitSize / longest);
        }

        readonly struct ShellSample
        {
            public ShellSample(Vector3 position, Vector3 normal)
            {
                Position = position;
                Normal = normal;
            }

            public Vector3 Position { get; }
            public Vector3 Normal { get; }
        }

        /// <summary>
        /// Tampa no extremo sem roda. A base fica de fora —
        /// a roda tem normal para todo lado e puxava a média para o pneu.
        /// </summary>
        void AddPointsOnSurface()
        {
            var samples = CollectShell();
            var bounds = BoundsInYawSpace();
            var wheelSign = WheelEndSign(samples, bounds);
            var spots = SafetyLensQrMachineCatalog.CreateSpots();
            for (var i = 0; i < spots.Length; i++)
            {
                var sideCap = Mathf.Abs(spots[i].LocalOutwardNormal.x) > 0.5f;
                var normal = sideCap ? Vector3.right * -wheelSign : Vector3.back;
                AddPoint(spots[i], SurfacePointOnPart(samples, bounds, sideCap, normal, wheelSign), normal);
            }

            if (TryGaugePoint(out var gaugePosition, out var gaugeNormal))
                AddPoint(SafetyLensQrMachineCatalog.GaugeSpot(), gaugePosition, gaugeNormal);

            if (TryAirOutletPoint(out var outletPosition, out var outletNormal))
                AddPoint(SafetyLensQrMachineCatalog.AirOutletSpot(), outletPosition, outletNormal);

            if (TryMotorPoint(out var motorPosition, out var motorNormal))
                AddPoint(SafetyLensQrMachineCatalog.MotorSpot(), motorPosition, motorNormal);
        }

        /// <summary>
        /// Os dois mostradores brancos do atlas. O ponto fica no meio deles, um pouco para fora da face.
        /// </summary>
        bool TryGaugePoint(out Vector3 position, out Vector3 normal)
        {
            position = Vector3.zero;
            normal = Vector3.back;
            var sumPosition = Vector3.zero;
            var sumNormal = Vector3.zero;
            var count = 0;
            var filters = _yaw.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var uvs = mesh.uv;
                var vertices = mesh.vertices;
                var normals = mesh.normals;
                if (uvs == null || uvs.Length != vertices.Length || normals == null || normals.Length != vertices.Length)
                    continue;

                var toYaw = _yaw.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;
                for (var v = 0; v < vertices.Length; v++)
                {
                    if (!IsGaugeDial(uvs[v])) continue;
                    sumPosition += toYaw.MultiplyPoint3x4(vertices[v]);
                    var n = toYaw.MultiplyVector(normals[v]);
                    if (n.sqrMagnitude > 1e-8f) sumNormal += n.normalized;
                    count++;
                }
            }

            if (count < 8) return false;

            var outward = sumNormal.sqrMagnitude < 1e-8f ? Vector3.back : sumNormal.normalized;
            outward.y = 0f;
            if (outward.sqrMagnitude < 1e-8f) outward = Vector3.back;
            normal = outward.normalized;
            position = sumPosition / count + normal * PointLiftInYawSpace();
            return true;
        }

        /// <summary>
        /// A casca de cima da mangueira amarela. O normal de toque fica na horizontal,
        /// senão o giro em Y nunca vira essa face para a câmera.
        /// A caixa está em metros do FBX em pé. Multiplicar pela escala da tela
        /// jogava a mangueira para fora da caixa e o ponto não nascia.
        /// </summary>
        bool TryAirOutletPoint(out Vector3 position, out Vector3 normal)
        {
            position = Vector3.zero;
            normal = Vector3.left;

            var sumPosition = Vector3.zero;
            var sumNormal = Vector3.zero;
            var count = 0;
            var filters = _yaw.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var vertices = mesh.vertices;
                var normals = mesh.normals;
                if (normals == null || normals.Length != vertices.Length) continue;

                var toYaw = _yaw.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;
                for (var v = 0; v < vertices.Length; v++)
                {
                    var yawPoint = toYaw.MultiplyPoint3x4(vertices[v]);
                    if (!IsAirOutlet(yawPoint)) continue;
                    var n = toYaw.MultiplyVector(normals[v]);
                    if (n.sqrMagnitude < 1e-8f || n.normalized.y < 0.35f) continue;
                    sumPosition += yawPoint;
                    sumNormal += n.normalized;
                    count++;
                }
            }

            if (count < 8 || sumNormal.sqrMagnitude < 1e-8f) return false;

            var skin = sumNormal.normalized;
            var facing = new Vector3(skin.x, 0f, skin.z);
            if (facing.sqrMagnitude < 1e-6f) facing = Vector3.left;
            normal = facing.normalized;
            position = sumPosition / count + skin * PointLiftInYawSpace();
            return true;
        }

        /// <summary>
        /// O disco cinza das faixas, acima do tanque. A face que olha para fora é a +X.
        /// A caixa está em metros do FBX em pé. O yaw já cancela a escala da tela,
        /// então multiplicar de novo jogava o disco para fora da caixa e o ponto sumia.
        /// </summary>
        bool TryMotorPoint(out Vector3 position, out Vector3 normal)
        {
            position = Vector3.zero;
            normal = Vector3.right;

            var sumPosition = Vector3.zero;
            var sumNormal = Vector3.zero;
            var count = 0;
            var filters = _yaw.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var vertices = mesh.vertices;
                var normals = mesh.normals;
                if (normals == null || normals.Length != vertices.Length) continue;

                var toYaw = _yaw.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;
                for (var v = 0; v < vertices.Length; v++)
                {
                    var yawPoint = toYaw.MultiplyPoint3x4(vertices[v]);
                    if (!IsMotorDisc(yawPoint)) continue;
                    var n = toYaw.MultiplyVector(normals[v]);
                    if (n.sqrMagnitude < 1e-8f || n.normalized.x < 0.55f) continue;
                    sumPosition += yawPoint;
                    sumNormal += n.normalized;
                    count++;
                }
            }

            if (count < 8 || sumNormal.sqrMagnitude < 1e-8f) return false;

            var skin = sumNormal.normalized;
            var facing = new Vector3(skin.x, 0f, skin.z);
            if (facing.sqrMagnitude < 1e-6f) facing = Vector3.right;
            normal = facing.normalized;
            position = sumPosition / count + normal * PointLiftInYawSpace();
            return true;
        }

        /// <summary>
        /// Face do disco com anéis, no espaço em pé do FBX. A malha fica numa lâmina em x ≈ 0,357;
        /// o limite antigo começava em 0,36 e deixava essa face de fora.
        /// </summary>
        static bool IsMotorDisc(Vector3 upright)
        {
            return upright.x >= 0.34f && upright.x <= 0.40f
                && upright.y >= 0.60f && upright.y <= 0.75f
                && upright.z >= -0.09f && upright.z <= 0.06f;
        }

        /// <summary>Trecho da mangueira no alto da bomba, no espaço em pé do FBX.</summary>
        static bool IsAirOutlet(Vector3 upright)
        {
            return upright.x >= -0.22f && upright.x <= -0.08f
                && upright.y >= 0.76f && upright.y <= 0.87f
                && upright.z >= -0.045f && upright.z <= 0.05f;
        }

        /// <summary>Os dois círculos com ponteiro no topo do atlas de cor.</summary>
        static bool IsGaugeDial(Vector2 uv)
        {
            if (uv.y < 0.94f || uv.y > 0.985f) return false;
            var left = uv.x >= 0.238f && uv.x <= 0.276f;
            var right = uv.x >= 0.287f && uv.x <= 0.326f;
            return left || right;
        }

        Vector3 SurfacePointOnPart(
            System.Collections.Generic.List<ShellSample> samples,
            Bounds bounds,
            bool sideCap,
            Vector3 outward,
            float wheelSign)
        {
            var lift = PointLiftInYawSpace();
            var min = HeightMark(bounds, sideCap ? 0.22f : 0.38f);
            var max = HeightMark(bounds, sideCap ? 0.48f : 0.74f);
            if (TryAveragePart(samples, bounds, outward, min, max, wheelSign, true, out var onPart))
                return onPart + outward * lift;
            if (TryAveragePart(samples, bounds, outward, min, max, wheelSign, false, out onPart))
                return onPart + outward * lift;

            return bounds.center + Vector3.Scale(outward, bounds.extents) + outward * lift;
        }

        static float HeightMark(Bounds bounds, float fraction)
        {
            return bounds.min.y + bounds.size.y * fraction;
        }

        /// <summary>Lado do tanque onde os vértices baixos se concentram. +1 é +X.</summary>
        static float WheelEndSign(System.Collections.Generic.List<ShellSample> samples, Bounds bounds)
        {
            var floor = HeightMark(bounds, 0.22f);
            var sum = 0f;
            var count = 0;
            for (var i = 0; i < samples.Count; i++)
            {
                if (samples[i].Position.y > floor) continue;
                sum += samples[i].Position.x;
                count++;
            }

            if (count == 0) return 1f;
            return (sum / count) >= bounds.center.x ? 1f : -1f;
        }

        bool TryAveragePart(
            System.Collections.Generic.List<ShellSample> samples,
            Bounds bounds,
            Vector3 outward,
            float yMin,
            float yMax,
            float wheelSign,
            bool avoidWheelEnd,
            out Vector3 center)
        {
            center = Vector3.zero;
            outward = outward.sqrMagnitude < 1e-8f ? Vector3.back : outward.normalized;
            var maxProj = float.NegativeInfinity;
            var any = false;
            for (var i = 0; i < samples.Count; i++)
            {
                if (!AcceptsPartVertex(samples[i], bounds, outward, yMin, yMax, wheelSign, avoidWheelEnd))
                    continue;
                var proj = Vector3.Dot(samples[i].Position, outward);
                if (proj > maxProj) maxProj = proj;
                any = true;
            }

            if (!any) return false;

            var band = Mathf.Max(bounds.size.magnitude * 0.04f, 1e-4f);
            var sum = Vector3.zero;
            var count = 0;
            for (var i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                if (!AcceptsPartVertex(sample, bounds, outward, yMin, yMax, wheelSign, avoidWheelEnd))
                    continue;
                if (Vector3.Dot(sample.Position, outward) < maxProj - band) continue;
                sum += sample.Position;
                count++;
            }

            if (count == 0) return false;
            center = sum / count;
            return true;
        }

        static bool AcceptsPartVertex(
            ShellSample sample,
            Bounds bounds,
            Vector3 outward,
            float yMin,
            float yMax,
            float wheelSign,
            bool avoidWheelEnd)
        {
            var position = sample.Position;
            if (position.y < yMin || position.y > yMax) return false;
            if (sample.Normal.y > 0.65f) return false;
            if (Vector3.Dot(sample.Normal, outward) < 0.4f) return false;
            if (!avoidWheelEnd) return true;

            var onWheelEnd = wheelSign > 0f
                ? position.x > bounds.center.x + bounds.size.x * 0.18f
                : position.x < bounds.center.x - bounds.size.x * 0.18f;
            return !onWheelEnd;
        }

        System.Collections.Generic.List<ShellSample> CollectShell()
        {
            var samples = new System.Collections.Generic.List<ShellSample>(16000);
            var filters = _yaw.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var vertices = mesh.vertices;
                var normals = mesh.normals;
                if (normals == null || normals.Length != vertices.Length) continue;

                var toYaw = _yaw.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;
                for (var v = 0; v < vertices.Length; v++)
                {
                    var normal = toYaw.MultiplyVector(normals[v]);
                    if (normal.sqrMagnitude < 1e-8f) continue;
                    samples.Add(new ShellSample(toYaw.MultiplyPoint3x4(vertices[v]), normal.normalized));
                }
            }

            return samples;
        }

        float PointLiftInYawSpace()
        {
            var scale = Mathf.Abs(_yaw.lossyScale.x);
            if (scale < 1e-4f) scale = 1f;
            return SafetyLensTheme.QrMachinePointSize * 0.55f / scale;
        }

        Bounds BoundsInYawSpace()
        {
            var filters = _yaw.GetComponentsInChildren<MeshFilter>(true);
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            var has = false;
            for (var i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh == null) continue;
                var toYaw = _yaw.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;
                var center = mesh.bounds.center;
                var ext = mesh.bounds.extents;
                for (var x = -1; x <= 1; x += 2)
                for (var y = -1; y <= 1; y += 2)
                for (var z = -1; z <= 1; z += 2)
                {
                    var corner = toYaw.MultiplyPoint3x4(center + Vector3.Scale(ext, new Vector3(x, y, z)));
                    if (!has)
                    {
                        bounds = new Bounds(corner, Vector3.zero);
                        has = true;
                    }
                    else bounds.Encapsulate(corner);
                }
            }

            return bounds;
        }

        /// <summary>
        /// Diâmetro da marca no espaço do yaw. O pai já está escalado para caber na tela.
        /// </summary>
        float PointLocalDiameter(bool selected)
        {
            var parent = Mathf.Abs(_yaw.lossyScale.x);
            if (parent < 1e-4f) parent = 1f;
            return SafetyLensTheme.QrMachinePointSize * (selected ? 1.25f : 1f) / parent;
        }

        /// <summary>
        /// Compressor de pistão montado com primitivas: base, tanque deitado,
        /// cabeçote com aletas, ventilador atrás e tubo de saída à frente.
        /// Sem collider nestas peças — o toque fica só nas marcas.
        /// </summary>
        void BuildCompressor()
        {
            var alongX = Quaternion.Euler(0f, 0f, 90f);
            var alongZ = Quaternion.Euler(90f, 0f, 0f);
            var body = SafetyLensTheme.MachineBody;
            var motor = SafetyLensTheme.MachineMotor;
            var pipe = SafetyLensTheme.MachinePipe;
            var skid = SafetyLensTheme.MachineSkid;

            Part("Skid", PrimitiveType.Cube, new Vector3(0f, 0.035f, 0f), new Vector3(0.48f, 0.02f, 0.20f), Quaternion.identity, skid);
            Part("RailL", PrimitiveType.Cube, new Vector3(0f, 0.02f, -0.07f), new Vector3(0.50f, 0.016f, 0.028f), Quaternion.identity, skid);
            Part("RailR", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.07f), new Vector3(0.50f, 0.016f, 0.028f), Quaternion.identity, skid);
            Foot(new Vector3(-0.20f, 0.008f, -0.07f), skid);
            Foot(new Vector3(0.20f, 0.008f, -0.07f), skid);
            Foot(new Vector3(-0.20f, 0.008f, 0.07f), skid);
            Foot(new Vector3(0.20f, 0.008f, 0.07f), skid);

            // Tanque deitado no eixo X. O cilindro do Unity nasce em pé: 90° em Z deita ele.
            var tank = new Vector3(0f, 0.135f, 0f);
            Part("Tank", PrimitiveType.Cylinder, tank, new Vector3(0.17f, 0.20f, 0.17f), alongX, body);
            Part("BandA", PrimitiveType.Cylinder, tank + new Vector3(-0.08f, 0f, 0f), new Vector3(0.185f, 0.012f, 0.185f), alongX, motor);
            Part("BandB", PrimitiveType.Cylinder, tank + new Vector3(0.08f, 0f, 0f), new Vector3(0.185f, 0.012f, 0.185f), alongX, motor);
            Part("SideCap", PrimitiveType.Cylinder, new Vector3(0.212f, 0.135f, 0f), new Vector3(0.11f, 0.008f, 0.11f), alongX, pipe);

            var pump = new Vector3(-0.03f, 0.255f, 0.02f);
            Part("Crankcase", PrimitiveType.Cube, pump, new Vector3(0.15f, 0.11f, 0.13f), Quaternion.identity, motor);
            var head = new Vector3(-0.03f, 0.335f, -0.01f);
            Part("Head", PrimitiveType.Cube, head, new Vector3(0.11f, 0.055f, 0.10f), Quaternion.identity, motor);
            for (var i = 0; i < 4; i++)
            {
                Part(
                    "Fin" + i,
                    PrimitiveType.Cube,
                    head + new Vector3(0f, -0.01f + i * 0.016f, 0f),
                    new Vector3(0.145f, 0.006f, 0.125f),
                    Quaternion.identity,
                    i % 2 == 0 ? body : pipe);
            }

            // Ventilador na traseira do cabeçote. 90° em X aponta o cilindro para +Z.
            var fan = new Vector3(-0.03f, 0.255f, 0.125f);
            Part("FanHousing", PrimitiveType.Cylinder, fan, new Vector3(0.13f, 0.035f, 0.13f), alongZ, motor);
            Part("FanHub", PrimitiveType.Cylinder, fan + new Vector3(0f, 0f, 0.04f), new Vector3(0.04f, 0.012f, 0.04f), alongZ, pipe);
            Part("GrillA", PrimitiveType.Cube, fan + new Vector3(0f, 0f, 0.055f), new Vector3(0.11f, 0.008f, 0.008f), Quaternion.identity, pipe);
            Part("GrillB", PrimitiveType.Cube, fan + new Vector3(0f, 0f, 0.055f), new Vector3(0.008f, 0.11f, 0.008f), Quaternion.identity, pipe);
            Part("GrillC", PrimitiveType.Cube, fan + new Vector3(0f, 0f, 0.055f), new Vector3(0.09f, 0.008f, 0.008f), Quaternion.Euler(0f, 0f, 40f), pipe);

            var outlet = new Vector3(-0.03f, 0.33f, -0.16f);
            Part("Outlet", PrimitiveType.Cylinder, outlet, new Vector3(0.045f, 0.08f, 0.045f), alongZ, pipe);
            Part("Flange", PrimitiveType.Cylinder, outlet + new Vector3(0f, 0f, -0.09f), new Vector3(0.08f, 0.012f, 0.08f), alongZ, body);
            Part("Valve", PrimitiveType.Cylinder, outlet + new Vector3(0f, 0.045f, 0.02f), new Vector3(0.055f, 0.01f, 0.055f), Quaternion.identity, motor);
            Part("Handwheel", PrimitiveType.Cube, outlet + new Vector3(0f, 0.055f, 0.02f), new Vector3(0.05f, 0.008f, 0.012f), Quaternion.identity, pipe);

            Part("Gauge", PrimitiveType.Cylinder, head + new Vector3(0.04f, 0.045f, 0.02f), new Vector3(0.04f, 0.012f, 0.04f), Quaternion.identity, pipe);
            Part("GaugeFace", PrimitiveType.Cylinder, head + new Vector3(0.04f, 0.058f, 0.02f), new Vector3(0.026f, 0.004f, 0.026f), Quaternion.identity, motor);
        }

        void Foot(Vector3 position, Color color)
        {
            Part("Foot", PrimitiveType.Cube, position, new Vector3(0.04f, 0.016f, 0.04f), Quaternion.identity, color);
        }

        void AddPoint(SafetyLensQrMachineSpot spot, Vector3 localPosition, Vector3 outwardNormal)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "QrMachinePoint";
            go.transform.SetParent(_yaw, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * PointLocalDiameter(false);

            var point = go.AddComponent<SafetyLensQrMachinePoint>();
            point.WhatItIs = spot.WhatItIs;
            point.WhatCanHappen = spot.WhatCanHappen;
            point.LocalOutwardNormal = outwardNormal.sqrMagnitude < 1e-8f
                ? spot.LocalOutwardNormal
                : outwardNormal.normalized;

            var renderer = go.GetComponent<Renderer>();
            var material = CreateUnlit(SafetyLensTheme.Brand);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            point.Bind(material);
            _points.Add(point);
        }

        Transform Part(string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            go.transform.SetParent(_yaw, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;

            var renderer = go.GetComponent<Renderer>();
            var material = CreateUnlit(color);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        Material CreateUnlit(Color color)
        {
            var template = Resources.Load<Material>(UnlitMaterialPath);
            var material = template != null
                ? new Material(template)
                : new Material(Shader.Find(SafetyLensArVisuals.FallbackShader));
            material.color = color;
            _materials.Add(material);
            return material;
        }
    }
}
