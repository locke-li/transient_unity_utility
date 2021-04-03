using System;
using UnityVector2 = UnityEngine.Vector2;
using UnityVector3 = UnityEngine.Vector3;
using UnityVector4 = UnityEngine.Vector4;

namespace Transient.Mathematical {
    [Serializable]
    public struct Float2 {
        public float x;
        public float y;

        public bool IsZero => x == 0 && y == 0;
        public float SqrMagnitude => x * x + y * y;
        public float Magnitude => (float)Math.Sqrt(x * x + y * y);
        public Float2 Normalized {
            get {
                var m = Magnitude;
                return new Float2(x / m, y / m);
            }
        }

        public Float2(float x_, float y_) {
            x = x_;
            y = y_;
        }

        public void Set(float x_, float y_) {
            x = x_;
            y = y_;
        }

        public override string ToString() => $"{x},{y}";

        public static float Dot(Float2 a_, Float2 b_) => a_.x * b_.x + a_.y * b_.y;

        public void SetPerpenticularTo(Float2 r_) {
            //unity coordinate system default
            x = -r_.y;
            y = r_.x;
        }

        public static Float2 TripleCrossDir(Float2 a_, Float2 b_, Float2 c_) {
            //m=axb=(0,0,ax*by-ay*bx)
            //mxc=(my*cz-mz*cy,-mx*cz+mz*cx,mx*cy-my*cx)=(-mz*cy,mz*cx,0)
            var z = a_.x * b_.y - a_.y * b_.x;
            return new Float2(-c_.y * z, c_.x * z);
        }

        public static Float2 Scale(Float2 a_, float p_, float b_) {
            var f = p_ / b_;
            return new Float2(a_.x * f, a_.y * f);
        }

        public static Float2 Lerp(Float2 a_, Float2 b_, float p_, float t_) {
            var f = p_ / t_;
            return new Float2(a_.x * (1-f) + b_.x * f, a_.y * (1-f) + b_.y * f);
        }

        public static Float2 LerpClamp(Float2 a_, Float2 b_, float p_, float t_) {
            var f = GenericMath.Clamp(p_ / t_, 0, 1);
            return new Float2(a_.x * (1 - f) + b_.x * f, a_.y * (1 - f) + b_.y * f);
        }

        public static Float2 operator +(Float2 a_, Float2 b_) => new Float2() { x = a_.x + b_.x, y = a_.y + b_.y };
        public static Float2 operator -(Float2 a_, Float2 b_) => new Float2() { x = a_.x - b_.x, y = a_.y - b_.y };
        public static Float2 operator -(Float2 a_) => new Float2() { x = -a_.x, y = -a_.y };
        public static Float2 operator *(Float2 a_, float s_) => new Float2() { x = a_.x * s_, y = a_.y * s_ };
        public static Float2 operator *(float s_, Float2 a_) => new Float2() { x = a_.x * s_, y = a_.y * s_ };
        public static Float2 operator /(Float2 a_, float s_) => new Float2() { x = a_.x / s_, y = a_.y / s_ };
        public static Float2 operator *(Float2 a_, int s_) => new Float2() { x = a_.x * s_, y = a_.y * s_ };
        public static Float2 operator *(int s_, Float2 a_) => new Float2() { x = a_.x * s_, y = a_.y * s_ };
        public static Float2 operator /(Float2 a_, int s_) => new Float2() { x = a_.x / s_, y = a_.y / s_ };

        public static bool operator ==(Float2 a_, Float2 b_) => a_.x == b_.x && a_.y == b_.y;
        public static bool operator !=(Float2 a_, Float2 b_) => a_.x != b_.x || a_.y != b_.y;

        public override int GetHashCode() {
            return x.GetHashCode() ^ y.GetHashCode() << 2;
        }

        public override bool Equals(object other) {
            if(other.GetType() != GetType()) {
                return false;
            }
            var v = (Float2)other;
            return x == v.x && y == v.y;
        }

        public static implicit operator UnityVector2(Float2 v_) => new UnityVector2(v_.x, v_.y);
        public static implicit operator UnityVector3(Float2 v_) => new UnityVector3(v_.x, v_.y);

        public static implicit operator Float2(UnityVector2 v_) => new Float2(v_.x, v_.y);
        public static explicit operator Float2(UnityVector3 v_) => new Float2(v_.x, v_.y);
        public static explicit operator Float2(Float3 v_) => new Float2(v_.x, v_.y);
    }

    [Serializable]
    public struct Float3 {
        public float x;
        public float y;
        public float z;

        public bool IsZero => x == 0 && y == 0 && z == 0;
        public float SqrMagnitude => x * x + y * y + z* z;
        public float Magnitude => (float)Math.Sqrt(x * x + y * y + z * z);
        public Float3 Normalized {
            get {
                var m = Magnitude;
                return new Float3(x / m, y / m, z / m);
            }
        }

        public Float3(float x_, float y_, float z_) {
            x = x_;
            y = y_;
            z = z_;
        }

        public void Set(float x_, float y_, float z_) {
            x = x_;
            y = y_;
            z = z_;
        }

        public override string ToString() => $"{x},{y},{z}";

        public static float Dot(Float3 a_, Float3 b_) => a_.x * b_.x + a_.y * b_.y + a_.z * b_.z;

        //normal to clockwise triangle s0,s1,s2
        public static Float3 TriangleNormal(Float3 s0_, Float3 s1_, Float3 s2_) => Cross(s1_-s0_, s2_-s0_);

        public static Float3 Cross(Float3 a_, Float3 b_) => new Float3(a_.y * b_.z - a_.z * b_.y, a_.z * b_.x - a_.x * b_.z, a_.x * b_.y - a_.y * b_.x);

        public static Float3 CrossDir(Float3 a_, Float3 b_) => Cross(a_, b_);

        public static Float3 TripleCross(Float3 a_, Float3 b_, Float3 c_) => Cross(Cross(a_, b_), c_);

        public static Float3 TripleCrossDir(Float3 a_, Float3 b_, Float3 c_) => TripleCross(a_, b_, c_);

        public static Float3 Scale(Float3 a_, float p_, float b_) {
            var f = p_ / b_;
            return new Float3(a_.x * f, a_.y * f, a_.z * f);
        }

        public static Float3 Lerp(Float3 a_, Float3 b_, float p_, float t_) {
            var f = p_ / t_;
            var k = 1-f;
            return new Float3(a_.x * k + b_.x * f, a_.y * k + b_.y * f, a_.z * k + b_.z * f);
        }

        public static Float3 LerpClamp(Float3 a_, Float3 b_, float p_, float t_) {
            var f = GenericMath.Clamp(p_ / t_, 0, 1);
            var k = 1 - f;
            return new Float3(a_.x * k + b_.x * f, a_.y * k + b_.y * f, a_.z * k + b_.z * f);
        }

        public static Float3 operator +(Float3 a_, Float3 b_) => new Float3() { x = a_.x + b_.x, y = a_.y + b_.y, z = a_.z + b_.z };
        public static Float3 operator -(Float3 a_, Float3 b_) => new Float3() { x = a_.x - b_.x, y = a_.y - b_.y, z = a_.z - b_.z };
        public static Float3 operator -(Float3 a_) => new Float3() { x = -a_.x, y = -a_.y, z= -a_.z };
        public static Float3 operator *(Float3 a_, float s_) => new Float3() { x = a_.x * s_, y = a_.y * s_, z= a_.z * s_ };
        public static Float3 operator *(float s_, Float3 a_) => new Float3() { x = a_.x * s_, y = a_.y * s_, z= a_.z * s_ };
        public static Float3 operator /(Float3 a_, float s_) => new Float3() { x = a_.x / s_, y = a_.y / s_, z= a_.z / s_ };
        public static Float3 operator *(Float3 a_, int s_) => new Float3() { x = a_.x * s_, y = a_.y * s_, z= a_.z * s_ };
        public static Float3 operator *(int s_, Float3 a_) => new Float3() { x = a_.x * s_, y = a_.y * s_, z= a_.z * s_ };
        public static Float3 operator /(Float3 a_, int s_) => new Float3() { x = a_.x / s_, y = a_.y / s_, z= a_.z / s_ };

        public static bool operator ==(Float3 a_, Float3 b_) => a_.x == b_.x && a_.y == b_.y && a_.z == b_.z;
        public static bool operator !=(Float3 a_, Float3 b_) => a_.x != b_.x || a_.y != b_.y || a_.z != b_.z;

        public override int GetHashCode() {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        }

        public override bool Equals(object other) {
            if(other.GetType() != GetType()) {
                return false;
            }
            var v = (Float3)other;
            return x == v.x && y == v.y && z == v.z;
        }

        public static implicit operator UnityVector3(Float3 v_) => new UnityVector3(v_.x, v_.y, v_.z);

        public static implicit operator Float3(UnityVector2 v_) => new Float3(v_.x, v_.y, 0);
        public static implicit operator Float3(UnityVector3 v_) => new Float3(v_.x, v_.y, v_.z);
        public static implicit operator Float3(Float2 v_) => new Float3(v_.x, v_.y, 0);
    }

    public struct Float4 {
        public float x, y, z, w;

        public Float4(float x_, float y_, float z_, float w_) {
            x = x_;
            y = y_;
            z = z_;
            w = w_;
        }

        public override int GetHashCode() {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode();
        }

        public override bool Equals(object other) {
            if(other.GetType() != GetType()) {
                return false;
            }
            var v = (Float4)other;
            return x == v.x && y == v.y && z == v.z && w == v.w;
        }

        public static implicit operator Float4(UnityVector4 v_) => new Float4(v_.x, v_.y, v_.z, v_.w);
    }
}
