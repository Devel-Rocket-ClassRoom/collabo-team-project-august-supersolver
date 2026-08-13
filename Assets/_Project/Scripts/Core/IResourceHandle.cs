using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    // 로드된 리소스 묶음을 가리키는 표식이다.
    // 소비자는 에셋을 꺼내 쓰고, 다 쓰면
    // 이 핸들을 그대로 Unload 에 돌려준다.
    public interface IResourceHandle
    {
        // 이 핸들로 로드된 에셋 전부.
        IReadOnlyList<Object> Assets { get; }
    }
}
