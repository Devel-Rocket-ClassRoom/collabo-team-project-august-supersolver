using System.IO;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 지금 표현 가능한 모든 피처를 담은
    /// 레벨·솔루션 한 쌍. 파일을 나눈 것은
    /// 레벨과 그림이 다른 것이기 때문이다.
    /// </summary>
    public static class FeatureLevelFile
    {
        public const string LevelRelativePath = "_Project/Levels/L002_Feature.json";
        public const string SolutionRelativePath = "_Project/Levels/L002_Feature.solution.json";

        public static string LevelPath => Path.Combine(Application.dataPath, LevelRelativePath);
        public static string SolutionPath => Path.Combine(Application.dataPath, SolutionRelativePath);

        public static bool Exists => File.Exists(LevelPath) && File.Exists(SolutionPath);

        /// <summary>
        /// 레벨 모양 JSON 을 직접 읽는다.
        /// 최상위 자리를 StageData 에 넘기면서
        /// 진입점을 잃었다.
        /// </summary>
        public static LevelData LoadLevel() => JsonUtility.FromJson<LevelData>(File.ReadAllText(LevelPath));

        /// <summary>
        /// Solution 도 같은 이유로 직접 읽는다.
        /// 리플레이 스키마와 함께 정하는 편이 낫다.
        /// </summary>
        public static Solution LoadSolution() => JsonUtility.FromJson<Solution>(File.ReadAllText(SolutionPath));

        // 기본값으로 채워졌는지 잡는 기준.
        public const float ExpectedInkLimit = 24f;
        public const float ExpectedKillY = -8f;
        public const int ExpectedTerrainCount = 4;
        public const int ExpectedDeviceCount = 1;

        public const int ExpectedStrokeCount = 5;
        public const int ExpectedFixedLineCount = 2;
        public const int ExpectedFreeBodyCount = 3;

        /// 스트로크 간 연결 1 + 월드 고정 1.
        public const int ExpectedPivotCount = 2;
    }
}
