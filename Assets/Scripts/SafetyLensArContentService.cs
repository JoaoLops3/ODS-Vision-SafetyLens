using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Overlays 3D filhos do <see cref="UnityEngine.XR.ARFoundation.ARTrackedImage"/>:
    /// criação, alinhamento UV, tint por estado e pick RF03. Sem plane detection.
    /// </summary>
    public sealed class SafetyLensArContentService
    {
        readonly Dictionary<string, SafetyLensHazardVisual> _visuals = new();
        GameObject _contentRoot;
        Transform _qrViewerPivot;
        Camera _arCamera;
        bool _qrViewerActive;
        float _qrViewerPitch;

        /// <summary>Visuais por hazard id (toque e pulso).</summary>
        public IReadOnlyDictionary<string, SafetyLensHazardVisual> Visuals => _visuals;

        /// <summary>True depois de <see cref="Ensure"/>.</summary>
        public bool HasContent => _contentRoot != null;

        /// <summary>True no modo QR: models na frente da câmera, desacoplados do Image Target.</summary>
        public bool IsQrViewerActive => _qrViewerActive;

        /// <summary>Cria o conteúdo uma vez e ancora no transform do marcador.</summary>
        public void Ensure(SafetyLensScenario scenario)
        {
            if (scenario?.hazards == null) return;
            if (_contentRoot != null) return;

            _contentRoot = new GameObject(SafetyLensAppConstants.ArContentRootName);
            foreach (var hazard in scenario.hazards)
                _visuals[hazard.id] = SafetyLensArVisuals.Create(hazard, _contentRoot.transform);

            foreach (var visual in _visuals.Values)
                SafetyLensArVisuals.ApplyMounting(visual);
        }

        /// <summary>Mostra ou esconde HOT/PINCH/EPI (ocultos no quiz e na telemetria).</summary>
        public void SetContentActive(bool active)
        {
            if (_contentRoot != null)
                _contentRoot.SetActive(active);
        }

        /// <summary>
        /// Parentaliza no marcador e posiciona por UV (letras A/B/C ou altTargets).
        /// Placas billboardam; o EPI fica no espaço do marcador.
        /// Não usar no caminho QR — ver <see cref="EnterQrViewer"/>.
        /// </summary>
        public void AlignToMarker(
            Transform marker,
            Vector2 markerSize,
            SafetyLensScenario scenario,
            string sessionTargetName,
            bool targetFound)
        {
            if (_qrViewerActive) return;
            if (_contentRoot == null || !targetFound || marker == null || scenario?.hazards == null)
                return;

            if (_contentRoot.transform.parent != marker)
                _contentRoot.transform.SetParent(marker, false);

            _contentRoot.transform.localPosition = Vector3.zero;
            _contentRoot.transform.localRotation = Quaternion.identity;
            _contentRoot.transform.localScale = Vector3.one;

            var size = markerSize;
            if (size.x < 1e-4f || size.y < 1e-4f)
                size = new Vector2(SafetyLensTheme.ArMarkerWidth, SafetyLensTheme.ArMarkerHeight);

            foreach (var hazard in scenario.hazards)
            {
                if (!_visuals.TryGetValue(hazard.id, out var visual) || visual.Root == null) continue;
                var uv = scenario.ResolveUv(hazard, sessionTargetName);
                visual.Root.localPosition = hazard.LocalPositionOnMarker(size, uv.x, uv.y);
                if (visual.Zone != null)
                {
                    var zonePos = hazard.LocalPositionOnMarker(size, uv.x, uv.y);
                    zonePos.y = SafetyLensTheme.ArZoneLift;
                    visual.Zone.localPosition = zonePos;
                }
            }

            FaceHazardsTowardCamera();
        }

        /// <summary>
        /// Caminho QR: tira os models do Image Target e coloca um pivot na frente da câmera
        /// para orbit com o dedo. O marcador WL03 não chama este método.
        /// </summary>
        public void EnterQrViewer(Camera camera, SafetyLensScenario scenario)
        {
            if (scenario?.hazards == null) return;
            Ensure(scenario);

            var cam = camera != null ? camera : ResolveArCamera();
            if (cam == null || _contentRoot == null) return;

            if (_qrViewerPivot == null)
            {
                var pivotGo = new GameObject("SafetyLens_QrViewerPivot");
                _qrViewerPivot = pivotGo.transform;
            }

            _qrViewerPivot.SetParent(cam.transform, false);
            _qrViewerPivot.localPosition = new Vector3(0f, 0f, SafetyLensTheme.QrViewerDistance);
            _qrViewerPivot.localRotation = Quaternion.identity;
            _qrViewerPitch = 0f;

            _contentRoot.transform.SetParent(_qrViewerPivot, false);
            _contentRoot.transform.localPosition = Vector3.zero;
            _contentRoot.transform.localRotation = Quaternion.identity;
            _contentRoot.transform.localScale = Vector3.one * SafetyLensTheme.QrViewerRootScale;

            var count = scenario.hazards.Length;
            for (var i = 0; i < count; i++)
            {
                var hazard = scenario.hazards[i];
                if (!_visuals.TryGetValue(hazard.id, out var visual) || visual.Root == null) continue;

                var local = SafetyLensQrViewerLayout.LocalPositionForHazard(i, count);
                visual.Root.localPosition = local;
                visual.Root.localRotation = Quaternion.identity;
                if (visual.Zone != null)
                {
                    var zonePos = local;
                    zonePos.y = SafetyLensTheme.ArZoneLift;
                    visual.Zone.localPosition = zonePos;
                }
            }

            FaceHazardsTowardCamera();
            _qrViewerActive = true;
        }

        /// <summary>Sai do viewer QR (reinício, fim da inspeção ou caminho marcador).</summary>
        public void ExitQrViewer()
        {
            _qrViewerActive = false;
            _qrViewerPitch = 0f;
            if (_contentRoot != null
                && _qrViewerPivot != null
                && _contentRoot.transform.parent == _qrViewerPivot)
                _contentRoot.transform.SetParent(null, true);

            if (_qrViewerPivot != null)
            {
                Object.Destroy(_qrViewerPivot.gameObject);
                _qrViewerPivot = null;
            }
        }

        /// <summary>Orbit do pivot QR: yaw/pitch locais com pitch limitado.</summary>
        public void ApplyQrViewerOrbit(float deltaYawDegrees, float deltaPitchDegrees)
        {
            if (!_qrViewerActive || _qrViewerPivot == null) return;

            var yaw = _qrViewerPivot.localEulerAngles.y + deltaYawDegrees;
            _qrViewerPitch = Mathf.Clamp(
                _qrViewerPitch + deltaPitchDegrees,
                -SafetyLensTheme.QrViewerPitchClamp,
                SafetyLensTheme.QrViewerPitchClamp);
            _qrViewerPivot.localRotation = Quaternion.Euler(_qrViewerPitch, yaw, 0f);
        }

        /// <summary>Tint, escala, anel e zona conforme progresso e fase.</summary>
        public void Refresh(SafetyLensScenario scenario, SafetyLensInspectionProgress progress, AppPhase phase)
        {
            if (scenario?.hazards == null || _visuals.Count == 0 || progress == null) return;

            for (var i = 0; i < scenario.hazards.Length; i++)
            {
                var hazard = scenario.hazards[i];
                if (!_visuals.TryGetValue(hazard.id, out var visual)) continue;

                var done = progress.IsHazardDone(i);
                var active = phase == AppPhase.RiskInspection && !done && i == progress.ActiveHazardIndex;

                Color tint;
                float scale;
                if (done) { tint = SafetyLensTheme.HazardTintDone; scale = SafetyLensTheme.ArDoneScale; }
                else if (active) { tint = SafetyLensTheme.HazardTintActive; scale = SafetyLensTheme.ArActiveScale; }
                else { tint = SafetyLensTheme.HazardTintIdle; scale = SafetyLensTheme.ArIdleScale; }

                if (visual.Material != null)
                {
                    tint.a = 1f;
                    visual.Material.color = hazard.IsEpi ? SafetyLensTheme.HelmetBase * tint : tint;
                }

                visual.Root.localScale = Vector3.one * scale;
                if (visual.ZoneMaterial != null)
                {
                    var zoneColor = done ? SafetyLensTheme.HazardZoneDone : SafetyLensTheme.HazardZone;
                    zoneColor.a = done
                        ? SafetyLensTheme.ArZoneDoneAlpha
                        : active ? SafetyLensTheme.ArZoneActiveAlpha : SafetyLensTheme.ArZoneIdleAlpha;
                    visual.ZoneMaterial.color = zoneColor;
                }

                if (visual.Ring != null)
                {
                    visual.Ring.gameObject.SetActive(active || done);
                    visual.RingMaterial.color = done ? SafetyLensTheme.HazardDone : visual.ActiveRingColor;
                    visual.Ring.localScale = Vector3.one * (visual.RingDiameter *
                                                            (done ? SafetyLensTheme.ArDoneRingScale : 1f));
                }

                if (visual.Label != null) visual.Label.SetActive(active);
            }
        }

        /// <summary>Pulso do anel e da zona do item ativo. Respeita reduce motion.</summary>
        public void PulseActive(SafetyLensHazard current, float now)
        {
            if (current == null || !_visuals.TryGetValue(current.id, out var visual)) return;

            if (visual.Ring != null && visual.Ring.gameObject.activeSelf)
            {
                var pulse = SafetyLensAccessibility.ActiveRingPulseScale(now);
                visual.Ring.localScale = Vector3.one * (visual.RingDiameter * pulse);
            }

            if (visual.ZoneMaterial != null)
            {
                var zoneColor = SafetyLensTheme.HazardZone;
                zoneColor.a = SafetyLensAccessibility.ZonePulseAlpha(
                    SafetyLensTheme.ArZoneActiveAlpha, now);
                visual.ZoneMaterial.color = zoneColor;
            }
        }

        /// <summary>
        /// RF03: raycast no collider com tag; fallback por raio mínimo em tela.
        /// </summary>
        public SafetyLensHazardTag PickHazard(Camera camera, Vector2 screenPosition)
        {
            if (camera == null) return null;

            if (Physics.Raycast(
                    camera.ScreenPointToRay(screenPosition),
                    out var hit,
                    SafetyLensAppConstants.HazardRaycastMaxDistance))
            {
                var direct = hit.collider.GetComponent<SafetyLensHazardTag>();
                if (direct != null) return direct;
            }

            var pointsToPixels = Mathf.Min(Screen.width, Screen.height) / SafetyLensTheme.ReferenceWidth;
            var minRadius = SafetyLensTheme.TouchMin * pointsToPixels;
            SafetyLensHazardTag best = null;
            var bestDistance = float.MaxValue;

            foreach (var visual in _visuals.Values)
            {
                var renderer = visual.Renderer;
                if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;

                var bounds = renderer.bounds;
                var center = camera.WorldToScreenPoint(bounds.center);
                if (center.z <= 0f) continue;

                var edge = camera.WorldToScreenPoint(bounds.center + camera.transform.right * bounds.extents.magnitude);
                var radius = Mathf.Max(minRadius, Vector2.Distance(center, edge));
                var distance = Vector2.Distance(screenPosition, center);
                if (distance > radius || distance >= bestDistance) continue;

                bestDistance = distance;
                best = visual.Body.GetComponent<SafetyLensHazardTag>();
            }

            return best;
        }

        /// <summary>Resolve a câmera AR uma vez e reutiliza.</summary>
        public Camera ResolveArCamera()
        {
            if (_arCamera != null) return _arCamera;

            var manager = Object.FindFirstObjectByType<ARCameraManager>(FindObjectsInactive.Include);
            if (manager != null)
                _arCamera = manager.GetComponent<Camera>();
            if (_arCamera == null)
                _arCamera = Camera.main;
            return _arCamera;
        }

        /// <summary>Destroi o root e os materiais de runtime (restart / OnDestroy).</summary>
        public void DestroyContent()
        {
            foreach (var visual in _visuals.Values)
                visual.DestroyMaterials();
            _visuals.Clear();

            ExitQrViewer();

            if (_contentRoot != null)
            {
                Object.Destroy(_contentRoot);
                _contentRoot = null;
            }

            _arCamera = null;
        }

        void FaceHazardsTowardCamera()
        {
            var cam = ResolveArCamera();
            if (cam == null) return;

            var camPos = cam.transform.position;
            var camUp = cam.transform.up;
            foreach (var visual in _visuals.Values)
            {
                if (visual.Root == null) continue;

                if (visual.IsModel)
                {
                    visual.Root.localRotation = Quaternion.identity;
                    continue;
                }

                var toCam = camPos - visual.Root.position;
                if (toCam.sqrMagnitude < 1e-8f) continue;
                visual.Root.rotation = Quaternion.LookRotation(toCam.normalized, camUp);
            }
        }
    }
}
