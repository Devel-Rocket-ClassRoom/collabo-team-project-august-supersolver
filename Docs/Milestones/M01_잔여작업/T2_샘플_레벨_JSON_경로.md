# T2 — 샘플 레벨 JSON → 월드 구축 경로

| | |
|---|---|
| **대상 체크리스트 항목** | C-2 |
| **담당** | 본인 (코어) |
| **선행** | 없음 — 지금 착수 가능 |
| **성격** | 데이터 파일 + 테스트 추가. 계약 타입 변경 없음 |
| **상태** | **구현 완료 — 실행 검증 대기** |

## 왜

미션 요구는 **"레벨 추가에 코드 수정이 필요하면 요구사항 미충족"** 이다.
`LevelData` 클래스 주석도 "데이터 추가만으로 동작해야 한다"를 첫 줄에 적어 두었다.

그런데 지금 저장소에는 **레벨 JSON 파일이 하나도 없다.**
`ContractTests.레벨데이터는_JSON_왕복에서_보존된다`는 C# 객체 → JSON → 객체 왕복만 본다.
이것은 직렬화가 동작한다는 증명이지, **파일에서 읽어 레벨이 선다**는 증명이 아니다.
현재 모든 레벨은 `TestLevels.cs`의 C# 코드다 — 즉 레벨 추가에 코드 수정이 필요한 상태다.

팀원 C의 에디터가 나오기를 기다릴 필요는 없다. 손으로 쓴 JSON 한 개면 경로가 증명된다.
그것이 곧 C에게 넘길 **입력 포맷의 실물 예시**이기도 하다.

## 할 일

### 1. 샘플 JSON 커밋

배치 위치는 `Assets/_Project/Levels/` 를 제안한다. 코어 asmdef 밖의 순수 데이터이므로 어셈블리와 무관하다.

`LevelData`가 `JsonUtility` 대상이므로 **필드명이 JSON 키와 정확히 일치해야 한다** —
`Id`, `InkLimit`, `BallStart`, `BallRadius`, `GoalPosition`, `GoalRadius`, `Terrain`, `KillY`.
`JsonUtility`는 알 수 없는 키를 **조용히 무시하고 없는 키를 기본값으로 채우므로**, 오타가 나도 예외 없이
엉뚱한 레벨이 만들어진다. 이 특성 때문에 아래 2번의 값 검증이 반드시 필요하다.

내용은 `TestLevels.RampToGoal()`과 동일한 배치로 한다 — 이미 `Clear`가 나오는 것이 확인된 레벨이라
JSON 경로가 잘못됐을 때 결과 차이로 바로 드러난다.

### 2. 로더 테스트 추가

PlayMode 쪽에 둔다 (월드를 만들기 때문).

- 파일을 읽어 `LevelData.FromJson` → `WorldBuilder.Build` → 월드 구축 성공
- **역직렬화된 값이 파일 내용과 일치**하는지 확인 — `BallStart`, `GoalPosition`, `InkLimit` 최소 3개.
  `JsonUtility`의 조용한 기본값 채우기를 잡는 유일한 방법이다
- 시뮬을 돌려 `SimOutcome.Clear`가 나오는지. C# 코드 레벨과 **같은 결과**여야 한다

### 3. 파일 접근 방식 결정

테스트는 `Application.dataPath` 기반 경로로 직접 읽으면 된다
([CoreSourceGuardTests.cs:72](../../../Assets/_Project/Tests/EditMode/CoreSourceGuardTests.cs:72)가 같은 방식을 쓴다).

다만 **런타임 로더는 M01 범위 밖**이다. 빌드된 APK에서 `Application.dataPath`로 `Assets/` 를 읽을 수 없으므로
`Resources` 또는 `StreamingAssets`가 필요한데, 그 선택은 팀원 D의 빌드·자동 저장 작업과 맞물린다.
여기서 증명할 것은 "JSON에서 월드가 선다"까지다.

## 구현 (완료)

| 파일 | 내용 |
|---|---|
| `Assets/_Project/Levels/L001_Ramp.json` | 손으로 쓴 샘플 레벨 |
| `Tests/Fixtures/SampleLevelFile.cs` | 경로·로더 + **파일에 적힌 값의 상수 사본** |
| `Tests/EditMode/LevelJsonTests.cs` (4) | 파일 존재, 값 대조, 지형, 앵커 원료 |
| `Tests/PlayMode/LevelJsonSimTests.cs` (4) | 월드 구축, 값이 물리에 반영, `Clear`, 왕복 불변 |

**샘플 값을 `LevelData` 기본값과 일부러 다르게 잡았다** — 잉크 14.5(기본 20), 공 반지름 0.28(0.25),
목표 반지름 0.55(0.5), KillY −12(−20). `JsonUtility` 는 모르는 키를 조용히 무시하고 없는 키를
기본값으로 채우므로, 기본값과 같은 숫자를 쓰면 **키 이름에 오타가 나도 테스트가 통과한다.**

`SampleLevelFile` 이 기대값 상수를 함께 들고 있어 EditMode·PlayMode 양쪽이 같은 기준으로 대조한다.

뷰어 카탈로그에도 `L001 (JSON 파일)` 항목을 넣었다 — 파일에서 읽은 레벨이 실제로 도는 것을 눈으로 볼 수 있다.

## 완료 조건

- [x] 손으로 작성한 레벨 JSON 최소 1개가 저장소에 커밋됨
- [x] JSON에 공 시작점·목표 위치·잉크 제한이 포함됨
- [x] 파일 → `LevelData` → 월드 구축이 테스트로 확인됨
- [x] 역직렬화 값이 파일 내용과 일치함이 확인됨 (기본값으로 조용히 채워지지 않았다는 증명)
- [x] 파일에서 읽은 레벨이 `Clear` 되고, JSON 왕복이 시뮬 결과를 바꾸지 않음
- [ ] **테스트 실행 검증 대기**

## 범위 밖

- 런타임 레벨 로더 (`Resources` / `StreamingAssets` 선택) — D의 빌드 작업과 함께
- 레벨 목록·진행도 관리 — M03 이후
- 스키마 검증·에러 리포팅 — C의 에디터가 생성기를 가지면 불필요해질 수 있다
- `LevelData` 필드 확장 (장치·별·앵커) — C의 작업. 이 문서는 **현재 골격**만 증명한다
