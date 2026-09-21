using PPS.Core;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 카탈로그에 꽂힌 클립을 울린다. BGM 은 한 줄기라
/// 전용 소스를 두고, SFX 는 겹쳐 울려야 해 자식으로
/// 만들어 둔 소스 풀을 돌려 쓴다.
/// </summary>
public class SoundManager : MonoSingleton<SoundManager>, ISoundManager
{
    [SerializeField] SoundCatalogSO catalog;
    [SerializeField] AudioSource bgmSource;

    [Header("SFX Pool")]
    [SerializeField] int sfxSourceCount = 8;
    [SerializeField] AudioMixerGroup sfxOutput;

    AudioSource[] _sfxSources;

    /// 다음에 빼앗을 자리. 전부 울리는 중일 때만 쓴다.
    int _cursor;

    protected override void Awake()
    {
        base.Awake();

        // 중복 인스턴스는 base 가 지운다. 지워질 것에
        // 풀을 달아 봐야 같이 버려진다.
        if (Instance != this) return;

        // 어셈블리가 갈린 뷰는 이 타입을 못 본다.
        // 계약만 걸어 두고 자신을 꽂는다.
        ServiceLocator.Register<ISoundManager>(this);

        _sfxSources = new AudioSource[sfxSourceCount];
        for (int i = 0; i < sfxSourceCount; i++)
            _sfxSources[i] = CreateSfxSource(i);
    }

    AudioSource CreateSfxSource(int index)
    {
        var go = new GameObject($"SfxSource_{index}");
        go.transform.SetParent(transform, false);

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.outputAudioMixerGroup = sfxOutput;

        return source;
    }

    public void PlayBgm(BgmType type)
    {
        AudioClip clip = catalog.Get(type);
        if (clip == null || bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PlaySfx(SfxType type)
    {
        AudioClip clip = catalog.Get(type);
        if (clip == null) return;

        Rent().PlayOneShot(clip);
    }

    /// <summary>
    /// 노는 소스를 준다. 전부 울리는 중이면 가장 오래
    /// 전에 빼앗은 자리를 다시 빼앗는다 — 소리를 버리는
    /// 것보다 앞선 소리를 끊는 쪽이 덜 이상하다.
    /// </summary>
    AudioSource Rent()
    {
        for (int i = 0; i < _sfxSources.Length; i++)
            if (!_sfxSources[i].isPlaying) return _sfxSources[i];

        AudioSource stolen = _sfxSources[_cursor];
        _cursor = (_cursor + 1) % _sfxSources.Length;

        stolen.Stop();
        return stolen;
    }
}
