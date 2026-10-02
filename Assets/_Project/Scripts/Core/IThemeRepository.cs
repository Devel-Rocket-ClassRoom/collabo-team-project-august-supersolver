using Cysharp.Threading.Tasks;
using System;

namespace PPS.Core
{
    public interface IThemeRepository
    {
        void Init(IResourceLoader loader);
        event Action OnLoaded;
        ThemeModel Asset { get; }
        UniTask LoadAsync(ThemeLabel theme);
    }
}
