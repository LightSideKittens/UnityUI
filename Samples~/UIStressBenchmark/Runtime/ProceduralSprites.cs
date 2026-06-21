using UnityEngine;

namespace LightSide.UIBenchmark
{
    /// <summary>
    /// Runtime-generated sprite set used by the benchmark so it needs no imported assets and
    /// behaves identically against any UGUI build. Textures are tiny and shared, so batching
    /// stays realistic. Create once per run and dispose with <see cref="Destroy"/>.
    /// </summary>
    public sealed class ProceduralSprites
    {
        /// <summary>Flat white quad. Tinted per element; used for Simple and Filled images.</summary>
        public Sprite Solid { get; private set; }

        /// <summary>Bordered frame with a non-trivial 9-slice border, for Sliced images.</summary>
        public Sprite Sliced { get; private set; }

        /// <summary>Small checker, meant to be repeated by Tiled images.</summary>
        public Sprite Tiled { get; private set; }

        /// <summary>Soft-edged disc, gives Filled/radial images a recognizable shape.</summary>
        public Sprite Circle { get; private set; }

        /// <summary>All four sprites in a stable order for round-robin assignment.</summary>
        public Sprite[] All { get; private set; }

        Texture2D[] _textures;

        /// <summary>Builds the full sprite set. Allocates textures; call only at scene build time.</summary>
        public static ProceduralSprites Create()
        {
            var s = new ProceduralSprites();

            var solidTex = MakeSolid(8, new Color32(255, 255, 255, 255));
            var slicedTex = MakeBorderedFrame(32, 7, new Color32(80, 90, 110, 255), new Color32(210, 220, 235, 255));
            var tiledTex = MakeChecker(16, 4, new Color32(150, 160, 180, 255), new Color32(90, 100, 120, 255));
            var circleTex = MakeDisc(64, new Color32(255, 255, 255, 255));

            s.Solid = MakeSprite(solidTex, Vector4.zero, "bench_solid");
            float b = 7f;
            s.Sliced = MakeSprite(slicedTex, new Vector4(b, b, b, b), "bench_sliced");
            s.Tiled = MakeSprite(tiledTex, Vector4.zero, "bench_tiled");
            s.Circle = MakeSprite(circleTex, Vector4.zero, "bench_circle");

            s.All = new[] { s.Solid, s.Sliced, s.Tiled, s.Circle };
            s._textures = new[] { solidTex, slicedTex, tiledTex, circleTex };
            return s;
        }

        /// <summary>Destroys the generated sprites and their textures.</summary>
        public void Destroy()
        {
            if (All != null)
            {
                foreach (var sp in All)
                    if (sp != null) Object.Destroy(sp);
            }
            if (_textures != null)
            {
                foreach (var t in _textures)
                    if (t != null) Object.Destroy(t);
            }
            All = null;
            _textures = null;
        }

        static Sprite MakeSprite(Texture2D tex, Vector4 border, string name)
        {
            var rect = new Rect(0, 0, tex.width, tex.height);
            var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            return sprite;
        }

        static Texture2D NewTex(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        static Texture2D MakeSolid(int size, Color32 c)
        {
            var t = NewTex(size);
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            t.SetPixels32(px);
            t.Apply(false, false);
            return t;
        }

        static Texture2D MakeBorderedFrame(int size, int border, Color32 edge, Color32 fill)
        {
            var t = NewTex(size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isEdge = x < border || y < border || x >= size - border || y >= size - border;
                    px[y * size + x] = isEdge ? edge : fill;
                }
            }
            t.SetPixels32(px);
            t.Apply(false, false);
            return t;
        }

        static Texture2D MakeChecker(int size, int cell, Color32 a, Color32 b)
        {
            var t = NewTex(size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool even = ((x / cell) + (y / cell)) % 2 == 0;
                    px[y * size + x] = even ? a : b;
                }
            }
            t.SetPixels32(px);
            t.Apply(false, false);
            return t;
        }

        static Texture2D MakeDisc(int size, Color32 c)
        {
            var t = NewTex(size);
            var px = new Color32[size * size];
            float r = size * 0.5f;
            float rInner = r - 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r;
                    float dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(rInner - d + 1f);
                    px[y * size + x] = new Color32(c.r, c.g, c.b, (byte)(a * 255f));
                }
            }
            t.SetPixels32(px);
            t.Apply(false, false);
            return t;
        }
    }
}
