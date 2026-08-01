using System.IO;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// **지금 표현할 수 있는 모든 피처를 담은 레벨·솔루션 한 쌍.**
    ///
    /// 레벨과 솔루션을 파일 두 개로 나눈 것은 취향이 아니라 계약이다 —
    /// <see cref="LevelData"/> 는 "퍼즐"(공·목표·지형·잉크 제한)이고
    /// <see cref="Solution"/> 은 "거기 그은 그림"(스트로크·회전축)이다.
    /// 자유 물체와 회전축은 레벨이 아니라 솔루션에 속하며, 솔버의 출력이자 리플레이의 본체다.
    ///
    /// 담긴 것: 다중 지형 세그먼트 · **장치(폭탄)** · 고정선 · 자유 물체(직선·닫힌 사각형·닫힌 다각형)
    /// · 스트로크 간 회전축 · 월드 고정 회전축. **현재 코어가 표현할 수 있는 전부다.**
    /// </summary>
    public static class FeatureLevelFile
    {
        public const string LevelRelativePath = "_Project/Levels/L002_Feature.json";
        public const string SolutionRelativePath = "_Project/Levels/L002_Feature.solution.json";

        public static string LevelPath => Path.Combine(Application.dataPath, LevelRelativePath);
        public static string SolutionPath => Path.Combine(Application.dataPath, SolutionRelativePath);

        public static bool Exists => File.Exists(LevelPath) && File.Exists(SolutionPath);

        public static LevelData LoadLevel() => LevelData.FromJson(File.ReadAllText(LevelPath));

        /// <summary>
        /// <see cref="Solution"/> 에는 <c>ToJson</c>/<c>FromJson</c> 이 없어 여기서 직접 읽는다.
        /// 코어 계약을 넓히지 않으려는 것이다 — 리플레이 스키마를 팀원 D 와 확정할 때
        /// 함께 정하는 편이 낫고, 그전까지는 테스트 픽스처가 알고 있으면 충분하다.
        /// </summary>
        public static Solution LoadSolution() => JsonUtility.FromJson<Solution>(File.ReadAllText(SolutionPath));

        // 파일에 적힌 구성. 조용히 기본값으로 채워졌는지 잡는 기준이다.
        public const string ExpectedId = "L002_Feature";
        public const float ExpectedInkLimit = 24f;
        public const float ExpectedBallRadius = 0.26f;
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
