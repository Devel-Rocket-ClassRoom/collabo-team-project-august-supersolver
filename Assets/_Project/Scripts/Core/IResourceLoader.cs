using Cysharp.Threading.Tasks;
using System;

namespace PPS.Core
{
    // 리소스를 불러오는 방식의 공통 규칙이다.
    // 로드 세 단계는 항상 순서대로 호출된다.
    public interface IResourceLoader
    {
        public event Action BeforeUnLoad;

        public event Action AfterUnLoad;

        public event Action BeforeLoad;

        public event Action AfterLoad;
        UniTask<IResourceHandle> LoadAsync(string key);
        UniTask Unload(IResourceHandle handle);
    }
}
