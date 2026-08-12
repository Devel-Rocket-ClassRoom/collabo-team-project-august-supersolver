using PPS.Core;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 스프라이트 하나를 세계 좌표에 놓는다.
    /// 무엇을 그릴지는 모르고 어떻게 놓을지만 안다 —
    /// 모양은 MapEditStyle 이 들고 있다.
    /// </summary>
    public static class MapHandleGfx
    {
        /// 지형을 그리는 굵기. 표시용일 뿐 물리는 선이다.
        public const float LineWidth = 0.12f;

        static Sprite _square;

        /// <summary>
        /// 선을 늘려 그리는 흰 사각형.
        /// 이것만 코드로 만든다 — 늘리고 돌려 쓰는 조각이라
        /// 여백이나 둥근 모서리가 있으면 선이 끊겨 보인다.
        /// </summary>
        public static Sprite Square => _square != null ? _square : _square = MakeSquare();

        static Sprite MakeSquare()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
        }

        /// <summary>
        /// 꺼진 채로 만든다. 자리를 받기 전까지는 원점에
        /// 기본 크기로 떠 있어서, 켜둔 채 돌려주면
        /// 아직 그릴 때가 아닌 것이 화면에 남는다.
        /// 켜는 것은 그리는 쪽이 정한다.
        /// </summary>
        public static SpriteRenderer Create(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.SetActive(false);

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
    }
}
