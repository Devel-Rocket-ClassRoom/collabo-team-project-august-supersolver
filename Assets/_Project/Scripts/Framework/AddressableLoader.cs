using Cysharp.Threading.Tasks;
using PPS.Core;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AddressableLoader : MonoBehaviour, IResourceLoader
{
    public event Action BeforeLoad;
    public event Action BeforeUnLoad;
    public event Action AfterUnLoad;
    public event Action AfterLoad;

    // key 는 라벨이다. 그룹은 런타임에 조회할 수 없어
    // 그룹 단위로 묶으려면 같은 라벨을 달아야 한다.

    private void Awake()
    {
        ServiceLocator.Register<IResourceLoader>(this);
    }
    public async UniTask<IResourceHandle> LoadAsync(string key)
    {
        BeforeLoad?.Invoke();
        var operation = Addressables.LoadAssetsAsync<UnityEngine.Object>(key, null);
        await operation.Task;

        AfterLoad?.Invoke();
        return new AddressableHandle(operation);
    }
    public UniTask Unload(IResourceHandle handle)
    {
        BeforeUnLoad?.Invoke();
        Addressables.Release(((AddressableHandle)handle).Operation);
        AfterUnLoad?.Invoke();

        // Release 는 동기라 기다릴 것이 없다.
        return UniTask.CompletedTask;
    }
}
