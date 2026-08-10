using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 별과 사용 가능 도구가 JSON 을 오가는가.
    /// 맵 에디터가 저장한 파일을 코어가 그대로
    /// 먹어야 변환 계층이 필요 없다.
    /// </summary>
    public class StarToolContractTests
    {
        [Test]
        public void 별과_도구가_저장하고_읽어도_같다()
        {
            var stage = new StageData { StageId = "M_TEST", Seed = 7 };
            stage.Level.Stars.Add(new Vector2(1.5f, -2.5f));
            stage.Level.Stars.Add(new Vector2(0f, 3f));
            stage.Level.StarRadius = 0.4f;
            stage.Level.AllowedTools.Add(ToolType.FixedLine);

            var restored = StageData.FromJson(stage.ToJson());

            Assert.AreEqual("M_TEST", restored.StageId);
            Assert.AreEqual(7, restored.Seed);
            Assert.AreEqual(2, restored.Level.Stars.Count);
            Assert.AreEqual(new Vector2(1.5f, -2.5f), restored.Level.Stars[0]);
            Assert.AreEqual(0.4f, restored.Level.StarRadius, 1e-4f);
            Assert.AreEqual(1, restored.Level.AllowedTools.Count);
            Assert.AreEqual(ToolType.FixedLine, restored.Level.AllowedTools[0]);
        }

        [Test]
        public void 두_번_저장해도_파일이_같다()
        {
            var stage = new StageData { StageId = "M_TEST", Seed = 3 };
            stage.Level.Stars.Add(new Vector2(2f, 2f));

            string first = stage.ToJson();
            string second = StageData.FromJson(first).ToJson();

            Assert.AreEqual(first, second);
        }

        [Test]
        public void 별이_없는_예전_파일도_열린다()
        {
            // 별·도구가 들어오기 전에 저장된 레벨이
            // 그대로 열려야 기존 레벨을 안 버린다.
            const string oldJson = @"{""InkLimit"":12.0,""KillY"":-8.0}";

            var level = JsonUtility.FromJson<LevelData>(oldJson);

            Assert.AreEqual(12f, level.InkLimit, 1e-4f);
            Assert.IsNotNull(level.Stars, "없는 항목은 기본값이어야 한다.");
            Assert.AreEqual(0, level.Stars.Count);
            Assert.AreEqual(0, level.AllowedTools.Count,
                "도구 목록이 비면 제한 없음으로 읽힌다.");
        }
    }
}
