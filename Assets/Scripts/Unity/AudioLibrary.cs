using System.Collections.Generic;
using UnityEngine;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// 音频总管：负责加载音效 / BGM、播放、音量与背景音乐交叉淡入。
    /// 音频文件全部是 tools/gen_audio.py 现场合成的，找不到时会在运行时合成一个提示音兜底。
    /// </summary>
    public class AudioLibrary : MonoBehaviour
    {
        public static AudioLibrary Instance;

        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private AudioSource _musicA;
        private AudioSource _musicB;
        private AudioSource _musicActive;
        private AudioSource[] _sfx;
        private int _sfxCursor;
        private float _musicTarget = 0.55f;
        private float _sfxVolume = 0.8f;
        private bool _musicEnabled = true;
        private bool _sfxEnabled = true;
        private string _currentMusic = "";
        private float _fadeSpeed = 1.4f;

        public string CurrentMusic
        {
            get { return _currentMusic; }
        }

        public static AudioLibrary Create(Transform parent)
        {
            GameObject go = new GameObject("AudioLibrary");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            AudioLibrary lib = go.AddComponent<AudioLibrary>();
            lib.Init();
            return lib;
        }

        public void Init()
        {
            Instance = this;

            _musicA = gameObject.AddComponent<AudioSource>();
            _musicB = gameObject.AddComponent<AudioSource>();
            ConfigureMusicSource(_musicA);
            ConfigureMusicSource(_musicB);
            _musicActive = _musicA;

            _sfx = new AudioSource[10];
            for (int i = 0; i < _sfx.Length; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f;
                src.volume = _sfxVolume;
                _sfx[i] = src;
            }
        }

        private static void ConfigureMusicSource(AudioSource src)
        {
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = 0f;
        }

        private void Update()
        {
            float target = (_musicEnabled && !string.IsNullOrEmpty(_currentMusic)) ? _musicTarget : 0f;
            float step = _fadeSpeed * Time.unscaledDeltaTime;
            for (int i = 0; i < 2; i++)
            {
                AudioSource src = i == 0 ? _musicA : _musicB;
                if (src == null)
                {
                    continue;
                }
                bool active = src == _musicActive;
                float want = active ? target : 0f;
                src.volume = Mathf.MoveTowards(src.volume, want, step);
                if (!active && src.volume <= 0.001f && src.isPlaying)
                {
                    src.Stop();
                }
                src.mute = !_musicEnabled;
            }
        }

        // ------------------------------------------------------------ 播放

        public void PlaySfx(string name)
        {
            PlaySfx(name, 1f, 0.05f);
        }

        public void PlaySfx(string name, float volume, float pitchJitter)
        {
            if (!_sfxEnabled || _sfx == null)
            {
                return;
            }
            AudioClip clip = GetClip("Audio/SFX/" + name);
            if (clip == null)
            {
                return;
            }
            AudioSource src = _sfx[_sfxCursor];
            _sfxCursor = (_sfxCursor + 1) % _sfx.Length;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.volume = Mathf.Clamp01(_sfxVolume * volume);
            src.clip = clip;
            src.Play();
        }

        public void PlayMusic(string name)
        {
            if (_currentMusic == name && _musicActive != null && _musicActive.isPlaying)
            {
                return;
            }
            AudioClip clip = GetClip("Audio/BGM/" + name);
            _currentMusic = name;
            if (clip == null)
            {
                return;
            }
            AudioSource next = _musicActive == _musicA ? _musicB : _musicA;
            next.clip = clip;
            next.volume = 0f;
            next.Play();
            _musicActive = next;
        }

        public void StopMusic()
        {
            _currentMusic = "";
        }

        private AudioClip GetClip(string path)
        {
            if (_clips.ContainsKey(path))
            {
                return _clips[path];
            }
            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                string leaf = path.Substring(path.LastIndexOf('/') + 1);
                if (path.StartsWith("Audio/BGM/"))
                {
                    clip = MakeTone(220f, 2.0f, 0.25f, true);
                }
                else
                {
                    clip = MakeTone(880f, 0.12f, 0.35f, false);
                }
                clip.name = "fallback_" + leaf;
                Debug.LogWarning("[AudioLibrary] 缺少音频 " + path + "，已用合成提示音代替。");
            }
            _clips[path] = clip;
            return clip;
        }

        /// <summary>运行时合成一个简单的正弦音，作为缺失音频的兜底。</summary>
        private static AudioClip MakeTone(float freq, float duration, float amp, bool loopable)
        {
            int sampleRate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(duration * sampleRate));
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float envV = loopable ? 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.5f * t)
                                      : Mathf.Exp(-t * 18f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * amp * envV;
            }
            AudioClip clip = AudioClip.Create("tone", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ------------------------------------------------------------ 设置

        public bool MusicEnabled
        {
            get { return _musicEnabled; }
        }

        public bool SfxEnabled
        {
            get { return _sfxEnabled; }
        }

        public float MusicVolume
        {
            get { return _musicTarget; }
            set { _musicTarget = Mathf.Clamp01(value); }
        }

        public float SfxVolume
        {
            get { return _sfxVolume; }
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                if (_sfx != null)
                {
                    for (int i = 0; i < _sfx.Length; i++)
                    {
                        if (_sfx[i] != null)
                        {
                            _sfx[i].volume = _sfxVolume;
                        }
                    }
                }
            }
        }

        public void ApplySettings(bool musicOn, bool sfxOn, float musicVolume, float sfxVolume)
        {
            _musicEnabled = musicOn;
            _sfxEnabled = sfxOn;
            _musicTarget = Mathf.Clamp01(musicVolume);
            _sfxVolume = Mathf.Clamp01(sfxVolume);
        }
    }
}
