using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Gera em tempo de execução as placas de advertência (triângulo amarelo ISO 7010 com pictograma)
    /// e o anel de destaque usados na RA. Não dependemos de modelos externos: a rasterização é feita
    /// em uma thread de fundo (Task) e só a cópia dos pixels para a GPU acontece na thread principal.
    /// </summary>
    public static class SafetyLensPictograms
    {
        public const string Hot = "hot";
        public const string Pinch = "pinch";
        public const string Generic = "generic";
        const string RingKey = "__ring";

        const int SignSize = 256;
        const int RingSize = 128;

        static readonly Dictionary<string, Task<Color32[]>> Pending = new();
        static readonly Dictionary<string, Texture2D> Ready = new();

        /// <summary>Dispara a rasterização em background para que a textura já exista ao achar o marcador.</summary>
        public static void Prewarm(IEnumerable<string> pictograms)
        {
            foreach (var id in pictograms)
                Queue(Normalize(id), SignSize, (x, y) => SamplePictogram(Normalize(id), x, y));
            Queue(RingKey, RingSize, SampleRing);
        }

        public static Texture2D GetSign(string pictogram) => Resolve(Normalize(pictogram), SignSize);

        public static Texture2D GetRing() => Resolve(RingKey, RingSize);

        public static string Normalize(string pictogram)
        {
            if (string.IsNullOrWhiteSpace(pictogram)) return Generic;
            switch (pictogram.Trim().ToLowerInvariant())
            {
                case Hot: return Hot;
                case Pinch: return Pinch;
                default: return Generic;
            }
        }

        static void Queue(string key, int size, Func<float, float, Color> sampler)
        {
            if (Ready.ContainsKey(key) || Pending.ContainsKey(key)) return;
            Pending[key] = Task.Run(() => Rasterize(size, sampler));
        }

        static Texture2D Resolve(string key, int size)
        {
            if (Ready.TryGetValue(key, out var texture) && texture != null) return texture;

            if (!Pending.TryGetValue(key, out var task))
            {
                Func<float, float, Color> sampler = key == RingKey
                    ? SampleRing
                    : (x, y) => SamplePictogram(key, x, y);
                Queue(key, size, sampler);
                task = Pending[key];
            }

            // Caso raro: o marcador foi achado antes da rasterização terminar. A espera é curta
            // (poucos ms para 256²) e evita exibir um objeto sem textura.
            var pixels = task.GetAwaiter().GetResult();
            Pending.Remove(key);

            texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = $"SafetyLens_{key}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            Ready[key] = texture;
            return texture;
        }

        static Color32[] Rasterize(int size, Func<float, float, Color> sampler)
        {
            var pixels = new Color32[size * size];
            var inv = 1f / size;
            for (var py = 0; py < size; py++)
            {
                for (var px = 0; px < size; px++)
                {
                    // 2x2 supersampling: bordas limpas mesmo com a placa pequena na tela.
                    var c = Color.clear;
                    c += sampler((px + 0.25f) * inv, (py + 0.25f) * inv);
                    c += sampler((px + 0.75f) * inv, (py + 0.25f) * inv);
                    c += sampler((px + 0.25f) * inv, (py + 0.75f) * inv);
                    c += sampler((px + 0.75f) * inv, (py + 0.75f) * inv);
                    c *= 0.25f;
                    pixels[py * size + px] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
                }
            }
            return pixels;
        }

        // ----- Placa triangular -----

        static readonly Vector2 TriA = new(0.5f, 0.96f);
        static readonly Vector2 TriB = new(0.04f, 0.14f);
        static readonly Vector2 TriC = new(0.96f, 0.14f);
        const float TriCorner = 0.045f;
        const float TriBorder = 0.055f;
        const float Aa = 0.006f;

        static Color SamplePictogram(string key, float x, float y)
        {
            var p = new Vector2(x, y);
            var sd = SdTriangle(p, TriA, TriB, TriC) - TriCorner;

            var outside = Mathf.SmoothStep(0f, 1f, (sd + Aa) / (2f * Aa));
            if (outside >= 1f) return Color.clear;

            var innerEdge = Mathf.SmoothStep(0f, 1f, (sd + TriBorder + Aa) / (2f * Aa));
            var fill = Color.Lerp(SafetyLensTheme.SignFace, SafetyLensTheme.SignInk, innerEdge);

            var glyph = key switch
            {
                Hot => GlyphHot(p),
                Pinch => GlyphPinch(p),
                _ => GlyphGeneric(p)
            };
            fill = Color.Lerp(fill, SafetyLensTheme.SignInk, glyph);

            fill.a = 1f - outside;
            return fill;
        }

        /// <summary>W017 "Superfície quente": barra horizontal e três ondas de calor subindo.</summary>
        static float GlyphHot(Vector2 p)
        {
            var d = SdRoundBox(p - new Vector2(0.5f, 0.30f), new Vector2(0.21f, 0.028f), 0.02f);
            for (var i = -1; i <= 1; i++)
            {
                var cx = 0.5f + i * 0.12f;
                d = Mathf.Min(d, SdWave(p, cx, 0.40f, 0.62f, 0.028f, 2) - 0.018f);
            }
            return Coverage(d);
        }

        /// <summary>W024 "Esmagamento das mãos": duas placas, mão entre elas e setas convergindo.</summary>
        static float GlyphPinch(Vector2 p)
        {
            // O triângulo estreita para cima: a placa superior é mais curta que a inferior.
            var d = SdRoundBox(p - new Vector2(0.5f, 0.575f), new Vector2(0.10f, 0.024f), 0.012f);
            d = Mathf.Min(d, SdRoundBox(p - new Vector2(0.5f, 0.235f), new Vector2(0.15f, 0.024f), 0.012f));

            // Setas (cabeças triangulares) apontando para o centro.
            d = Mathf.Min(d, SdTriangle(p, new Vector2(0.5f, 0.485f), new Vector2(0.44f, 0.535f), new Vector2(0.56f, 0.535f)));
            d = Mathf.Min(d, SdTriangle(p, new Vector2(0.5f, 0.325f), new Vector2(0.56f, 0.275f), new Vector2(0.44f, 0.275f)));

            // Mão: palma + quatro dedos, levemente à direita para ler como "mão prensada".
            d = Mathf.Min(d, SdRoundBox(p - new Vector2(0.56f, 0.405f), new Vector2(0.075f, 0.048f), 0.026f));
            for (var i = 0; i < 4; i++)
            {
                var fy = 0.36f + i * 0.031f;
                var len = i == 0 || i == 3 ? 0.10f : 0.13f;
                d = Mathf.Min(d, SdRoundBox(p - new Vector2(0.49f - len * 0.5f, fy), new Vector2(len * 0.5f, 0.012f), 0.012f));
            }
            return Coverage(d);
        }

        /// <summary>W001 "Atenção": ponto de exclamação.</summary>
        static float GlyphGeneric(Vector2 p)
        {
            var d = SdRoundBox(p - new Vector2(0.5f, 0.50f), new Vector2(0.045f, 0.17f), 0.03f);
            d = Mathf.Min(d, Vector2.Distance(p, new Vector2(0.5f, 0.245f)) - 0.05f);
            return Coverage(d);
        }

        // ----- Anel de destaque (branco; a cor vem do material) -----

        static Color SampleRing(float x, float y)
        {
            var r = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));
            var ring = 1f - Mathf.Clamp01(Mathf.Abs(r - 0.42f) / 0.05f);
            var glow = Mathf.Clamp01((0.48f - r) / 0.48f) * 0.18f;
            var a = Mathf.Max(ring * ring, glow);
            return new Color(1f, 1f, 1f, a);
        }

        // ----- Distâncias assinadas (Inigo Quilez) -----

        static float Coverage(float sd) => 1f - Mathf.SmoothStep(0f, 1f, (sd + Aa) / (2f * Aa));

        static float SdRoundBox(Vector2 p, Vector2 half, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x) - half.x + r, Mathf.Abs(p.y) - half.y + r);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        static float SdSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var pa = p - a;
            var ba = b - a;
            var h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        /// <summary>Distância a uma senoide vertical amostrada em segmentos (onda de calor).</summary>
        static float SdWave(Vector2 p, float cx, float y0, float y1, float amplitude, int periods)
        {
            const int steps = 24;
            var best = float.MaxValue;
            var prev = WavePoint(0);
            for (var i = 1; i <= steps; i++)
            {
                var next = WavePoint(i);
                best = Mathf.Min(best, SdSegment(p, prev, next));
                prev = next;
            }
            return best;

            Vector2 WavePoint(int i)
            {
                var t = i / (float)steps;
                return new Vector2(cx + amplitude * Mathf.Sin(t * periods * Mathf.PI), Mathf.Lerp(y0, y1, t));
            }
        }

        static float SdTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            var e0 = p1 - p0; var e1 = p2 - p1; var e2 = p0 - p2;
            var v0 = p - p0; var v1 = p - p1; var v2 = p - p2;
            var pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            var pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            var pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            var s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            var dx = Mathf.Min(Mathf.Min(Vector2.Dot(pq0, pq0), Vector2.Dot(pq1, pq1)), Vector2.Dot(pq2, pq2));
            var dy = Mathf.Min(Mathf.Min(s * (v0.x * e0.y - v0.y * e0.x), s * (v1.x * e1.y - v1.y * e1.x)),
                s * (v2.x * e2.y - v2.y * e2.x));
            return -Mathf.Sqrt(dx) * Mathf.Sign(dy);
        }
    }
}
