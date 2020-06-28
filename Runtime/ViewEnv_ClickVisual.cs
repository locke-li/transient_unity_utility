using UnityEngine;
using Transient.DataAccess;
using System;

namespace Transient {
    public class ClickVisual {
        private ParticleSystem _clickVisual;
        private Action<ParticleSystem, Vector3, float> Emit;

        public static ClickVisual TryCreate(string asset_, Vector3 pos_, Action<ParticleSystem, Vector3, float> Emit_) {
            try {
                var obj = AssetMapping.View.TakePersistent<GameObject>(null, asset_);
                if (obj is null || Emit_ is null) return null;
                var ps = obj.GetComponent<ParticleSystem>();
                if (ps == null) {
                    GameObject.Destroy(obj);
                    return null;
                }
                obj.transform.position = pos_;
                return new ClickVisual() {
                    _clickVisual = ps,
                    Emit = Emit_,
                };
            }
            catch(Exception e) {
                Log.Warning($"{nameof(ClickVisual)} create failed. {e.Message}");
            }
            return null;
        }

        public void EmitAt(Vector3 pos_, float scale_) {
            Emit(_clickVisual, pos_, scale_);
        }
    }
}