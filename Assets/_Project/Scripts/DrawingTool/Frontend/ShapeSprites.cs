using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 표시용 스프라이트를 코드로 만든다. 아트를
    /// 기다리느라 렌더링을 못 여는 게 더 비싸다.
    /// 전부 1wu 라 스케일이 곧 월드 크기다.
    /// </summary>
    public static class ShapeSprites
    {
        /// 원 텍스처 한 변. 화면에서 가장 큰 원이
        /// 목표(지름 1wu)라 이 정도면 계단이 안 보인다.
        const int CircleSize = 128;

        /// 링 안쪽 반경 ÷ 바깥 반경. 더 얇게 하면
        /// 핀 마커 크기에서 선이 사라진다.
        const float RingInnerRatio = 0.68f;

        static Sprite _disc;
        static Sprite _ring;
        static Sprite _quad;

        /// 지름 1wu 의 채운 원.
        public static Sprite Disc => _disc != null ? _disc : _disc = MakeCircle(0f);

        /// 지름 1wu 의 속 빈 원. 채운 원과 형태로 갈려
        /// 색을 못 가리는 눈에도 구분이 남는다.
        public static Sprite Ring =>
            _ring != null ? _ring : _ring = MakeCircle(RingInnerRatio);

        /// 한 변 1wu 의 사각. 선분을 눕혀 쓴다.
        public static Sprite Quad => _quad != null ? _quad : _quad = MakeQuad();

        /// <param name="innerRatio">0 이면 꽉 찬 원.</param>
        static Sprite MakeCircle(float innerRatio)
        {
            Texture2D texture = NewTexture(CircleSize);

            float outer = (CircleSize - 1) * 0.5f;
            float inner = outer * innerRatio;
            var center = new Vector2(outer, outer);

            for (int y = 0; y < CircleSize; y++)
            for (int x = 0; x < CircleSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                // 경계를 픽셀 한 겹만큼 흐린다. 잘라내면
                // 고해상도 폰에서 테두리가 톱니로 보인다.
                float alpha = Mathf.Clamp01(outer - distance);
                if (inner > 0f) alpha *= Mathf.Clamp01(distance - inner);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }

            texture.Apply();
            return ToSprite(texture, CircleSize);
        }

        static Sprite MakeQuad()
        {
            Texture2D texture = NewTexture(1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            return ToSprite(texture, 1);
        }

        static Texture2D NewTexture(int size) =>
            new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                // 반복이면 가장자리가 반대편 픽셀을 문다.
                wrapMode = TextureWrapMode.Clamp,
            };

        /// pixelsPerUnit 에 한 변을 주면 크기가 1wu 다.
        static Sprite ToSprite(Texture2D texture, int size) =>
            Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, size);
    }
}
