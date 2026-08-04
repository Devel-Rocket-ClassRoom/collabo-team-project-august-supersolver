using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 잉크 파이프라인. 원본 궤적을
    /// 물리에 넣을 Stroke 로 만든다.
    /// 시뮬 시작 전에만 호출된다.
    /// </summary>
    public interface IStrokeProcessor
    {
        /// <returns>못 만들면 IsValid 가 false 인 값.</returns>
        Stroke Process(ToolType tool, List<Vector2> rawPoints);
    }
}
