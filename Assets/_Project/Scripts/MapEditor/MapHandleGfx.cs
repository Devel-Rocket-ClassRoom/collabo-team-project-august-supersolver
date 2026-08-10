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

        static Sprite _circle;
        static Sprite _square;

        public static Sprite Circle => _circle != null ? _circle : _circle = MakeCircle();

        public static Sprite Square => _square != null ? _square : _square = MakeSquare();

        public static SpriteRenderer Create(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            return renderer;
        }

        public static void PlaceDot(SpriteRenderer handle, Vector2 world, float radius, Color color)
        {
            handle.transform.position = new Vector3(world.x, world.y, 0f);
            handle.transform.rotation = Quaternion.identity;
            handle.transform.localScale = Vector3.one * (radius * 2f);
            handle.color = color;
        }

        public static void PlaceLine(SpriteRenderer handle, in StaticSegment segment, Color color)
        {
            Vector2 center = (segment.A + segment.B) * 0.5f;
            Vector2 ab = segment.B - segment.A;

            handle.transform.position = new Vector3(center.x, center.y, 0f);
            handle.transform.rotation =
                Quaternion.Euler(0f, 0f, Mathf.Atan2(ab.y, ab.x) * Mathf.Rad2Deg);
            handle.transform.localScale = new Vector3(ab.magnitude, LineWidth, 1f);
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

        static Sprite MakeSquare()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
        }
    }
}
