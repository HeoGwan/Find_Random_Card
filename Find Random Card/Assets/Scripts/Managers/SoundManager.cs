using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public enum AUDIO
{
    BGM,
    EFFECT,
}

public class SoundManager : MonoBehaviour
{
    // Audio Sources
    [SerializeField] private AudioSource[]  _audioSources; // 0: BGM, 1: Effect

    [Space(10)]
    [Header("BGM Audio Clips")]
    [SerializeField] private AudioClip[]    _startMusics;
    [SerializeField] private AudioClip      _tutorialMusic;
    [SerializeField] private AudioClip      _previewMusic;
    [SerializeField] private AudioClip[]    _gameMusics;

    [Space(10)]
    [Header("Effect Audio Clips")]
    [SerializeField] private AudioClip      _findCard;
    [SerializeField] private AudioClip      _buttonPopSound;
    [SerializeField] private AudioClip      _buttonPopSoundDown;
    [SerializeField] private AudioClip      _cardButtonSound;

    [Space(10)]
    [Header("Card AudioSources")]
    [SerializeField] private AudioSource[]  _cardAudioSources;

    private Dictionary<string, AudioClip>   _audios;

    // Getter
    public AudioSource[] AudioSources { get { return _audioSources; } }

    private void Awake()
    {
        _audios = new Dictionary<string, AudioClip>();

        _audios.Add("FindCard", _findCard);
        _audios.Add("ButtonPopSound", _buttonPopSound);
        _audios.Add("ButtonPopSoundDown", _buttonPopSoundDown);
        _audios.Add("CardButton", _cardButtonSound);

        AudioClip startMusic = _startMusics[Random.Range(0, _startMusics.Length)];
        _audios.Add("StartMusic", startMusic);
        _audios.Add("TutorialMusic", _tutorialMusic);
        _audios.Add("PreviewMusic", _previewMusic);
        
    }

    void RandomGameMusic()
    {
        AudioClip gameMusic = _gameMusics[Random.Range(0, _gameMusics.Length)];
        _audios["GameMusic"] = gameMusic;
    }

    /// <summary>
    /// ȿ���� ����: FindCard, ButtonPopSound, ButtonPopSoundDown, CardButton <br></br>
    /// ����� ����: StartMusic, TutorialMusic, PreviewMusic, GameMusic
    /// </summary>
    /// <param name="isEffect">ȿ�����̸� true, ������̸� false�� �Է��ϼ���</param>
    /// <param name="sound">����� ȿ���� �Ǵ� ������� �̸��� �Է��ϼ���</param>
    public void Play(bool isEffect, string sound, float pitch=1.0f)
    {
        if (isEffect)
        {
            _audioSources[(int)AUDIO.EFFECT].clip = _audios[sound];
            _audioSources[(int)AUDIO.EFFECT].pitch = pitch;
            _audioSources[(int)AUDIO.EFFECT].Play();
        }
        else
        {
            if (sound == "GameMusic") { RandomGameMusic(); }

            _audioSources[(int)AUDIO.BGM].clip = _audios[sound];
            _audioSources[(int)AUDIO.BGM].Play();
        }
    }

    /// <summary>
    /// ȿ������ ��� ��
    /// </summary>
    /// <param name="sound">����: FindCard, ButtonPopSound, ButtonPopSoundDown, CardButton</param>
    public void PlayEffectSound(string sound)
    {
        _audioSources[(int)AUDIO.EFFECT].clip = _audios[sound];
        _audioSources[(int)AUDIO.EFFECT].Play();
    }

    /// <summary>
    /// ����� �Ǵ� ȿ������ ������ �����Ѵ�
    /// </summary>
    /// <param name="audioType">�Ҹ� ����: BGM, EFFECT</param>
    public void ChangeVolume(AUDIO audioType, float volume)
    {
        _audioSources[(int)audioType].volume = volume;

        if (audioType == AUDIO.EFFECT)
        {
            foreach (AudioSource cardAudioSource in _cardAudioSources)
            {
                cardAudioSource.volume = volume;
            }
        }
    }
}
