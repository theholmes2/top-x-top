# 탑x탑 (Top X Top)

**자동전투의 실행 흐름과 코드 역할을 하나씩 검토하며 제작 중인 Unity 2D 방치형 게임입니다.**

현재는 몬스터 스폰·풀링, 기본 자동공격, 이동과 대시 연출, 체력바를 연결한 로컬 프로토타입입니다. 완성된 서비스가 아니며, 다음 단계에서는 기존 동작을 유지하면서 전투 시간 정책과 연출 계산의 책임을 정리할 예정입니다.

## 프로젝트 정보

| 항목 | 내용 |
|---|---|
| 개발 형태 | 개인 개발 · AI 보조 개발 |
| 엔진 | Unity 6000.3.19f1 · 2D URP |
| 언어 | C# |
| 주요 적용 기술 | VContainer 1.19.0, R3 1.3.1, 이벤트 버스 패턴, 체력 UI의 MVVM |
| 개발 브랜치 | `dev` |
| 현재 범위 | Unity Editor에서 실행하는 로컬 자동전투 |

이전 `top of top` 프로젝트를 참고하되, 이 저장소에서는 필요한 요소의 역할·입출력·연결을 검토하며 다시 구성하고 있습니다. 다른 프로젝트의 서버·저장·멀티플레이 구현을 이 프로젝트의 성과로 포함하지 않습니다.

## 현재 구현

- **캐릭터 상태:** ScriptableObject의 기본 능력치로 상태를 초기화하고, 플레이어·적의 공통 수치와 증감·범위 제한을 공유합니다.
- **난이도별 스폰:** 난이도 1~5의 프리팹 목록, 종류별 최대 수, 난이도 전체 최대 스폰 수를 사용합니다.
- **풀링과 대상 선택:** 프리팹별 풀에서 적을 대여하고, 활성 적을 등장 순서대로 관리해 앞에 도착한 적부터 공격합니다.
- **기본 자동공격:** 기본공격도 스킬 데이터로 정의하며, 기본 쿨타임 1초와 요청 시각을 기준으로 한 번씩 실행합니다. 이동·선딜·후딜 때문에 실제 공격 간격은 길어질 수 있습니다.
- **이동과 대시:** 거리와 남은 쿨타임을 바탕으로 접근을 계산하고, 적·배경의 이동과 플레이어의 Run·잔상 연출을 연결합니다.
- **공격·피격·사망:** 성공한 공격의 모션을 1→2→3으로 순환하고, 타격 시점에 체력 변경과 피격·사망 표시를 연결합니다. 피해 적용은 Animator 종료 콜백에 의존하지 않습니다.
- **사망 중 개체 관리:** 사망 연출 개체를 활성 전투 목록에서 분리하고, 연출 종료 후 풀에 반환합니다. 사망 처리 후 활성 적이 없으면 즉시 한 마리를 보충합니다.
- **체력 UI:** R3 ViewModel이 현재·최대 체력을 전달하고, View는 이를 구독해 체력바를 표시합니다. 풀 반환 시 구독을 정리하고 재대여 시 새 상태에 연결합니다.

## 코드 흐름과 설계 의도

```text
CombatCompositionRoot (VContainer / LifetimeScope)
  └─ SimulationTickGlue (IStartable / ITickable)
       ├─ SpawnScheduleBrick → EnemySpawnGlue → EnemyPoolGlue
       ├─ ApproachBrick → 적·배경 이동 / 플레이어 대시 연출
       └─ SkillUseBrick → AttackBrick → EnemyState의 체력 변경
            ├─ CombatHitBus → 피격 연출
            └─ EnemyHealthViewModel (R3) → EnemyHealthBarGlue
```

| 선택 | 적용과 목적 | 대표 코드 |
|---|---|---|
| VContainer | Root에서 실행 객체와 서비스를 조립하고 전투 Scope의 수명을 관리 | [CombatCompositionRoot](Assets/Scripts/Simulation/CombatCompositionRoot.cs) |
| 중앙 갱신 | 스폰·이동·공격의 실행 순서를 한 흐름에서 조율 | [SimulationTickGlue](Assets/Scripts/Simulation/SimulationTickGlue.cs) |
| Brick–Glue | Unity가 필요 없는 계산과 Unity 객체 연결을 분리하는 기준으로 사용 | [AttackBrick](Assets/Scripts/Combat/AttackBrick.cs), [EnemySpawnGlue](Assets/Scripts/Enemy/EnemySpawnGlue.cs) |
| 이벤트 버스 | 확정된 양수 피해를 방송해 공격 계산과 피격 연출의 직접 연결을 줄임 | [CombatHitBus](Assets/Scripts/Combat/CombatHitBus.cs) |
| R3 / MVVM | 체력 상태 관찰과 Unity UI 표시를 분리하고 대여별 구독 수명을 관리 | [EnemyHealthViewModel](Assets/Scripts/Enemy/EnemyHealthViewModel.cs), [EnemyHealthBarGlue](Assets/Scripts/Enemy/EnemyHealthBarGlue.cs) |
| ScriptableObject | 능력치·스킬·난이도 기본값과 프리팹 참조를 에셋으로 관리 | [Configs](Assets/Configs) |

현재 체력 ViewModel은 Unity View에 의존하지 않습니다. 다만 전체 게임에 MVVM이 적용됐거나 모든 계산이 Glue에서 분리된 상태는 아닙니다. 중앙 Tick의 전투 시간 정책과 연출 기준값 결합 등 남은 문제는 리팩터링 대상으로 기록했습니다.

## 실행 방법

1. Git LFS를 사용할 수 있는 환경에서 저장소의 `dev` 브랜치를 내려받습니다. LFS 파일이 포인터로 남아 있으면 `git lfs pull`을 실행합니다.
2. Unity Hub에서 Unity **6000.3.19f1**로 프로젝트를 엽니다.
3. UPM 및 NuGetForUnity 패키지 복원과 스크립트 컴파일이 끝날 때까지 기다립니다.
4. `Assets/Scenes/SampleScene.unity`를 열고 Play를 누릅니다.
5. 적이 등장·접근하고, 자동공격 후 체력바·피격·사망·다음 적 처리로 이어지는 흐름을 확인합니다.

씬의 `CombatCompositionRoot`에서 `Starting Difficulty` 참조를 바꾸면 다른 난이도 정의를 확인할 수 있습니다. 현재 흐름에는 수동 공격 조작이나 서버 실행이 필요하지 않습니다.

## 검증 기록과 다음 작업

검증 자료는 작업 시점별 기록입니다. 과거 스폰·전투 검사 문서에는 이후 변경된 동작이 포함될 수 있으므로, 현재 구현과 해당 문서의 검증 시점을 함께 봐야 합니다.

- [몬스터 풀·스폰 검증](Docs/몬스터_풀_스폰_검증.md)
- [중앙 갱신 검증](Docs/ITickable_중앙갱신_검증.md)
- [플레이·스폰·전투 검증](Docs/플레이_스폰_전투_검증.md)
- [리팩터링 검토 및 개발 인계안 — 2026-11-18](Docs/리팩터링_검토_2026-11-18.md)

다음 작업은 전투 시간 정책·잔상·스폰 배치 계산의 책임 분리, 예외 발생 시 상태 일관성, 필수 참조와 입력값 검증, 가독성 보완입니다. 정기 스폰의 시간 누적 지연 개선은 동작을 바꾸는 작업으로 별도 검증할 계획입니다.

빌드·실기기 성능·서버 연동까지 검증 완료한 프로젝트로 소개하지 않습니다. 이번 README 작성 과정에서는 게임 코드를 변경하거나 새 플레이 테스트를 실행하지 않았습니다.

## 향후 목표와 현재 경계

서버 저장·복원, 오프라인 진행, 보상·성장 흐름은 아직 이 프로젝트의 실행 기능에 연결하지 않았습니다. DOTween의 실제 기능 적용, Unity IAP 5 D2C, Unity Diagnostics도 향후 목표이며 구현·연동 완료로 취급하지 않습니다. 현재 이벤트 버스의 적용은 피격 신호이며, 처치 신호를 의뢰·업적 등 여러 소비자에 전달하는 사례는 후속 범위입니다.

## AI 활용과 직접 수행한 역할

Codex를 코드 구조 조사, 구현안 검토, 승인한 범위의 코드 작성, 코드 설명과 검증 보조에 활용합니다. 프로젝트 공통 기준은 [AGENTS.md](AGENTS.md)에 기록하며, 개발·코드 설명·계획·구조도 채팅의 역할을 나눠 진행합니다.

- **직접 수행:** 기능 요구사항과 동작 정책 결정, 구현안·코드 설명 검토, 불명확한 조건과 부작용 질문, Unity 직접 플레이를 통한 화면·로그 확인, 수정 범위 승인.
- **AI 활용:** 기존 코드와 연결 관계 조사, 설계 대안·위험 조건 정리, 요청한 코드·문서 작성, Unity CLI를 통한 에디터 확인 및 검증 보조.
- **작업 순서:** 요구사항 정리 → 기존 구조 조사 → 구현안과 예외 조건 검토 → 범위 승인 → 구현 → 코드 설명 확인 → 테스트·직접 플레이 → 수정 사항 기록.

별도 채팅의 설명이나 도구의 성공 응답만으로 정확성을 보장하지 않으며, 실제 코드·컴파일 상태·검증 기록과 직접 플레이를 함께 확인합니다.

## 리소스 출처

캐릭터·몬스터 리소스의 출처는 [이미지 출처 기록](Assets/Art/Characters/ATTRIBUTION.md)에 정리돼 있습니다. 숲 배경은 기존 개인 프로젝트에서 사용하던 리소스를 참고해 연결했습니다. 폰트는 Noto Sans KR을 사용하며 [동봉 라이선스](Assets/Art/Fonts/NotoSansKR/OFL.txt)를 따릅니다. 외부 리소스의 권리는 각 원저작자에게 있으며, 저장소 전체가 동일한 라이선스로 제공되는 것은 아닙니다.
