using System.Collections.Generic;
using PPS.Core;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

// Addressables 핸들을 Core 가 아는 타입으로 감싼다.
public class AddressableHandle : IResourceHandle
{
    public AsyncOperationHandle<IList<Object>> Operation { get; }

    public IReadOnlyList<Object> Assets { get; }

    public AddressableHandle(AsyncOperationHandle<IList<Object>> operation)
    {
        Operation = operation;

        // IList 는 IReadOnlyList 가 아니라 캐스팅이 안 된다.
        // 목록만 복사하며, 에셋 참조 자체는 그대로다.
        Assets = new List<Object>(operation.Result);
    }
}
