using UnityEngine;
using Transient.SimpleContainer;
using System;
using System.Diagnostics;

namespace Transient.Audio {
    public sealed class SimpleAudio {
        class AudioRef {
            public AudioSource[] source;
            public float[] progress;

            private AudioRef() {}
            public AudioRef(float volume_, params AudioSource[] source_) {
                source = source_;
                for (int p = 0; p < source_.Length; ++p) {
                    source_[p].volume = volume_;
                }
                progress = new float[source_.Length];
            }

            public void Timestep(float dt) {
                for (int j = 0; j < progress.Length; ++j) {
                    if (progress[j] > 0) {
                        progress[j] -= dt;
                    }
                }
            }

            public void Play(AudioClip clip_, bool loop_) {
                source[0].loop = loop_;
                source[0].clip = clip_;
                source[0].Play();
            }

            public void Sound(AudioClip clip_) {
                for (int l = 0; l < source.Length; ++l) {
                    if (progress[l] <= 0) {
                        source[l].PlayOneShot(clip_);
                    }
                }
            }

            public void SkipTo(float time_) {
                source[0].time = time_;
            }
        }

        public static SimpleAudio Default { get; private set; } = new SimpleAudio();

        public const float DefaultVolume = 0.5f;
        List<AudioRef> channel;
        Dictionary<string, (string name, int channel)> eventList;
        public bool mute { get; set; }
        public static readonly string KeyMute = "audiomute";
        public static readonly string KeyChannelVolume0 = "channelvolume0";
        public static readonly string KeyChannelVolume1 = "channelvolume1";
        public GameObject Asset { get; private set; }
        public Func<string, AudioClip> LoadClip = s => Resources.Load<AudioClip>(s);

        [Conditional("AudioEnabled")]
        public void Init(GameObject root_) {
            Log.Assert(root_ is object, "root is null");
            Asset = root_;
            Reset();
            MainLoop.OnUpdate.Add(Update, this);
        }

#if UNITY_EDITOR
        const string MenuPath = "DevShortcut/Audio Enabled";

        [UnityEditor.MenuItem(MenuPath, priority = 1000)]
        private static void ToggleAudio() {
            var value = UnityEditor.EditorPrefs.GetInt(KeyMute, 0);
            UnityEditor.EditorPrefs.SetInt(KeyMute, 1 - value);
        }

        [UnityEditor.MenuItem(MenuPath, priority = 1000, validate = true)]
        private static bool ToggleAudioValidate() {
            UnityEditor.Menu.SetChecked(MenuPath, UnityEditor.EditorPrefs.GetInt(KeyMute, 0) == 0);
            return true;
        }
#endif

        private void Reset() {
            mute = PlayerPrefs.GetInt(KeyMute, 0) > 0;
#if UNITY_EDITOR
            mute = UnityEditor.EditorPrefs.GetInt(KeyMute, 1) > 0;
#endif
            eventList = new Dictionary<string, (string, int)>(32);
            channel = new List<AudioRef>(2);
            channel.Add(new AudioRef(
                PlayerPrefs.GetFloat(KeyChannelVolume0, DefaultVolume),
                Asset.AddComponent<AudioSource>()
            ));
            channel.Add(new AudioRef(
                PlayerPrefs.GetFloat(KeyChannelVolume1, DefaultVolume),
                Asset.AddComponent<AudioSource>(),
                Asset.AddComponent<AudioSource>()
            ));
        }

        private void Update(float deltaTime_) {
            foreach(var chan in channel) {
                chan.Timestep(deltaTime_);
            }
        }

        [Conditional("AudioEnabled")]
        public void Volume(int ch, float v) {
            if (ch < 0 || ch > channel.Count) return;
            foreach (var source in channel[ch].source) {
                source.volume = v;
            }
        }

        [Conditional("AudioEnabled")]
        public void VolumePersist() {
            PlayerPrefs.SetInt(KeyMute, mute ? 1 : 0);
            PlayerPrefs.SetFloat(KeyChannelVolume0, Volume(0));
            PlayerPrefs.SetFloat(KeyChannelVolume1, Volume(1));
        }

        public float Volume(int ch) {
            return channel is null ? 0 : channel[ch].source[0].volume;
        }

        public void RegisterEvent(string event_, string clip_, int ch_) {
            eventList[event_] = (clip_, ch_);
        }

        [Conditional("AudioEnabled")]
        public void Event(string event_) {
            if (eventList.TryGetValue(event_, out var info)) {
                SoundAt(info.name, info.channel);
            }
        }

        [Conditional("AudioEnabled")]
        public void PlayAt(string clipName, int ch, bool loop) {
            if (string.IsNullOrEmpty(clipName) || mute || ch < 0 || ch > channel.Count) return;
            var clip = LoadClip(clipName);
            if (clip == null) return;
            channel[ch].Play(clip, loop);
        }

        [Conditional("AudioEnabled")]
        public void PlayAt(AudioClip clip, int ch, bool loop) {
            if (mute || ch < 0 || ch > channel.Count) return;
            channel[ch].Play(clip, loop);
        }

        [Conditional("AudioEnabled")]
        public void SoundAt(string clipName, int ch) {
            if (string.IsNullOrEmpty(clipName) || mute || ch < 0 || ch > channel.Count) return;
            var clip = LoadClip(clipName);
            if (clip == null) return;
            channel[ch].Sound(clip);
        }

        [Conditional("AudioEnabled")]
        public void SoundAt(AudioClip clip, int ch) {
            if (mute || ch < 0 || ch > channel.Count) return;
            channel[ch].Sound(clip);
        }

        [Conditional("AudioEnabled")]
        public void SkipTo(int ch, float p) {
            channel[ch].SkipTo(p);
        }
    }
}