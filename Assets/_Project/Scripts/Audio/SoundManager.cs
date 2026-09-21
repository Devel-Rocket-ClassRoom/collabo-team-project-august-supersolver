using UnityEngine;

/// <summary>
/// 카탈로그에 꽂힌 클립을 울린다. BGM 은 한 줄기라
/// 전용 소스를 두고, SFX 는 겹쳐 울려야 해 OneShot 이다.
/// </summary>
public class SoundManager : MonoSingleton<SoundManager>
{
    [SerializeField] SoundCatalogSO catalog;
    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioSource sfxSource;

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

        sfxSource.PlayOneShot(clip);
    }
}
