using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using PPS.Core.Tests;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 완료조건 2-1 의 마지막 조각. 화면과 잉크는 7번이
    /// 닫았고, 남은 「콜라이더 0개」는 월드를 지어야 센다.
    /// </summary>
    public class UndoWorldTests
    {
        [Test]
        public void 획_3개를_되돌리면_월드에_획이_남지_않는다()
        {
            var history = new SolutionHistory();
            for (int i = 0; i < 3; i++) history.AddStroke(Bar(i));

            using (var drawn = WorldBuilder.Build(TestLevels.PivotSwing(), history.Current, 0))
            {
                // 대조군. 애초에 안 서면 0 개는 아무 뜻도 없다.
                Assert.AreEqual(3, drawn.StrokeBodies.Count, "그린 획이 월드에 서지 않았다");
            }

            for (int i = 0; i < 3; i++)
                Assert.IsTrue(history.Undo(), $"{i + 1} 번째 되돌리기가 먹지 않았다");

            using (var empty = WorldBuilder.Build(TestLevels.PivotSwing(), history.Current, 0))
            {
                Assert.AreEqual(0, empty.StrokeBodies.Count, "되돌린 획이 월드에 남았다");
            }
        }

        /// 최소 획 길이를 넉넉히 넘는 막대. 서로 겹치지 않게 띄운다.
        static Stroke Bar(int index) =>
            new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-1f, 3f + index),
                new Vector2(1f, 3f + index),
            });
    }
}
