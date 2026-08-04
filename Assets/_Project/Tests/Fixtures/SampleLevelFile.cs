using System.IO;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 커밋된 샘플 레벨 JSON.
    /// "레벨 추가는 데이터만으로"의 증거물이다.
    /// 값은 기본값과 일부러 다르게 잡았다.
    /// </summary>
    public static class SampleLevelFile
    {
        /// 에디터·테스트 전용 접근이다.
        public const string RelativePath = "_Project/Levels/L001_Ramp.json";

        public static string FullPath => Path.Combine(Application.dataPath, RelativePath);

        public static bool Exists => File.Exists(FullPath);

        public static string ReadText() => File.ReadAllText(FullPath);

        /// <summary>
        /// 스테이지 이전에 만들어진 레벨 모양 JSON.
        /// 최상위가 아닌 타입은 진입점이 없어
        /// JsonUtility 를 직접 쓴다.
        /// </summary>
        public static LevelData Load() => JsonUtility.FromJson<LevelData>(ReadText());

        // 파일에 적힌 값. 기본값으로 채워졌는지 잡는 기준.
        public const float ExpectedInkLimit = 14.5f;
        public const float ExpectedBallRadius = 0.28f;
        public const float ExpectedGoalRadius = 0.55f;
        public const float ExpectedKillY = -12f;

        public static readonly Vector2 ExpectedBallStart = new Vector2(-4.5f, 3.3f);
        public static readonly Vector2 ExpectedGoalPosition = new Vector2(4.5f, -0.5f);
        public static readonly Vector2 ExpectedTerrainA = new Vector2(-5f, 3f);
        public static readonly Vector2 ExpectedTerrainB = new Vector2(5f, -1f);
    }
}
