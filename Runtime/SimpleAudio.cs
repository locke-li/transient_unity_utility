using UnityEngine;
using Transient.SimpleContainer;
using System;
using System.Diagnostics;
using UnityEngine.Audio;

namespace Transient.Audio {
    public sealed class SimpleAudio {
        public static SimpleAudio Default { get; private set; } = new SimpleAudio();

        public static float DefaultVolume = 0.8f;
        private AudioMixer _mixer;
        List<string> groupKey;
        Dictionary<string, AudioSource> channel;
        Dictionary<string, (string name, string channel)> eventList;
        List<AudioSource> copyList;
        public bool Mute { get; set; }
        private static readonly string KeyMute = "audio_mute";
        private static readonly string KeyChannelPrefix = "audio_volume";
        public GameObject Asset { get; private set; }
        public AudioListener Listener { get; set; }
        public Func<string, AudioClip> LoadClip = s => Resources.Load<AudioClip>(s);

        [Conditional("AudioEnabled")]
        public void Init(GameObject root_, AudioMixer mixer_, AudioListener listener_ = null) {
            Performance.RecordProfiler(nameof(SimpleAudio));
            Log.Assert(root_ is object, "root is null");
            Asset = root_;
            if (listener_ == null) {
                var obj = Asset.transform.AddChild("listener");
                Listener = obj.gameObject.AddComponent<AudioListener>();
            }
            else {
                Listener = listener_;
            }
            Reset(mixer_);
            Performance.End(nameof(SimpleAudio));
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

        private void Reset(AudioMixer mixer_) {
            _mixer = mixer_;
            Mute = PlayerPrefs.GetInt(KeyMute, 0) > 0;
#if UNITY_EDITOR
            Mute = UnityEditor.EditorPrefs.GetInt(KeyMute, 1) > 0;
#endif
            eventList = new Dictionary<string, (string, string)>(32);
            copyList = new List<AudioSource>(16);
            groupKey = new List<string>(4);
            channel = new Dictionary<string, AudioSource>(8);
            //init channels
            for (var k = 0; k < 16; ++k) {
                var groupList = mixer_.FindMatchingGroups($"{k}/");
                if (groupList.Length == 0) break;
                var key = $"{KeyChannelPrefix}{k}";
                mixer_.SetFloat(key, PlayerPrefs.GetFloat(key, 0));
                groupKey.Add(key);
                foreach (var g in groupList) {
                    var source = Asset.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.outputAudioMixerGroup = g;
                    channel.Add(g.name, source);
                }
            }
        }

        public void Volume(int group_, float v) {
            if (group_ < 0 || group_ >= groupKey.Count) return;
            var key = groupKey[group_];
            if (!_mixer.SetFloat(key, v < 0 ? 0 : (v * 100 - 80))) {
                Log.Warning($"key {key} not found on mixer");
            }
        }

        public float Volume(int group_) {
            if (group_ < 0 || group_ >= groupKey.Count) return 0;
            var key = groupKey[group_];
            if (!_mixer.GetFloat(key, out var v)) {
                Log.Warning($"key {key} not found on mixer");
            }
            return (v + 80) / 100;
        }

        public void VolumePersist() {
            PlayerPrefs.SetInt(KeyMute, Mute ? 1 : 0);
            foreach(var k in groupKey) {
                if (!_mixer.GetFloat(k, out var v)) {
                    Log.Warning($"key {k} not found on mixer");
                }
                PlayerPrefs.SetFloat(k, v);
            }
        }

        public int VolumeToggle(int group_) {
            //NOTE 0->1, 1->0
            var volume = 1 - Mathf.CeilToInt(Volume(group_));
            //NOTE -(1 - v), so 0 -> -1(default), 1 -> 0
            Volume(group_, -volume);
            return volume;
        }

        public void RegisterEvent(string event_, string clip_, string ch_) {
            eventList[event_] = (clip_, ch_);
        }

        [Conditional("AudioEnabled")]
        public void Event(string event_) {
            if (!string.IsNullOrEmpty(event_) && eventList.TryGetValue(event_, out var info)) {
                Sound(info.name, info.channel);
            }
            else {
                Log.Warning($"audio event failed {event_}");
            }
        }

        private AudioClip GetClip(string name_) {
            AudioClip clip;
            if (string.IsNullOrEmpty(name_) || (clip = LoadClip(name_)) == null) return null;
            return clip;
        }

        private AudioSource Channel(string name_) {
            if (string.IsNullOrEmpty(name_) || !channel.TryGetValue(name_, out var ch)) return null;
            return ch;
        }

        private AudioSource ChannelCopy(AudioSource from_, float distanceMin_, float distanceMax_) {
            AudioSource copy = null;
            foreach (var s in copyList) {
                if (!s.isPlaying) {
                    copy = s;
                    break;
                }
            }
            if (copy == null) {
                var obj = Asset.transform.AddChild("copy");
                copy = obj.gameObject.AddComponent<AudioSource>();
                DynamicSource(copy);
                copyList.Add(copy);
            }
            copy.outputAudioMixerGroup = from_.outputAudioMixerGroup;
            copy.volume = from_.volume;
            copy.minDistance = distanceMin_;
            copy.maxDistance = distanceMax_;
            return copy;
        }

        public void DynamicSource(AudioSource source_) {
            source_.playOnAwake = false;
            source_.spatialBlend = 0.95f;
            source_.rolloffMode = AudioRolloffMode.Linear;
        }

        public void OutputGroup(AudioSource source_, string ch_) {
            var ch = Channel(ch_);
            source_.volume = ch.volume;
            source_.outputAudioMixerGroup = ch.outputAudioMixerGroup;
        }

        [Conditional("AudioEnabled")]
        public void Play(string name_, string ch_, bool loop_) {
            AudioClip clip = GetClip(name_);
            AudioSource ch = Channel(ch_);
            if (clip == null || ch == null) {
                Log.Warning($"failed to play {name_}:{clip} {ch_}:{ch}");
                return;
            }
            Play(clip, ch, loop_);
        }

        [Conditional("AudioEnabled")]
        public void Play(string name_, AudioSource ch_, bool loop_) {
            AudioClip clip = GetClip(name_);
            if (clip == null || ch_ == null) {
                Log.Warning($"failed to play {name_}:{clip} {ch_}:{ch_}");
                return;
            }
            Play(clip, ch_, loop_);
        }

        [Conditional("AudioEnabled")]
        public void Play(AudioClip clip, AudioSource ch, bool loop) {
            if (clip == null || ch == null || Mute) return;
            ch.loop = loop;
            ch.clip = clip;
            ch.Play();
        }

        [Conditional("AudioEnabled")]
        public void Sound(string name_, string ch_) {
            AudioClip clip = GetClip(name_);
            AudioSource ch = Channel(ch_);
            if (clip == null || ch == null) {
                Log.Warning($"failed to sound {name_}:{clip} {ch_}:{ch}");
                return;
            }
            Sound(clip, ch);
        }

        [Conditional("AudioEnabled")]
        public void Sound(string name_, AudioSource ch_) {
            AudioClip clip = GetClip(name_);
            if (clip == null || ch_ == null) {
                Log.Warning($"failed to sound {name_}:{clip} {ch_}:{ch_}");
                return;
            }
            Sound(clip, ch_);
        }

        [Conditional("AudioEnabled")]
        public void SoundAt(string name_, string ch_, Vector3 target_, float min_, float max_) {
            AudioClip clip = GetClip(name_);
            AudioSource ch = Channel(ch_);
            if (clip == null || ch == null) {
                Log.Warning($"failed to sound {name_}:{clip} {ch_}:{ch}");
                return;
            }
            if ((target_ - Listener.transform.position).sqrMagnitude > max_ * max_) return;
            ch = ChannelCopy(ch, min_, max_);
            ch.transform.position = target_;
            Sound(clip, ch);
        }

        [Conditional("AudioEnabled")]
        public void Sound(AudioClip clip, AudioSource ch) {
            if (clip == null || ch == null || Mute) return;
            ch.PlayOneShot(clip);
        }

        [Conditional("AudioEnabled")]
        public void SkipTo(string ch, float time_) => SkipTo(Channel(ch), time_);

        [Conditional("AudioEnabled")]
        public void SkipTo(AudioSource ch, float time_) {
            if (ch == null || Mute) return;
            ch.time = time_;
        }
    }
}