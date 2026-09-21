using System;
using UnityEngine;

public enum BgmType
{
    Title,
    Stage,
}

public enum SfxType
{
    UIClick,
    Bomb,
    Death,
    Slime,
    Reward,
}

/// <summary>
/// 종류와 클립을 이어 두는 표. 종류마다 클립은 하나다.
/// 같은 이름의 파일이 여럿 있어도 여기 꽂힌 것만 울린다.
/// </summary>
[CreateAssetMenu(fileName = "SoundCatalog", menuName = "PPS/Sound Catalog")]
public sealed class SoundCatalogSO : ScriptableObject
{
    [Serializable]
    public struct BgmEntry
    {
        public BgmType Type;
        public AudioClip Clip;
    }

    [Serializable]
    public struct SfxEntry
    {
        public SfxType Type;
        public AudioClip Clip;
    }

    [SerializeField] BgmEntry[] bgm;
    [SerializeField] SfxEntry[] sfx;

    public AudioClip Get(BgmType type)
    {
        for (int i = 0; i < bgm.Length; i++)
            if (bgm[i].Type == type) return bgm[i].Clip;

        Debug.LogWarning($"[SoundCatalog] BGM 미등록: {type}", this);
        return null;
    }

    public AudioClip Get(SfxType type)
    {
        for (int i = 0; i < sfx.Length; i++)
            if (sfx[i].Type == type) return sfx[i].Clip;

        Debug.LogWarning($"[SoundCatalog] SFX 미등록: {type}", this);
        return null;
    }
}
