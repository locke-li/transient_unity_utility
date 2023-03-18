using UnityEngine;

namespace Transient {
    public abstract class AbstractCoordinateOffset {
        public abstract float Value { get; set; }
        public abstract void Init(Transform tr_, float v_);
        public abstract Vector2 CalculateRelative(float v_);
        public Vector2 Calculate(float v_) => CalculateRelative(v_ - Value);
        public abstract Vector2 Calculate(Vector3 p_);
        public abstract Vector2 Apply(Vector2 p_, float v_);
        public abstract Vector2 Remove(Vector2 p_, float v_);
    }

    public class OffsetNone : AbstractCoordinateOffset {
        public override float Value { get; set; } = 0;
        public override void Init(Transform tr_, float v_) { }
        public override Vector2 CalculateRelative(float v_) => Vector2.zero;
        public override Vector2 Calculate(Vector3 p_) => Vector2.zero;
        public override Vector2 Apply(Vector2 pos, float v_) => pos;
        public override Vector2 Remove(Vector2 pos, float v_) => pos;
    }

    public class OffsetByYRelative : AbstractCoordinateOffset {
        public override float Value { get => height; set => height = value; }
        public float AngleDegree { set => InitAngle(value * Mathf.Deg2Rad); }
        public float AngleRadian { set => InitAngle(value); }
        public float angleFactor;
        public float height;

        public override void Init(Transform tr_, float v_) {
            height = v_;
            AngleDegree = tr_.rotation.eulerAngles.x;
        }

        public void InitAngle(float rad_) {
            angleFactor = 1 / Mathf.Tan(rad_);
        }

        public override Vector2 CalculateRelative(float v_)
            => angleFactor == 0 ? Vector2.zero : new Vector2(0f, v_ * angleFactor);
        public override Vector2 Calculate(Vector3 p_) => CalculateRelative(p_.y - Value);

        public override Vector2 Apply(Vector2 pos, float v_)
            => pos + Calculate(v_);
        public override Vector2 Remove(Vector2 pos, float v_)
            => pos - Calculate(v_);
    }
}
