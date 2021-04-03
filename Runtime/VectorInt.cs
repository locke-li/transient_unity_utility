using System;
using static System.Math;
using UnityVector2 = UnityEngine.Vector2;
using UnityVector3 = UnityEngine.Vector3;
using N = System.Int32;
using N2 = System.Int64;
using F = System.Single;

namespace Transient.Mathematical {
    public static class IntVectorParam {
        //int64=19 digits
        //square calculation digit limit = 19 / 2 - 3 = 6
        //cubic calculation digit limit = 19 / 3 - 3 = 3
        public static N Precision { get; } = 1000;
        public static N PrecisionSqr { get; } = Precision * Precision;
        public static F Scaler { get; } = 1f / Precision;
    }

    [Serializable]
    public struct Int2 {
        public N x, y;

        public bool IsZero => x == 0 && y == 0;
        public N2 SqrMagnitude { get { checked { return (N2)x * x + (N2)y * y; } } }

        public Int2(int x_, int y_) {
            x = x_;
            y = y_;
        }

        public Int2(float x_, float y_) {
            checked {
                x = (N)Round(x_ * IntVectorParam.Precision);
                y = (N)Round(y_ * IntVectorParam.Precision);
            }
        }

        public Int2(double x_, double y_) {
            checked {
                x = (N)Round(x_ * IntVectorParam.Precision);
                y = (N)Round(y_ * IntVectorParam.Precision);
            }
        }

        public void Set(N x_, N y_) {
            x = x_;
            y = y_;
        }

        public override string ToString() => $"{x},{y}";

        public static N2 Dot(Int2 a_, Int2 b_) {
            checked {
                return (N2)a_.x * b_.x + (N2)a_.y * b_.y;
            }
        }

        public void SetPerpenticularTo(Int2 r_) {
            //unity coordinate system default
            x = -r_.y;
            y = r_.x;
        }

        public static Int2 TripleCrossDir(Int2 a_, Int2 b_, Int2 c_) {
            //m=axb=(0,0,ax*by-ay*bx)
            //mxc=(my*cz-mz*cy,-mx*cz+mz*cx,mx*cy-my*cx)=(-mz*cy,mz*cx,0)
            //use only the sign of mz to avoid overflow
            checked {
                N2 z = (N2)a_.x * b_.y - (N2)a_.y * b_.x;
                return new Int2(-c_.y, c_.x) * Sign(z);
            }
        }

        public static Int2 Scale(Int2 a_, N p_, N b_) {
            checked {
                return new Int2() {
                    x = (N)((N2)a_.x * p_ / b_),
                    y = (N)((N2)a_.y * p_ / b_)
                };
            }
        }

        public static Int2 Scale(Int2 a_, N2 p_, N2 b_) {
            if (p_ == b_) return new Int2(a_.x, a_.y);
            checked {
                return new Int2() {
                    x = (N)(a_.x * p_ / b_),
                    y = (N)(a_.y * p_ / b_)
                };
            }
        }

        public static Int2 Lerp(Int2 a_, Int2 b_, N2 p_, N2 t_) => a_ + Scale(b_ - a_, p_, t_);
        public static Int2 LerpClamp(Int2 a_, Int2 b_, N2 p_, N2 t_) => Lerp(a_, b_, GenericMath.Clamp(p_, 0, t_), t_);

        public static Int2 operator +(Int2 a_, Int2 b_) => new Int2() { x = a_.x + b_.x, y = a_.y + b_.y };
        public static Int2 operator -(Int2 a_, Int2 b_) => new Int2() { x = a_.x - b_.x, y = a_.y - b_.y };
        public static Int2 operator -(Int2 a_) => new Int2() { x = -a_.x, y = -a_.y };
        public static Int2 operator *(Int2 a_, N s_) { checked { return new Int2() { x = a_.x * s_, y = a_.y * s_ }; } }
        public static Int2 operator *(N s_, Int2 a_) { checked { return new Int2() { x = a_.x * s_, y = a_.y * s_ }; } }
        public static Int2 operator *(Int2 a_, N2 s_) { checked { return new Int2() { x = (N)(a_.x * s_), y = (N)(a_.y * s_) }; } }
        public static Int2 operator *(N2 s_, Int2 a_) { checked { return new Int2() { x = (N)(a_.x * s_), y = (N)(a_.y * s_) }; } }
        public static Int2 operator /(Int2 a_, N s_) => new Int2() { x = a_.x / s_, y = a_.y / s_ };
        public static Int2 operator /(Int2 a_, N2 s_) => new Int2() { x = (N)(a_.x / s_), y = (N)(a_.y / s_) };

        public static bool operator ==(Int2 a_, Int2 b_) => a_.x == b_.x && a_.y == b_.y;
        public static bool operator !=(Int2 a_, Int2 b_) => a_.x != b_.x || a_.y != b_.y;

        public override int GetHashCode() {
            return x.GetHashCode() ^ y.GetHashCode() << 2;
        }

        public override bool Equals(object other) {
            if (other.GetType() != GetType()) {
                return false;
            }
            var v = (Int2)other;
            return x == v.x && y == v.y;
        }

        public static explicit operator UnityVector2(Int2 v_) => new UnityVector2(v_.x * IntVectorParam.Scaler, v_.y * IntVectorParam.Scaler);
        public static explicit operator Float2(Int2 v_) => new Float2(v_.x * IntVectorParam.Scaler, v_.y * IntVectorParam.Scaler);
        public static implicit operator Int3(Int2 v_) => new Int3(v_.x, v_.y, 0);

        public static explicit operator Int2(UnityVector2 v_) => new Int2(v_.x, v_.y);
        public static explicit operator Int2(UnityVector3 v_) => new Int2(v_.x, v_.y);
        public static explicit operator Int2(Float2 v_) => new Int2(v_.x, v_.y);
        public static explicit operator Int2(Float3 v_) => new Int2(v_.x, v_.y);
        public static explicit operator Int2(Int3 v_) => new Int2(v_.x, v_.y);
    }

    [Serializable]
    public struct Int3 {
        public N x, y, z;

        public bool IsZero => x == 0 && y == 0 && z == 0;
        public N2 SqrMagnitude { get { checked { return (N2)x * x + (N2)y * y + (N2)z * z; } } }

        public Int3(N x_, N y_, N z_) {
            x = x_;
            y = y_;
            z = z_;
        }

        public Int3(float x_, float y_, float z_) {
            checked {
                x = (N)Round(x_ * IntVectorParam.Precision);
                y = (N)Round(y_ * IntVectorParam.Precision);
                z = (N)Round(z_ * IntVectorParam.Precision);
            }
        }

        public Int3(double x_, double y_, double z_) {
            checked {
                x = (N)Round(x_ * IntVectorParam.Precision);
                y = (N)Round(y_ * IntVectorParam.Precision);
                z = (N)Round(z_ * IntVectorParam.Precision);
            }
        }

        public void Set(N x_, N y_, N z_) {
            x = x_;
            y = y_;
            z = z_;
        }

        public override string ToString() => $"{x},{y},{z}";

        public static N2 Dot(Int3 a_, Int3 b_) {
            checked {
                return (N2)a_.x * b_.x + (N2)a_.y * b_.y + (N2)a_.z * b_.z;
            }
        }

        //normal to clockwise triangle s0,s1,s2
        public static Int3 TriangleNormal(Int3 s0_, Int3 s1_, Int3 s2_) {
            return Cross(s1_ - s0_, s2_ - s0_);
        }

        public static Int3 Cross(Int3 a_, Int3 b_) {
            checked {
                return new Int3() {
                    x = (N)((N2)a_.y * b_.z - (N2)a_.z * b_.y),
                    y = (N)((N2)a_.z * b_.x - (N2)a_.x * b_.z),
                    z = (N)((N2)a_.x * b_.y - (N2)a_.y * b_.x)
                };
            }
        }

        public static Int3 CrossDir(Int3 a_, Int3 b_) {
            checked {
                return new Int3() {
                    x = (N)((N2)a_.y * b_.z / IntVectorParam.Precision - (N2)a_.z * b_.y / IntVectorParam.Precision),
                    y = (N)((N2)a_.z * b_.x / IntVectorParam.Precision - (N2)a_.x * b_.z / IntVectorParam.Precision),
                    z = (N)((N2)a_.x * b_.y / IntVectorParam.Precision - (N2)a_.y * b_.x / IntVectorParam.Precision)
                };
            }
        }

        public static Int3 TripleCross(Int3 a_, Int3 b_, Int3 c_) => Cross(Cross(a_, b_), c_);

        public static Int3 TripleCrossDir(Int3 a_, Int3 b_, Int3 c_) => CrossDir(CrossDir(a_, b_), c_);

        public static Int3 Scale(Int3 a_, N p_, N b_) {
            return new Int3() {
                x = (N)((N2)a_.x * p_ / b_),
                y = (N)((N2)a_.y * p_ / b_),
                z = (N)((N2)a_.z * p_ / b_),
            };
        }

        public static Int3 Scale(Int3 a_, N2 p_, N2 b_) {
            if (p_ == b_) return new Int3(a_.x, a_.y, a_.z);
            checked {
                return new Int3() {
                    x = (N)(a_.x * p_ / b_),
                    y = (N)(a_.y * p_ / b_),
                    z = (N)(a_.z * p_ / b_)
                };
            }
        }

        public static Int3 Lerp(Int3 a_, Int3 b_, N2 p_, N2 t_) => a_ + Scale(b_ - a_, p_, t_);
        public static Int3 LerpClamp(Int3 a_, Int3 b_, N2 p_, N2 t_) => Lerp(a_, b_, GenericMath.Clamp(p_, 0, t_), t_);

        public static Int3 operator +(Int3 a_, Int3 b_) => new Int3() { x = a_.x + b_.x, y = a_.y + b_.y, z = a_.z + b_.z };
        public static Int3 operator -(Int3 a_, Int3 b_) => new Int3() { x = a_.x - b_.x, y = a_.y - b_.y, z = a_.z - b_.z };
        public static Int3 operator -(Int3 a_) => new Int3() { x = -a_.x, y = -a_.y, z = -a_.z };
        public static Int3 operator *(Int3 a_, N s_) { checked { return new Int3() { x = a_.x * s_, y = a_.y * s_, z = a_.z * s_ }; } }
        public static Int3 operator *(N s_, Int3 a_) { checked { return new Int3() { x = a_.x * s_, y = a_.y * s_, z = a_.z * s_ }; } }
        public static Int3 operator *(Int3 a_, N2 s_) { checked { return new Int3() { x = (N)(a_.x * s_), y = (N)(a_.y * s_), z = (N)(a_.z * s_) }; } }
        public static Int3 operator *(N2 s_, Int3 a_) { checked { return new Int3() { x = (N)(a_.x * s_), y = (N)(a_.y * s_), z = (N)(a_.z * s_) }; } }
        public static Int3 operator /(Int3 a_, N s_) => new Int3() { x = a_.x / s_, y = a_.y / s_, z = a_.z / s_ };
        public static Int3 operator /(Int3 a_, N2 s_) => new Int3() { x = (N)(a_.x / s_), y = (N)(a_.y / s_), z = (N)(a_.z / s_) };

        public static bool operator ==(Int3 a_, Int3 b_) => a_.x == b_.x && a_.y == b_.y && a_.z == b_.z;
        public static bool operator !=(Int3 a_, Int3 b_) => a_.x != b_.x || a_.y != b_.y || a_.z != b_.z;

        public override int GetHashCode() {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        }

        public override bool Equals(object other) {
            if (other.GetType() != GetType()) {
                return false;
            }
            var v = (Float3)other;
            return x == v.x && y == v.y && z == v.z;
        }

        public static explicit operator UnityVector3(Int3 v_) => new UnityVector3(v_.x * IntVectorParam.Scaler, v_.y * IntVectorParam.Scaler, v_.z * IntVectorParam.Scaler);
        public static explicit operator Float3(Int3 v_) => new Float3(v_.x * IntVectorParam.Scaler, v_.y * IntVectorParam.Scaler, v_.z * IntVectorParam.Scaler);
        public static explicit operator Int2(Int3 v_) => new Int2(v_.x, v_.y);

        public static explicit operator Int3(UnityVector2 v_) => new Int3(v_.x, v_.y, 0);
        public static explicit operator Int3(UnityVector3 v_) => new Int3(v_.x, v_.y, v_.z);
        public static explicit operator Int3(Float2 v_) => new Int3(v_.x, v_.y, 0);
        public static explicit operator Int3(Float3 v_) => new Int3(v_.x, v_.y, v_.z);
    }
}
