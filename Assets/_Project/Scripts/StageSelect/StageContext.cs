using Cysharp.Threading.Tasks;
using PPS.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StageContext : MonoSingleton<StageContext>
{
    const int StagePerTheme = 20;
    public int StageIndex { get; private set; }
    public IReadOnlyList<StageData> Stages => _cached;

    private string[] labels = new string[]
    {
        "Korea",
        "Japan"
    };
    private int _cachedLabelIndex;
    private List<StageData> _cached = new();

    private IResourceHandle _handle;
    
    public async UniTask LoadStageAsync(int stageIndex)
    {
        int labelIndex = stageIndex / StagePerTheme;
        if (labelIndex < 0 || labelIndex > labels.Length - 1) return;

        if (_handle != null && _cachedLabelIndex == labelIndex) return;

        if(ServiceLocator.TryGet<IResourceLoader>(out var service))
        {
            if (_handle != null)
            {
                await service.Unload(_handle);
                _handle = null;
            }

            _cachedLabelIndex = labelIndex;
            _handle = await service.LoadAsync(labels[_cachedLabelIndex]);

            _cached = _handle.Assets
                .OfType<TextAsset>()
                .Select(ta => StageData.FromJson(ta.text))
                .ToList();
        }
        StageIndex = stageIndex;
    }
}
