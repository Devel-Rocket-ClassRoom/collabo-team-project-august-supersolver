using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 그리는 쪽은 스케일에 지름을 그대로 넣는다. 스프라이트
    /// 한 장이 1wu 여야 보이는 크기와 콜라이더가 같아진다 —
    /// 아트가 새로 들어올 때 조용히 어긋나는 것을 잡는다.
    /// </summary>
    public class SimStyleSpriteGuardTests
    {
        /// 1wu 에서 허용할 오차. 이보다 어긋나면
        /// 공 지름에서 눈에 띈다.
        const float Tolerance = 0.001f;

        [Test]
        public void SimStyle_스프라이트가_전부_1wu다()
        {
            var offenses = new List<string>();
            int scanned = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:SimStyle"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var style = AssetDatabase.LoadAssetAtPath<SimStyle>(path);
                if (style == null) continue;

                foreach (Sprite sprite in SpritesOf(style))
                {
                    scanned++;
                    Vector2 size = sprite.bounds.size;

                    if (Mathf.Abs(size.x - 1f) <= Tolerance
                        && Mathf.Abs(size.y - 1f) <= Tolerance) continue;

                    offenses.Add($"{style.name}/{sprite.name}: {size} — "
                        + "Pixels Per Unit 을 타일 한 변과 같게 맞춘다.");
                }
            }

            // 스캔이 비면 조용히 초록불이 된다.
            // 일했다는 증거를 남긴다.
            Assert.Greater(scanned, 0, "SimStyle 스프라이트를 한 장도 읽지 못했다 — 검사가 헛돌았다.");

            Assert.IsEmpty(offenses, "\n" + string.Join("\n", offenses));
        }

        /// 안 꽂힌 자리는 건너뛴다. 아직 그리지 않은
        /// 것까지 막으면 아트를 기다리느라 작업이 선다.
        static IEnumerable<Sprite> SpritesOf(SimStyle style)
        {
            SimStyle.Shapes shapes = style.Sprites;
            Sprite[] all =
            {
                shapes.Dot, shapes.Ball, shapes.Goal, shapes.Star,
                shapes.Bomb, shapes.FragBomb, shapes.Spike, shapes.Wind,
            };

            foreach (Sprite sprite in all)
                if (sprite != null) yield return sprite;
        }
    }
}
