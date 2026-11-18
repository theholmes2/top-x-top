# ITickable 중앙 갱신 전환 — 2026-11-03

> 2026-11-05 갱신: 현재 실행 흐름은 [플레이_스폰_전투_검증.md](플레이_스폰_전투_검증.md)를 따른다. 아래는 당시 구현·검증 이력이다. 고정 EnemyState 생성, 타깃 보관 생성자, 풀·스폰의 Root 미연결 설명은 현재 실행 경로에 적용하지 않는다.
>
> 현재 API: `new SkillUseBrick(attack, cooldownMs, powerPermille)` → `TryUse(nowMs, currentTarget)`.
> 현재 Root는 풀·스폰을 타입 등록하고 시작 난이도·스폰 위치를 EntryPoint에 공급한다. 활성 개체는 등장 순서 List이고 전체 상한도 적용한다.


## 최신 흐름

Config → CombatCompositionRoot 조립 → VContainer EntryPoint의 Tick → SkillUseBrick.TryUse(nowMs) → AttackBrick.TryAttack → CharacterState HP 변경.

기존 SimulationUpdateGlue의 MonoBehaviour.Update는 제거했다. 파일과 meta는 Unity AssetDatabase.MoveAsset으로 SimulationTickGlue.cs로 이동했고 GUID 8c6232139fbe54b40a499c63f44a1707을 유지했다. 씬에는 이전 컴포넌트만 제거했고 Root/Camera/Light와3개 Config 참조를 보존했다. 중앙 전용 GameObject나 두 번째 Update를 추가하지 않았다.

SimulationTickGlue는 일반 C# 클래스이며 ITickable을 구현한다. 생성자로 SkillUseBrick을 받고, Tick에서 Unity 게임 시간을 정수 밀리초로 바꿔 basicAttack.TryUse(nowMs)를 한 번 요청한다. 피해·쿨타임 계산은 없다. readonly 필드는 생성 시 받은 사용 객체 참조를 유지하고, MillisecondsPerSecond=1000은 초→ms 변환 계수다.

Root는 RegisterEntryPoint<SimulationTickGlue>(Lifetime.Scoped)로 등록한다. VContainer가 생성자에 기존 SkillUseBrick을 공급하고 자신의 PlayerLoop에서 Tick을 호출한다. 단순 RegisterInstance만으로 프레임 호출이 된다고 가정하지 않았다. MonoBehaviour가 아닌 Tick 객체에 Inspector 에셋을 넣지 않으며, 에셋 참조와 상태 생성은 MonoBehaviour인 LifetimeScope Root에 남는다.

AttackBrick/SkillUseBrick은 이번 전환에서 수정하지 않았다. 순수 Brick은 UnityTime/MonoBehaviour/VContainer/ITickable을 모른다. 기본1000ms·최초 요청 즉시·성공 후 다음시각 갱신·실패 미소비·0피해 성공 소비·요청당최대1회·독립 스킬 시각을 유지했다. 프레임당 요청과 1초당 성공은 다른 개념이다.

## 파일별 읽는 순서

1. SkillConfig.cs: ID/표시명/쿨타임/위력 고정 정의. BaseCooldownMs=1000, PowerPermille=1000은 기본공격 임시값이다. 사용자별 다음 시각을 SO에 기록하지 않는다.
2. CombatCompositionRoot.cs: Inspector3개SO를 읽어 PlayerState/EnemyState/AttackBrick/SkillUseBrick을 한 번 생성·등록한다. 마지막 RegisterEntryPoint가 중앙 Tick 수명과 호출을 연결한다.
3. SimulationTickGlue.cs: 생성자 의존성 수신, 게임 시각 변환, TryUse 한 줄. 이 클래스의 이름 Glue는 Unity/VContainer와 순수 로직의 연결 책임을 뜻한다.
4. SkillUseBrick.cs: nowMs<NextUseAtMs면 거절, 범위 검증, 공격 효과 성공 시 NextUseAtMs=nowMs+cooldownMs. 기본과 강타는 별도 사용 인스턴스라 서로 잠그지 않는다.
5. AttackBrick.cs: 현재 공격력×위력/1000의 소수점 버림 후 방어력을 빼고 실제 HP 감소량을 반환한다. 타깃/생존 검사도 수행하며 시간은 다루지 않는다.
6. CharacterState/PlayerState/EnemyState는 이미 있던 현재 상태다. Root는 SO 기본값으로 각각 별도 인스턴스를 만들고 공격 효과는 그 같은 참조를 계속 읽는다.

예를 들어0ms에 성공하면 다음1000이다. 매 프레임 요청해도0ms 재요청·999ms 요청은 false,1000ms에서 한 번 성공한다. 늦게5000ms에 요청하면 밀린 횟수를 반복하지 않고 한 번 성공한 뒤 다음6000으로 잡는다. timeScale/게임 일시정지 영향을 받는 시각이며 서버 UTC가 아니다.

## 수명

한 Scene Scope에 Tick EntryPoint 하나다. Root 파괴/Scope Dispose 시 VContainer의 EntryPointDispatcher가 PlayerLoop 항목을 종료한다. 별도 코루틴·이벤트 구독을 만들지 않았다. 단순 GameObject 비활성/화면 숨김은 Scope 소유권 종료와 같다고 가정하지 않는다. 일반 C# Tick에는 MonoBehaviour.enabled 체크박스가 없으므로 이후 명시적 일시정지 정책이 필요하면 별도 승인 범위로 검토한다. 미래 세션 유효성 시스템은 아직 없다.

## 검증

- 최신 Unity 컴파일 실패false·컴파일 중false·최신 콘솔 오류0.
- 실제 씬 Root의 IEnumerable<ITickable> 등록이 정확히1개이며 SimulationTickGlue 타입.
- 생성자에 공급된 SkillUseBrick과 Root에 등록된 객체가 동일.
- 실제 PlayerLoop의 프레임 진행 중 HP45→37→29→21. 기록된 성공시각1000/2001/3003ms, 간격1001/1002ms.
- 같은 성공 시각 재요청은 false, HP·다음 시각 불변.
- Play 메모리에서 Root를 실제 파괴하고, 보관한 적 상태의 HP만 검사 준비로 복원한 뒤1300ms 관찰. Unity 프레임은 계속 진행했지만 HP/NextUseAtMs 불변으로 Scope 종료 후 Tick 미실행을 확인.
- 단독 쿨타임25개 재검사 PASS. 기존 효과21개는 직전 단계에서 검증했으며 이번 두 Brick의 소스는 바꾸지 않았다.
- Scene Missing Script0, 이전 GUID 보존. 소스 검색상 이전 Update/SimulationUpdateGlue/RegisterComponent 경로 없음. 중앙은 RegisterEntryPoint 경로 하나.
- Play 종료 후 Root1개·Scene dirty=false·Missing Script0으로 복구됐고 임시 runInBackground 값을 원복했다. 검사에서 파괴한 것은 Play 메모리 객체이며 최종 씬을 지우지 않았다.

실기기·모든 프레임률·에디터 일시정지 조합·런 재접속은 미검증이다. UI/아트·반격·재생·재출현·스킬큐·서버는 추가하지 않았다. 이전 top of top 수정·커밋·Push·배포 없음.
