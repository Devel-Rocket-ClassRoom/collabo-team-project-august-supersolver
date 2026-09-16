# 장치(Device) 데이터 다형화 — 구현 계획서

대상: 장치 4종이 공유하던 공통 struct `DeviceData` 를 해체하고,
**장치마다 자기 형태의 데이터 클래스**를 갖게 한다.
그 형태 그대로 `StageData` 에 저장되고, 옛 파일은 마이그레이션으로 올린다.

이 문서는 **수행 에이전트(오케스트레이터)를 위한 지시서**다.
설계 결정은 이미 사용자와 합의되어 끝났다. 다시 묻지 말고 그대로 구현한다.
새로 생긴 의문은 임의로 정하지 말고 사용자에게 묻는다.

---

## 0. 수행 규칙 (예외 없음)

1. **git 명령을 실행하지 않는다.** `commit`·`push`·`merge`·PR·태그·브랜치 이동 전부 사용자가 한다.
   읽기 전용(`git log`·`git diff`·`git status`)만 허용.
2. **테스트와 빌드를 실행하지 않는다.** Unity batchmode 로 대신 돌리지 않는다.
   사용자가 돌리고 결과를 알려 준다.
3. **작업 단위가 끝날 때마다 멈춘다.** 단위 종료 시 다음 셋을 내고 사용자의 "다음 진행"을 기다린다.
   - 바꾼 것을 **파일 단위로** 요약
   - 사용자가 직접 해야 할 검증 항목 (컴파일 · 테스트 · 맵 에디터 수동 확인)
   - **복사 가능한 코드 펜스로 커밋 메시지**
4. **`Assets/_Project/Scripts/Solver/` 하위는 닫힌 모듈이다.** 공용화·추상화 제안을 하지 않는다.
   컴파일이 깨지는 최소한만 고친다.
5. 하위 에이전트에게 넘길 때는 **작업 범위 · 건드리면 안 되는 파일 · 완료 기준**을 명시한다.
   하위 에이전트의 보고를 그대로 믿지 말고 `git diff` 로 확인한다.
6. **결정론에 영향이 갈 수 있는 판단은 무조건 묻는다.**
   난수 소비 순서 · 바디 등록 순서 · 위험(hazard) 등록 순서.

---

## 1. 확정된 설계 결정

사용자와 합의가 끝난 항목이다. **되묻지 않는다.**

| # | 결정 |
|---|---|
| 1 | **직렬화는 A안** — 타입 태그 + 중첩 JSON 문자열. Newtonsoft 를 도입하지 않는다. 레벨 json 을 손으로 고치는 일은 없다 |
| 2 | **`DeviceType` 은 enum 유지** (정수 직렬화, 추가는 뒤에만) |
| 3 | **런타임 마이그레이션을 영구 유지한다.** 저장소의 레벨 파일을 일괄 재작성하지 않는다 |
| 4 | **`L002_Feature` 는 마지막 단위에서 삭제** — 그 전까지는 전후 동일성 검증의 기준으로 쓴다 |
| 5 | **표시 분기는 선택적 인터페이스로** — Core 에 표시용 통합 디스크립터를 두지 않는다 |
| 6 | **맵 에디터의 장치 파라미터 편집 UI 는 이번 범위 밖** — 지금처럼 팔레트 기본값으로 배치만 한다 |
| 7 | **복제는 `Clone()`** — 기존 `ShapeData.Clone()` 선례를 따른다. JSON 왕복 복제를 쓰지 않는다 |
| 8 | **옛 필드 중 지금 코드가 안 읽는 값은 전부 버린다** (Spike 의 Power·Angle 등, 4-3 매핑표) |
| 9 | 카메라 영역은 **`IOccupiesCameraArea`** 로 분리하고, **영향 반경은 `IHasReach`** 로 따로 둔다 |
| 10 | 카메라 영역은 **동작 보존** — 영향 반경을 가진 장치는 그 값을 그대로 영역에 보탠다 |
| 11 | **단위 3 이 한 커밋으로 커지는 것을 받아들인다.** 쪼개려고 임시 어댑터를 세우지 않는다 |

---

## 2. 현행 구조

### 2-1. 데이터가 흐르는 길

```
Stage##.json ── StageData.FromJson ──> StageData.Level.Devices : List<DeviceData>
                                          │
   ┌──────────────────────────────────────┼─────────────────────────────────┐
   ▼ 시뮬                                  ▼ 표시                            ▼ 편집
WorldBuilder (등록 순서)                SimStyle.SpriteOf/RadiusOf/AngleOf   MapEditHandles
 └ DeviceFactory.Create (switch)        LevelView / MapSimView               MapEditView (범위 원)
    └ Bomb/FragBomb/Spike/Wind          SimStageView / SolverViewer          MapEditStyle.HasReach
SimWorld.GetDevice (MakesBody 누적)     LevelRenderer / SimScrubber          MapEditHistory (JSON 스냅샷)
                                        LevelDataArea (카메라 영역)           MapFile (저장)
```

### 2-2. 장치 타입으로 분기하는 곳 (장치를 늘릴 때 손이 가는 자리)

| 파일 | 어셈블리 | 분기 내용 |
|---|---|---|
| `Core/Sim/DeviceFactory.cs` `Create` | PPS.Core | 로직 생성 + 바디·위험 등록 |
| `Core/Sim/DeviceFactory.cs` `MakesBody` | PPS.Core | 바디 인덱스 계산의 근거 |
| `Core/SimStyle.cs` `SpriteOf`/`RadiusOf`/`AngleOf` | PPS.Core | 스프라이트 · 그릴 반경 · 회전 |
| `Core/Contracts/LevelDataArea.cs` | PPS.Core | `Radius` 로 카메라 영역 |
| `MapEditor/MapEditStyle.cs` `HasReach` | PPS.MapEditor | 범위 원을 그릴지 |
| `MapEditor/MapEditHandles.cs` `AddDeviceItem` | PPS.MapEditor | 팔레트 기본값 |
| `MapEditor/MapEditHandles.cs` `HasAngle` | PPS.MapEditor | 회전 버튼 활성 |
| `Solver/Viewer/SolverViewer.cs` `DeviceColor` | PPS.Solver.Viewer (닫힘) | 뷰어 색 |

### 2-3. 소비처 전체 목록 (단위 3 에서 전부 손댄다)

**정의**
- `Core/Contracts/DeviceData.cs` — 옛 공통 struct. 최종적으로 **삭제**
- `Core/Contracts/LevelData.cs` — `List<DeviceData> Devices`
- `Core/Contracts/StageData.cs` — JSON 진입점

**시뮬**
- `Core/Sim/DeviceFactory.cs`
- `Core/Sim/WorldBuilder.cs:70-74` — 레벨 순서대로 `Create` 호출
- `Core/Sim/SimWorld.cs:77-96` — `GetDevice`, `MakesBody` 로 바디 인덱스 누적
- `Core/Devices/{Bomb,FragBomb,Spike,Wind}Device.cs`

**표시**
- `Core/SimStyle.cs`
- `Core/Contracts/LevelDataArea.cs:38-45`
- `DrawingTool/Frontend/LevelView.cs:114-115`, `AddDevice`
- `DrawingTool/Frontend/SimStageView.cs` — `FollowDevices`
- `Game/MapSimView.cs` — `DrawDevices`
- `Solver/Viewer/SolverViewer.cs`, `Solver/Viewer/LevelRenderer.cs` (닫힌 모듈 — 최소 수정)
- `Tools/SimScrubber.cs`

**편집**
- `MapEditor/MapEditHandles.cs` — 배치 · 드래그 · 회전 · 좌우반전 · 복사/붙여넣기 · 삭제 · 집기
- `MapEditor/MapEditView.cs` — `DrawDevices`, `DrawReach`
- `MapEditor/MapEditStyle.cs`
- `MapEditor/MapEditHistory.cs` — `StageData` JSON 왕복 스냅샷 (직접 수정은 없지만 왕복 무손실이 전제)
- `MapEditor/MapFile.cs` — 저장·로드

**솔버**
- `Solver/SolutionSearch.cs:430-440` `Rebased` — `Devices` 리스트 **참조를 공유**한다

**테스트·픽스처**
- `Tests/Fixtures/{TestLevels,ViewerLevels,FeatureLevelFile}.cs`
- `Tests/EditMode/{StageContractTests,LevelDataAreaTests,FeatureLevelJsonTests}.cs`
- `Tests/EditMode/DrawingTool/LevelViewTests.cs`
- `Tests/PlayMode/{FeatureLevelSimTests,ReplayJsonSimTests}.cs`

### 2-4. 놓치면 무너지는 제약

1. **결정론.** 장치 리스트 순서 = 로직 등록 순서 = 난수 소비 순서 = 바디 등록 순서.
   `SimWorld.GetDevice` 의 "바디를 만든 장치만 한 자리" 규칙이 새 구조에서도 같은 값을 내야 한다.
2. **`SolutionSearch.Rebased` 는 리스트 참조를 공유한다.** 읽기 전용 전제가 유지되어야 한다.
   참조 타입이 되므로 **시뮬·솔버는 장치 데이터를 변형하지 않는다**를 주석으로 못 박는다.
3. **`MapEditHistory` 의 undo 는 JSON 왕복이다.** 왕복이 무손실이 아니면 undo 가 데이터를 깎는다.
4. **두 번째 JSON 진입점**: `JsonUtility.FromJson<LevelData>` — `FeatureLevelFile`, `SampleLevelFile`.
   `L002_Feature` 는 단위 4 에서 삭제한다. `SampleLevelFile`(L001)은 장치가 없어 영향이 없다.
5. **직렬화는 `JsonUtility` 다.** Newtonsoft 없음(`Packages/manifest.json` 에 `com.unity.modules.jsonserialize` 만).
   인터페이스·추상 클래스·`[SerializeReference]` 를 직렬화하지 못한다.

---

## 3. 새 구조

### 3-1. 인터페이스 — `Core/Contracts/IDeviceData.cs`

```csharp
/// <summary>
/// 배치된 장치 하나. 공통은 종류·자리·복제뿐이다 —
/// 나머지 성질은 장치마다 다르다.
/// </summary>
public interface IDeviceData
{
    DeviceType Type { get; }

    Vector2 Position { get; set; }

    /// <summary>
    /// 맵 에디터 붙여넣기가 쓴다. 참조를 공유하면
    /// 붙인 것을 옮길 때 원본이 따라 움직인다.
    /// </summary>
    IDeviceData Clone();
}

/// <summary>
/// 카메라가 잡을 영역에 이만큼을 보탠다.
/// 모든 장치가 구현한다 — 화면 밖의 장치는 대응할 수 없다.
/// </summary>
public interface IOccupiesCameraArea
{
    float AreaRadius { get; }
}

/// <summary>
/// 미치는 범위. 이걸 가진 장치만 범위 원을 그린다.
/// 가시는 몸이 곧 범위라 구현하지 않는다.
/// </summary>
public interface IHasReach
{
    float Reach { get; }
}

/// <summary>
/// 미는 방향(도). 0 이 오른쪽이다.
/// 회전·좌우반전 편집이 이걸 보고 열린다.
/// </summary>
public interface IHasFacing
{
    float FacingDegrees { get; set; }
}
```

### 3-2. 장치별 데이터 — `Core/Contracts/Devices/`

| 클래스 | 필드 | 구현 인터페이스 | `AreaRadius` |
|---|---|---|---|
| `BombData` | `Radius, Power, DelaySteps, JitterSteps` | Area, Reach | `Radius` |
| `FragBombData` | `Power, DelaySteps, JitterSteps` | Area | `FragBombDevice.BodyRadius` |
| `SpikeData` | `Radius` | Area | `Mathf.Max(Radius, SpikeDevice.MinRadius)` |
| `WindData` | `Radius, Power, Angle` | Area, Reach, Facing | `Radius` |

- 전부 `[Serializable] public sealed class`. `Position` 은 각 클래스의 필드다.
- **필드 이름은 옛 이름을 그대로 쓴다.** 클래스가 이미 장치를 밝히고 있어
  `BombData.Radius` 는 모호하지 않고, 이름까지 바꾸면 마이그레이션 매핑과 diff 가 함께 커진다.
- `WindData.FacingDegrees` 는 `Angle` 필드를 그대로 읽고 쓰는 프로퍼티다.

### 3-3. 레지스트리 — `Core/Sim/DeviceRegistry.cs`

```csharp
public delegate IStepLogic DeviceBuilder(IDeviceData data, in DeviceBuildContext ctx);

/// <summary>
/// 장치 종류마다 필요한 것을 한 줄로 모은다.
/// 새 장치를 등록하는 곳은 여기 하나다.
/// </summary>
public static class DeviceRegistry
{
    public readonly struct Entry
    {
        /// 역직렬화가 쓴다. JsonUtility.FromJson(Type, json).
        public readonly System.Type DataType;

        /// 바디 인덱스 계산의 근거. 만든 장치만 한 자리를 쓴다.
        public readonly bool MakesBody;

        public readonly DeviceBuilder Build;
    }

    static readonly Dictionary<DeviceType, Entry> Table = new Dictionary<DeviceType, Entry>
    {
        { DeviceType.Bomb,     new Entry(typeof(BombData),     true,  BombDevice.Build) },
        { DeviceType.FragBomb, new Entry(typeof(FragBombData), true,  FragBombDevice.Build) },
        { DeviceType.Spike,    new Entry(typeof(SpikeData),    true,  SpikeDevice.Build) },
        { DeviceType.Wind,     new Entry(typeof(WindData),     false, WindDevice.Build) },
    };
}
```

`DeviceBuildContext` 는 `Index, Scene, Bodies, Hazards, Events, Name` 을 든 `readonly struct` 다.

**`DeviceFactory.Create` 의 `switch` 4 개 `case` 는 각 장치 클래스의
`static IStepLogic Build(IDeviceData, in DeviceBuildContext)` 로 그대로 옮긴다.**
바디를 목록에 넣는 순서, 위험 목록에 넣는 순서를 한 줄도 바꾸지 않는다.
`DeviceFactory` 에는 표 조회와 미등록 시 예외만 남는다.

### 3-4. 직렬화 (A안)

```csharp
/// <summary>디스크에 남는 장치 한 칸. 속은 장치마다 다르다.</summary>
[Serializable]
public struct DeviceEntry
{
    public DeviceType Type;

    /// 장치별 데이터를 JsonUtility 로 찍은 것.
    public string Json;
}
```

```csharp
public class LevelData
{
    /// 디스크 형태. 런타임에 읽지 않는다.
    public List<DeviceEntry> DeviceEntries = new List<DeviceEntry>();

    /// 런타임 형태. 리스트 순서 = 등록 순서 = 난수 소비 순서.
    [NonSerialized] public List<IDeviceData> Devices = new List<IDeviceData>();

    public void PackDevices();    // Devices → DeviceEntries
    public void UnpackDevices();  // DeviceEntries → Devices
}
```

**`ISerializationCallbackReceiver` 를 쓰지 않는다.**
그 안에서 `JsonUtility` 를 다시 부르는 재진입이 안전한지 확인할 방법이 없고,
진입점이 `StageData.FromJson`/`ToJson` 둘뿐이라 거기서 명시적으로 부르는 편이
단순하고 테스트가 쉽다.

- `StageData.ToJson()` → `Level.PackDevices()` 후 직렬화
- `StageData.FromJson()` → 역직렬화 → 마이그레이션 → `Level.UnpackDevices()`

소비처는 `level.Devices` 라는 이름을 그대로 쓴다. 원소 타입만 `IDeviceData` 로 바뀐다.

#### 디스크 json 예시 (Stage15 의 장치 3개)

```json
"DeviceEntries": [
  { "Type": 0, "Json": "{\"Position\":{\"x\":-3.1775584,\"y\":2.7242846},\"Radius\":2.0,\"Power\":11.0,\"DelaySteps\":30,\"JitterSteps\":0}" },
  { "Type": 1, "Json": "{\"Position\":{\"x\":1.3058459,\"y\":1.5272601},\"Power\":6.0,\"DelaySteps\":30,\"JitterSteps\":0}" },
  { "Type": 2, "Json": "{\"Position\":{\"x\":2.7858047,\"y\":5.2053928},\"Radius\":0.3}" }
]
```

이스케이프된 문자열이 남아 손으로 읽기 나빠지는 것이 A안의 값이다. 받아들인 결정이다.

### 3-5. 표시 경로

```csharp
// 지금:  float r = SimStyle.RadiusOf(device);   // Core 안의 switch
// 앞으로: 몸 크기는 SimStyle 에셋 테이블, 범위는 인터페이스
if (device is IHasReach reach) DrawRing(device.Position, reach.Reach);
if (device is IHasFacing facing) rotation = Quaternion.Euler(0f, 0f, facing.FacingDegrees);
```

`SimStyle.Shapes` 의 장치 스프라이트 필드 4개(`Bomb`·`FragBomb`·`Spike`·`Wind`)를
`List<DeviceVisual { DeviceType Type; Sprite Sprite; float BodyRadius; }>` 로 바꾼다.
`SpriteOf`/`RadiusOf` 의 `switch` 가 사라지고, 새 장치는 **에셋에 항목 하나**로 끝난다.

> 대가: 기존 `SimStyle` 에셋을 한 번 손으로 다시 채워야 한다(단위 3-3).
> 에셋 경로와 개수는 수행 시 확인한다.

### 3-6. 새 장치를 추가할 때 손대는 것

1. `XxxData.cs` — 데이터 클래스 (필요한 선택 인터페이스 + `Clone()`)
2. `XxxDevice.cs` — 장치 클래스 (`IStepLogic` + `static Build`)
3. `DeviceType` enum 에 값 하나 (**뒤에만** 추가 — 정수로 직렬화된다)
4. `DeviceRegistry.Table` 에 한 줄
5. `SimStyle` **에셋**에 항목 하나 (코드 아님)

3·4 가 합의된 "등록 한 줄"이다.
이것까지 없애려면 리플렉션 스캔이어야 하는데 IL2CPP 스트리핑을 신경 써야 하고
오타를 컴파일러가 못 잡는다 — 하지 않는다.

---

## 4. 마이그레이션

### 4-1. 버전 체계

```csharp
public class StageData
{
    public const int CurrentVersion = 1;

    /// 없는 파일 = 0 = 옛 공통 DeviceData 형식.
    public int Version;

    public static StageData FromJson(string json)
    {
        var stage = JsonUtility.FromJson<StageData>(json);
        if (stage == null) return null;

        // 원본 문자열이 함께 간다. JsonUtility 가 새 형식에
        // 없는 필드를 이미 버려서, 옛 값은 여기서만 되찾을 수 있다.
        StageDataMigration.Migrate(stage, json);
        return stage;
    }
}
```

```csharp
/// <summary>옛 형식으로 저장된 판을 현재 형식으로 올린다.</summary>
public static class StageDataMigration
{
    // 단계는 순서대로 적용된다. 각 단계는 앞 단계가
    // 끝난 상태를 전제한다. 버전은 끝에서 한 번만 올린다.
    public static void Migrate(StageData stage, string json)
    {
        if (stage.Version < 1) ToV1(stage, json);

        stage.Version = StageData.CurrentVersion;
    }

    // 공통 DeviceData 하나가 장치별 데이터로 갈라졌다.
    static void ToV1(StageData stage, string json) { /* 레거시 DTO 재파싱 → 새 리스트 */ }
}
```

`UserDataMigration.cs` 와 같은 꼴이다. 그 파일을 본보기로 삼는다.

### 4-2. 레거시 DTO

`internal` 로 두고 옛 `DeviceData` struct 를 **그대로 복제**한다.
옛 형식은 더 이상 바뀌지 않으므로 이 복제는 영구히 얼어붙는다.

```csharp
[Serializable] class LegacyStageV0 { public LegacyLevelV0 Level; }
[Serializable] class LegacyLevelV0 { public List<LegacyDeviceV0> Devices; }
[Serializable] struct LegacyDeviceV0
{
    public DeviceType Type;
    public Vector2 Position;
    public float Radius;
    public float Power;
    public int DelaySteps;
    public int JitterSteps;
    public float Angle;
}
```

버전 1 파일에는 `Devices` 키가 없어 재파싱 결과가 비고, 그때는 `UnpackDevices()` 만 돈다.

### 4-3. 옛 ↔ 새 필드 매핑

| 옛 필드 | Bomb | FragBomb | Spike | Wind |
|---|---|---|---|---|
| `Position` | `Position` | `Position` | `Position` | `Position` |
| `Radius` | `Radius` | **버림** | `Radius` | `Radius` |
| `Power` | `Power` | `Power` | **버림** | `Power` |
| `DelaySteps` | `DelaySteps` | `DelaySteps` | **버림** | **버림** |
| `JitterSteps` | `JitterSteps` | `JitterSteps` | **버림** | **버림** |
| `Angle` | **버림** | **버림** | **버림** | `Angle` |

**버리는 값은 전부 지금 코드가 읽지 않는 값이다.**
`SpikeDevice` 는 `Position`·`Radius` 만, `WindDevice` 는 `Position`·`Radius`·`Power`·`Angle` 만,
`FragBombDevice` 는 `Position`·`Power`·`DelaySteps`·`JitterSteps` 만 쓴다.
→ **마이그레이션은 시뮬 결과를 바꾸지 않는다.** 이것이 검증의 핵심 주장이다.

딱 하나 바뀌는 것: `FragBombData` 가 `Radius` 를 잃어 카메라 영역 기여가
`Radius`(저장소의 파일들은 전부 0) → 몸 크기 `0.28` 이 된다.
판 하나당 최대 0.28wu 넓어지고 여백 `AreaMargin = 2f` 에 묻힌다.
**눈에 띄면 그때 사용자에게 알리고 조정한다.**

### 4-4. 마이그레이션 검증

- **왕복**: `FromJson(옛 json).ToJson()` → 다시 `FromJson` → 새 구조 필드가 동일
- **매핑**: 장치 4종의 옛 json 문자열 리터럴을 넣어 위 표대로 들어갔는지
- **시뮬 동일성**: 단위 1 의 기준선 해시

---

## 5. 작업 단위

각 단위는 독립적으로 컴파일되고 커밋 가능해야 한다.
단위가 끝나면 **0-3 규칙대로 멈추고 사용자의 응답을 기다린다.**

### 단위 1 — 기준선 해시 (현행 코드, 회귀 그물)

코드 변경 없음. 테스트만 추가한다.

1. `Stage11~16, 18, 19, 20` 과 `L002_Feature`(솔루션 포함)를 고정 시드로 돌려
   궤적 해시를 상수로 박은 PlayMode 테스트를 작성한다
   → **검증: 현행 코드에서 전부 통과**
2. 이 테스트는 단위 2~4 내내 그대로 통과해야 한다
   → **검증: 해시 불변**

> 먼저 하는 이유: 이후 어느 단위에서 결정론이 깨져도 어디서 깨졌는지 즉시 잡힌다.
> 없으면 "마이그레이션이 틀렸나 전환이 틀렸나"를 구분할 수 없다.
>
> 구현 참고: `Tests/PlayMode/FeatureLevelSimTests.cs:89` 의 `SimRunner.RunTraced` 사용례.
> 스테이지 json 을 읽는 경로는 `StageData.FromJson` 이다.

### 단위 2 — Core 데이터 계층 신설 (소비처 무관)

전부 신규 파일이다. **기존 `DeviceData` 와 소비처는 한 줄도 건드리지 않는다**
→ 중간에도 컴파일이 깨지지 않는다.

1. 인터페이스 4종 + 데이터 클래스 4종 + `DeviceEntry` + `DeviceBuildContext` +
   `DeviceRegistry` + 각 장치 클래스의 `static Build`
   (`DeviceFactory` 는 아직 옛 경로를 쓴다)
   → **검증: 컴파일**
2. `LevelData.PackDevices`/`UnpackDevices` + `StageDataMigration` + 레거시 DTO
   → **검증: EditMode 신규 테스트 — 왕복 무손실, 매핑표 4종**
3. → **검증: 기존 테스트 전부 통과 (단위 1 해시 포함)**

### 단위 3 — 전면 전환 (큰 커밋)

`LevelData.Devices` 의 타입이 바뀌는 순간 소비처 10여 곳이 한꺼번에 깨진다.
가짜 발판 없이는 쪼갤 수 없고, 발판을 세우면 그것을 걷어내는 커밋이 하나 더 생긴다.
**한 번에 간다** (결정 11).

1. `LevelData` 전환 + `StageData.Version`/`FromJson`/`ToJson` 에 마이그레이션·팩 연결
   → **검증: 컴파일**
2. 시뮬: `DeviceFactory` 를 표 조회로, `MakesBody` 를 레지스트리로,
   `SimWorld.GetDevice` 의 인덱스 계산 유지
   → **검증: 단위 1 해시 불변** ← 결정론의 관문
3. 표시: `SimStyle` 테이블화 + **에셋 재설정(수동)**,
   `LevelView`·`MapSimView`·`SimStageView`·`SolverViewer`·`LevelRenderer`·`SimScrubber`·`LevelDataArea`
   → **검증: `LevelDataAreaTests` 통과 + 게임 씬에서 Stage15·Stage20 눈으로 확인**
4. 편집: `MapEditStyle.HasReach` → `is IHasReach`, `MapEditHandles.HasAngle` → `is IHasFacing`,
   드래그의 "꺼내→고쳐→도로 넣기" 되넣기 제거, 복사/붙여넣기는 `Clone()`
   → **검증(수동): 장치 4종 배치 → 드래그 → 회전 → 좌우반전 → 복사·붙여넣기 →
     붙인 것만 움직이는지 → undo/redo → 저장 → 다시 열기**
5. 옛 `DeviceData` struct 삭제, 테스트 픽스처(`TestLevels`·`ViewerLevels`·`LevelViewTests`·
   `StageContractTests`·`LevelDataAreaTests`) 갱신
   → **검증: 전체 테스트 통과**

> 주의: `SolutionSearch.Rebased` 는 `Devices` 리스트 참조를 공유한다.
> 그대로 두되, 시뮬·솔버가 장치 데이터를 변형하지 않는다는 전제를 주석으로 남긴다.
> `Solver/` 는 닫힌 모듈이므로 **컴파일이 깨지는 최소한만** 고친다.

### 단위 4 — `L002_Feature` 정리

1. `ReplayJsonSimTests` 가 쓸 **대체 레벨+솔루션 픽스처**를 코드로 만든다
   (`TestLevels` 에 장치가 든 판 + 솔루션)
   → **검증: 대체 픽스처로 리플레이 왕복 결정론 테스트 통과**
2. 삭제: `Levels/L002_Feature.json`, `Levels/L002_Feature.solution.json`,
   `Tests/Fixtures/FeatureLevelFile.cs`, `Tests/EditMode/FeatureLevelJsonTests.cs`,
   `Tests/PlayMode/FeatureLevelSimTests.cs`, `Tools/SimScrubber.cs:472` 의 항목 한 줄
   (`.meta` 파일도 함께)
   → **검증: 컴파일 + 전체 테스트 통과**
3. 단위 1 의 기준선 해시에서 `L002` 항목 제거
   → **검증: 나머지 9판 해시 불변**

> `L002_Feature.json` 은 `StageData` 가 아니라 **`LevelData` 통짜 json** 이고
> `JsonUtility.FromJson<LevelData>` 로 직접 읽힌다(`FeatureLevelFile.cs:26`).
> 이것을 지우면 두 번째 JSON 진입점이 사라진다.
> 단위 3 까지는 전후 동일성의 기준이므로 **먼저 지우지 않는다.**

---

## 6. 범위 밖

- 맵 에디터의 장치 파라미터 편집 UI (다음 작업으로 예정)
- 카메라 프레이밍 규칙 변경 (영향 반경 → 몸 크기)
- 솔버 내부 구조 변경 · 공용화 · 추상화
- 레벨 파일 일괄 재작성 (런타임 마이그레이션을 영구 유지하기로 했다)
- 기존에 이미 있던 죽은 코드 제거 (발견하면 언급만 한다)
