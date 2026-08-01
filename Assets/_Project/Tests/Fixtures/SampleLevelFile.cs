using System.IO;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 저장소에 커밋된 샘플 레벨 JSON. **"레벨 추가는 데이터만으로"의 증거물**이다.
    ///
    /// 미션 요구는 레벨 추가에 코드 수정이 필요하면 미충족이다. 그런데 <c>TestLevels</c> 의
    /// 레벨은 전부 C# 코드라 그 요구를 증명하지 못한다 — 직렬화 왕복 테스트가 있어도
    /// 그것은 "타입이 JSON 을 견딘다"이지 "파일에서 레벨이 선다"가 아니다.
    ///
    /// 값을 <see cref="LevelData"/> 의 기본값과 **일부러 다르게** 잡아 두었다
    /// (잉크 14.5, 공 반지름 0.28, 목표 반지름 0.55, KillY -12).
    /// JsonUtility 는 모르는 키를 조용히 무시하고 없는 키를 기본값으로 채우므로,
    /// 기본값과 같은 숫자를 쓰면 파싱이 실패해도 테스트가 통과해 버린다.
    /// </summary>
    public static class SampleLevelFile
    {
        /// Assets 아래 상대 경로. 런타임 로더가 아니라 **에디터·테스트 전용** 접근이다.
        public const string RelativePath = "_Project/Levels/L001_Ramp.json";

        public static string FullPath => Path.Combine(Application.dataPath, RelativePath);

        public static bool Exists => File.Exists(FullPath);

        public static string ReadText() => File.ReadAllText(FullPath);

        public static LevelData Load() => LevelData.FromJson(ReadText());

        // 파일에 적힌 값. 테스트가 이 상수와 대조해 "조용히 기본값으로 채워졌는지"를 잡는다.
        public const string ExpectedId = "L001_Ramp";
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
