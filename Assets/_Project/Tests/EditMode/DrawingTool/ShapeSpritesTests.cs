using NUnit.Framework;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 스프라이트가 1wu 여야 스케일이 곧 월드 크기다.
    /// 어긋나면 레벨 지오메트리가 통째로 다른 크기로
    /// 앉는데, 그럴듯해 보여서 눈으로는 못 잡는다.
    /// </summary>
    public class ShapeSpritesTests
    {
        [Test]
        public void 셋_다_1wu_크기이고_중심이_원점이다()
        {
            AssertUnitSized(ShapeSprites.Disc, "Disc");
            AssertUnitSized(ShapeSprites.Ring, "Ring");
            AssertUnitSized(ShapeSprites.Quad, "Quad");
        }

        [Test]
        public void 다시_읽어도_새로_만들지_않는다()
        {
            Assert.AreSame(ShapeSprites.Disc, ShapeSprites.Disc);
            Assert.AreSame(ShapeSprites.Ring, ShapeSprites.Ring);
            Assert.AreSame(ShapeSprites.Quad, ShapeSprites.Quad);
        }

        /// <summary>
        /// 채움과 링의 차이가 색과 별개의 구분 채널이라
        /// 형태가 무너지면 표시가 색 하나에만 걸린다.
        /// </summary>
        [Test]
        public void 링은_가운데가_비고_원은_차_있다()
        {
            Texture2D disc = ShapeSprites.Disc.texture;
            Texture2D ring = ShapeSprites.Ring.texture;

            int middle = disc.width / 2;

            Assert.AreEqual(1f, disc.GetPixel(middle, middle).a, 1e-3f,
                "채운 원의 가운데가 비었다");
            Assert.AreEqual(0f, ring.GetPixel(middle, middle).a, 1e-3f,
                "링의 가운데가 뚫려 있지 않다");

            // 바깥 반경과 안쪽 반경의 중간이라 띠 위다.
            int band = middle + (int)(middle * 0.84f);
            Assert.AreEqual(1f, ring.GetPixel(band, middle).a, 1e-3f,
                "링의 띠가 비어 있다");
        }

        static void AssertUnitSized(Sprite sprite, string name)
        {
            Assert.AreEqual(1f, sprite.bounds.size.x, 1e-5f, $"{name} 의 가로가 1wu 가 아니다");
            Assert.AreEqual(1f, sprite.bounds.size.y, 1e-5f, $"{name} 의 세로가 1wu 가 아니다");

            Assert.AreEqual(0f, sprite.bounds.center.x, 1e-5f, $"{name} 의 중심이 원점이 아니다");
            Assert.AreEqual(0f, sprite.bounds.center.y, 1e-5f, $"{name} 의 중심이 원점이 아니다");
        }
    }
}
