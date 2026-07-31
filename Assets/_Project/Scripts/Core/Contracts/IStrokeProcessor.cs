using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 잉크 파이프라인. 손가락 원본 궤적을 물리에 넣을 최종 <see cref="Stroke"/> 로 만든다.
    /// (단순화, 자기 교차·과소 길이 처리 — 구현은 팀원 B 담당)
    ///
    /// **반드시 시뮬 시작 전에만 호출된다.** 시뮬 중에는 그리기가 잠기므로
    /// 이 인터페이스가 시뮬 루프 안에서 불릴 일이 없고, 따라서 결정론 부담이 없다.
    ///
    /// 역방향(Stroke → 파라미터) 변환은 설계 범위 밖이다 (설계서 결정 5).
    /// </summary>
    public interface IStrokeProcessor
    {
        /// <returns>
        /// 유효한 스트로크를 만들 수 없으면 <see cref="Stroke.IsValid"/> 가 false 인 값을 돌려준다.
        /// (너무 짧은 선 등 — 호출부가 버린다)
        /// </returns>
        Stroke Process(ToolType tool, List<Vector2> rawPoints);
    }
}
