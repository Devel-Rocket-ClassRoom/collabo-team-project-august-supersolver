using PPS.Core;
using UnityEngine;

namespace PPS.Game
{
    /// <summary>
    /// 스프라이트 하나를 세계 좌표에 놓는다.
    /// 무엇을 그릴지는 모르고 어떻게 놓을지만 안다 —
    /// 모양은 SimStyle 이 들고 있다.
    /// </summary>
    public static class MapHandleGfx
    {
        /// 지형을 그리는 굵기. 표시용일 뿐 물리는 선이다.
        public const float LineWidth = 0.12f;

        public static void PlaceDot(SpriteRenderer handle, Sprite art, Vector2 world,
            float radius, Color color, float degrees = 0f) =>
            Place(handle, art, world, Vector2.one * (radius * 2f), color, degrees, true);

        public static void PlaceLine(SpriteRenderer handle, Sprite art, in StaticSegment segment,
            Color color, float width = LineWidth)
        {
            Vector2 ab = segment.B - segment.A;
            Place(handle, art, (segment.A + segment.B) * 0.5f, new Vector2(ab.magnitude, width),
                color, Mathf.Atan2(ab.y, ab.x) * Mathf.Rad2Deg, false);
        }

        static void Place(SpriteRenderer handle, Sprite art, Vector2 world,
            Vector2 size, Color color, float degrees, bool preserveAspect)
        {
            handle.sprite = art;
            handle.enabled = art != null;
            if (!handle.enabled) return;
            Vector2 contentSize = art.bounds.size;
            Vector2 scale = new Vector2(size.x / contentSize.x, size.y / contentSize.y);
            if (preserveAspect) scale = Vector2.one * Mathf.Min(scale.x, scale.y);
            Vector2 center = art.bounds.center;
            Quaternion rotation = Quaternion.Euler(0, 0, degrees);
            handle.transform.localScale = new Vector3(scale.x, scale.y, 1);
            handle.transform.rotation = rotation;
            handle.transform.position = (Vector3)world - rotation * Vector2.Scale(center, scale);
            handle.color = color;
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
            float size = handle.sprite == null ? 1f : Mathf.Max(handle.sprite.bounds.size.x, handle.sprite.bounds.size.y);
            handle.transform.localScale = Vector3.one * (radius * 2f / size);
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
            Vector2 size = handle.sprite == null ? Vector2.one : (Vector2)handle.sprite.bounds.size;
            handle.transform.localScale = new Vector3(ab.magnitude / size.x, width / size.y, 1f);
            handle.color = color;
        }
    }
}
