using PPS.Core;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 편집 화면과 테스트 화면이 같은 모양으로
    /// 그리게 하는 표시용 도구. 임시 표시라
    /// 스프라이트를 코드로 만든다.
    /// </summary>
    public static class MapHandleGfx
    {
        /// 지형을 그리는 굵기. 표시용일 뿐 물리는 선이다.
        public const float LineWidth = 0.12f;

        /// <summary>
        /// 폭탄 스프라이트에서 몸통이 차지하는 비율.
        /// 심지 자리를 남기느라 꽉 채우지 못한다 —
        /// 그린 몸통을 콜라이더 크기에 맞출 때 나눈다.
        /// </summary>
        public const float BombBodySpan = 0.78f;

        /// 파편 자리를 남겨 몸통이 더 작다.
        public const float FragBombBodySpan = 0.53f;

        /// 가시 끝까지가 콜라이더 반지름이다.
        public const float SpikeBodySpan = 0.97f;

        static Sprite _circle;
        static Sprite _square;
        static Sprite _bomb;
        static Sprite _fragBomb;
        static Sprite _spike;
        static Sprite _wind;

        public static Sprite Circle => _circle != null ? _circle : _circle = MakeCircle();

        public static Sprite Square => _square != null ? _square : _square = MakeSquare();

        public static Sprite Bomb => _bomb != null ? _bomb : _bomb = MakeBomb();

        public static Sprite FragBomb =>
            _fragBomb != null ? _fragBomb : _fragBomb = MakeFragBomb();

        public static Sprite Spike => _spike != null ? _spike : _spike = MakeSpike();

        public static Sprite Wind => _wind != null ? _wind : _wind = MakeWind();

        public static SpriteRenderer Create(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            return renderer;
        }

        /// <param name="degrees">방향이 있는 장치를 돌릴 때 넘긴다.</param>
        public static void PlaceDot(SpriteRenderer handle, Vector2 world, float radius, Color color,
            float degrees = 0f)
        {
            handle.transform.position = new Vector3(world.x, world.y, 0f);
            handle.transform.rotation = Quaternion.Euler(0f, 0f, degrees);
            handle.transform.localScale = Vector3.one * (radius * 2f);
            handle.color = color;
        }

        /// <param name="width">지형보다 얇게 그릴 때 넘긴다.</param>
        public static void PlaceLine(SpriteRenderer handle, in StaticSegment segment, Color color,
            float width = LineWidth)
        {
            Vector2 center = (segment.A + segment.B) * 0.5f;
            Vector2 ab = segment.B - segment.A;

            handle.transform.position = new Vector3(center.x, center.y, 0f);
            handle.transform.rotation =
                Quaternion.Euler(0f, 0f, Mathf.Atan2(ab.y, ab.x) * Mathf.Rad2Deg);
            handle.transform.localScale = new Vector3(ab.magnitude, width, 1f);
            handle.color = color;
        }

        static Sprite MakeCircle()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, dist <= center ? Color.white : Color.clear);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        /// <summary>
        /// 둥근 몸통에 심지를 얹은 폭탄.
        /// 한 색으로 칠해지므로 실루엣만으로 읽혀야 한다.
        /// </summary>
        static Sprite MakeBomb()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            var body = new Vector2(28f, 26f);
            var fuseFrom = new Vector2(36f, 44f);
            var fuseTo = new Vector2(48f, 57f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var at = new Vector2(x, y);

                bool on = Vector2.Distance(at, body) <= 25f
                    || DistanceToSegment(at, fuseFrom, fuseTo) <= 2.5f
                    || Vector2.Distance(at, fuseTo) <= 5f;

                texture.SetPixel(x, y, on ? Color.white : Color.clear);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        /// <summary>
        /// 몸통에서 파편이 떨어져 나가는 폭탄.
        /// 조각 수는 실제 파편 수와 같고, 몸통과 띄워 두어
        /// 붙은 가시가 아니라 날아가는 조각으로 읽힌다.
        /// </summary>
        static Sprite MakeFragBomb()
        {
            const int size = 64;
            const int fragments = 5;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(31.5f, 31.5f);

            // 위에서 시작해 균등하게 벌린다.
            var at = new Vector2[fragments];
            for (int i = 0; i < fragments; i++)
            {
                float angle = Mathf.PI * 0.5f + 2f * Mathf.PI * i / fragments;
                at[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 26f;
            }

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var point = new Vector2(x, y);
                bool on = Vector2.Distance(point, center) <= 17f;

                for (int i = 0; i < fragments && !on; i++)
                    on = Vector2.Distance(point, at[i]) <= 4.5f;

                texture.SetPixel(x, y, on ? Color.white : Color.clear);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        /// <summary>
        /// 가시가 돋은 공. 콜라이더가 원이라 가시 끝이
        /// 그 원에 닿아야 보이는 것과 닿는 것이 맞는다.
        /// </summary>
        static Sprite MakeSpike()
        {
            const int size = 64;
            const int spikes = 10;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(31.5f, 31.5f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 delta = new Vector2(x, y) - center;
                float dist = delta.magnitude;
                bool on = dist <= 18f;

                if (!on && dist <= 31f)
                {
                    // 끝으로 갈수록 좁아져 뾰족해 보인다.
                    float taper = 1f - (dist - 18f) / 13f;
                    float angle = Mathf.Atan2(delta.y, delta.x);
                    float step = 2f * Mathf.PI / spikes;
                    float offset = Mathf.Abs(Mathf.Repeat(angle + step * 0.5f, step) - step * 0.5f);

                    on = offset <= 0.26f * taper;
                }

                texture.SetPixel(x, y, on ? Color.white : Color.clear);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        /// <summary>
        /// 오른쪽을 향한 갈매기꼴 셋.
        /// 0 도가 오른쪽이라 그대로 돌려 쓰면 방향이 맞는다.
        /// </summary>
        static Sprite MakeWind()
        {
            const int size = 64;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            // 위·가운데·아래 세 줄. 가운데가 가장 길다.
            var tips = new[]
            {
                new Vector2(44f, 48f), new Vector2(52f, 31.5f), new Vector2(44f, 15f),
            };
            var backs = new[] { 22f, 12f, 22f };

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var point = new Vector2(x, y);
                bool on = false;

                for (int i = 0; i < tips.Length && !on; i++)
                {
                    var upper = new Vector2(backs[i], tips[i].y + (tips[i].x - backs[i]) * 0.35f);
                    var lower = new Vector2(backs[i], tips[i].y - (tips[i].x - backs[i]) * 0.35f);

                    on = DistanceToSegment(point, tips[i], upper) <= 2.6f
                        || DistanceToSegment(point, tips[i], lower) <= 2.6f;
                }

                texture.SetPixel(x, y, on ? Color.white : Color.clear);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(point, a + ab * t);
        }

        static Sprite MakeSquare()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
        }
    }
}
