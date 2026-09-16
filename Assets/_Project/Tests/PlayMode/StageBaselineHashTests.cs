using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 장치가 든 판의 시뮬 결과를 지문으로 굳혀 둔다.
    /// 장치 데이터 구조를 바꾸는 동안 결정론이 깨지면
    /// 어느 판에서 깨졌는지 바로 드러나야 한다.
    ///
    /// 기준선은 파일로 남긴다 — 값을 사람이 손으로 구할 수 없다.
    /// 파일에 없는 항목은 한 번 기록하고 실패한다. 실수로
    /// 조용히 다시 찍히면 회귀 그물이 아니게 된다.
    /// </summary>
    public class StageBaselineHashTests
    {
        /// 장치가 하나라도 있는 판. Stage17 은 장치가 없어 뺐다.
        static readonly string[] StageIds =
        {
            "Stage11", "Stage12", "Stage13", "Stage14",
            "Stage15", "Stage16", "Stage18", "Stage19", "Stage20",
        };

        /// L002_Feature 는 솔루션과 함께 돈다. 시드는 판에 없어 여기서 고정한다.
        const string FeatureId = "L002_Feature";
        const int FeatureSeed = 11;

        const int MaxSteps = SimWorld.DefaultMaxSteps;

        const string BaselineRelativePath = "_Project/Tests/Baselines/StageBaseline.txt";

        static string BaselinePath => Path.Combine(Application.dataPath, BaselineRelativePath);

        /// 파일에서 읽은 기준선. 키는 판 이름.
        static Dictionary<string, string> _baseline;

        /// 이번 실행에서 새로 기록한 것. 있으면 TearDown 이 파일을 다시 쓴다.
        static Dictionary<string, string> _recorded;

        [OneTimeSetUp]
        public void LoadBaseline()
        {
            _baseline = new Dictionary<string, string>();
            _recorded = new Dictionary<string, string>();

            if (!File.Exists(BaselinePath)) return;

            foreach (string line in File.ReadAllLines(BaselinePath))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed[0] == '#') continue;

                int space = trimmed.IndexOf(' ');
                if (space <= 0) continue;

                _baseline[trimmed.Substring(0, space)] = trimmed.Substring(space + 1).Trim();
            }
        }

        [OneTimeTearDown]
        public void SaveBaseline()
        {
            if (_recorded.Count == 0) return;

            var merged = new SortedDictionary<string, string>(_baseline, System.StringComparer.Ordinal);
            foreach (var pair in _recorded) merged[pair.Key] = pair.Value;

            var text = new System.Text.StringBuilder();
            text.AppendLine("# 장치가 든 판의 시뮬 지문. 손으로 고치지 않는다.");
            text.AppendLine("# 값이 바뀌었다면 물리 결과가 바뀐 것이다.");
            foreach (var pair in merged) text.AppendLine($"{pair.Key} {pair.Value}");

            Directory.CreateDirectory(Path.GetDirectoryName(BaselinePath));
            File.WriteAllText(BaselinePath, text.ToString());
        }

        [TestCaseSource(nameof(StageIds))]
        public void 판의_시뮬_지문이_기준선과_같다(string stageId)
        {
            string path = Path.Combine(Application.dataPath, $"_Project/Levels/{stageId}.json");
            Assert.IsTrue(File.Exists(path), $"{stageId}.json 이 없다.");

            var stage = StageData.FromJson(File.ReadAllText(path));
            Assert.IsNotNull(stage, $"{stageId}.json 을 스테이지로 읽지 못했다.");

            var trace = new List<ulong>();
            var result = SimRunner.RunTraced(stage.Level, null, stage.Seed, trace, MaxSteps);

            Compare(stageId, result, trace);
        }

        [Test]
        public void 피처_레벨의_시뮬_지문이_기준선과_같다()
        {
            var trace = new List<ulong>();
            var result = SimRunner.RunTraced(
                FeatureLevelFile.LoadLevel(), FeatureLevelFile.LoadSolution(),
                FeatureSeed, trace, MaxSteps);

            Compare(FeatureId, result, trace);
        }

        static void Compare(string id, in SimResult result, List<ulong> trace)
        {
            Assert.Greater(trace.Count, 0, $"{id} 이 한 스텝도 진행되지 않았다.");

            string actual = string.Format(
                CultureInfo.InvariantCulture,
                "steps={0} outcome={1} digest=0x{2:X16}",
                trace.Count, result.Outcome, Digest(trace));

            if (!_baseline.TryGetValue(id, out string expected))
            {
                _recorded[id] = actual;
                Assert.Fail(
                    $"{id} 의 기준선이 없어 새로 기록했다 ({actual}).\n" +
                    $"{BaselineRelativePath} 를 확인하고 커밋한 뒤 다시 돌려라.");
                return;
            }

            Assert.AreEqual(expected, actual,
                $"{id} 의 시뮬 결과가 기준선과 다르다 — 결정론이 깨졌다.");
        }

        /// <summary>스텝별 해시를 한 값으로 접는다. FNV-1a.</summary>
        static ulong Digest(List<ulong> trace)
        {
            unchecked
            {
                ulong h = 14695981039346656037UL;

                for (int i = 0; i < trace.Count; i++)
                {
                    h ^= trace[i];
                    h *= 1099511628211UL;
                }

                return h;
            }
        }
    }
}
