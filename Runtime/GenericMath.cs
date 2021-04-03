using UnityEngine;
using System;
using static System.Math;
using UnityVector2 = UnityEngine.Vector2;

namespace Transient.Mathematical {
    public static class GenericMath {

        #region value comparison

        public static float Clamp(float v, float min, float max) {
            return v<min ? min : (v>max ? max : v);
        }

        public static int Clamp(int v, int min, int max) {
            return v<min ? min : (v>max ? max : v);
        }

        public static Int64 Clamp(Int64 v, Int64 min, Int64 max) {
            return v < min ? min : (v > max ? max : v);
        }

        #endregion

        //http://graphics.stanford.edu/~seander/bithacks.html#InterleaveBMN
        //Round up to the next highest power of 2 
        public static uint NextPowerOfTwo(uint v) {
            --v;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            return ++v;
        }

        #region trigonometric approximation table

        //https://en.wikipedia.org/wiki/Small-angle_approximation
        const float tdenom = 0.19634954f;//32 slices,0.19634954f=2*pi/32 radian gap
        const float ivtdenom = 1f / tdenom;//5.092958
        static readonly float[] sinapprox = new float[32 + 8] {
            0, 0.1950903f, 0.3826835f, 0.5555702f, 0.7071068f, 0.8314697f, 0.9238796f, 0.9807853f,
            1, 0.9807853f, 0.9238795f, 0.8314696f, 0.7071067f, 0.5555702f, 0.3826834f, 0.1950902f,
            0, -0.1950904f, -0.3826835f, -0.5555703f, -0.7071068f, -0.8314697f, -0.9238796f, -0.9807853f,
            -1, -0.9807853f, -0.9238795f, -0.8314695f, -0.7071066f, -0.5555701f, -0.3826833f, -0.1950902f,
            0, 0.1950903f, 0.3826835f, 0.5555702f, 0.7071068f, 0.8314697f, 0.9238796f, 0.9807853f
        };//offset 8(=32/4) to get cos value

        public static float SinApprox(float radian) {
            radian *= ivtdenom;
            int slice = (int)radian;
            return sinapprox[slice] + (radian - slice) * tdenom;
        }

        public static float CosApprox(float radian) {
            radian *= ivtdenom;
            int slice = (int)radian;
            radian = (radian - slice) * tdenom;
            return sinapprox[slice + 8] - radian * radian * 0.5f;
        }

        [System.Obsolete("very inaccurate")]
        public static Quaternion QuaternionZApprox(float y, float x) {
            y = (float)(Math.Atan2(x, y) * 0.5);
            x = -1;
            if(y < 0) {
                x = -x;
                y = -y;
            }
            y *= ivtdenom;
            int slice = (int)y;
            y = (y - slice) * tdenom;
            return new Quaternion(0, 0, (sinapprox[slice] + y) * x, sinapprox[slice + 8] - y * y * 0.5f);
        }

        [System.Obsolete("very inaccurate")]
        public static Quaternion QuaternionZApprox(float radian) {
            radian *= 0.5f;
            int sign = -1;
            if(radian < 0) {
                sign = -sign;
                radian = -radian;
            }
            radian *= ivtdenom;
            int slice = (int)radian;
            radian = (radian - slice) * tdenom;
            return new Quaternion(0, 0, (sinapprox[slice] + radian) * sign, sinapprox[slice + 8] - radian * radian * 0.5f);
        }

        #endregion trigonometric approximation table

        public static float SqrtApproxNewton(float v, float guess, int itr) {
            float rval = guess;
            for(int r = 0;r < itr;++r) {
                rval = (rval + v / rval) * 0.5f;
            }
            return rval;
        }

        //http://www.codecodex.com/wiki/Calculate_an_integer_square_root
        // Finds the integer square root of a positive number  
        public static int Sqrt(Int64 num) {
            if(0 == num) return 0;  // Avoid zero divide  
            var n = (num / 2) + 1;       // Initial estimate, never low  
            var n1 = (n + (num / n)) / 2;
            while(n1 < n) {
                n = n1;
                n1 = (n + (num / n)) / 2;
            }
            return (int)n;
        }

        #region triangle winding

        // 2D cross product of OA and OB vectors, i.e. z-component of their 3D cross product.
        // Returns a positive value, if OAB makes a counter-clockwise turn,
        // negative for clockwise turn, and zero if the points are collinear.
        public static float TriangleWindingCCW(Float2 o, Float2 a, Float2 b) {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }
        public static float TriangleWindingCCW(Float2 oa, Float2 ob) {
            return oa.x * ob.y - oa.y * ob.x;
        }
        public static Int64 TriangleWindingCCW(Int2 o, Int2 a, Int2 b) {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }
        public static Int64 TriangleWindingCCW(Int2 oa, Int2 ob) {
            return oa.x * ob.y - oa.y * ob.x;
        }

        #endregion triangle winding

        #region point-polygon 2d

        //assume counter-clockwise http://blackpawn.com/texts/pointinpoly/
        public static bool PointTriangleBarycentric(Float2 t0_, Float2 t1_, Float2 t2_, Float2 p_) {
            Float2 v0 = t2_ - t0_;
            Float2 v1 = t1_ - t0_;
            Float2 v2 = p_ - t0_;
            float dot00 = Float2.Dot(v0, v0);
            float dot01 = Float2.Dot(v0, v1);
            float dot02 = Float2.Dot(v0, v2);
            float dot11 = Float2.Dot(v1, v1);
            float dot12 = Float2.Dot(v1, v2);
            float area2 = dot00 * dot11 - dot01 * dot01;
            float u = dot11 * dot02 - dot01 * dot12;
            float v = dot00 * dot12 - dot01 * dot02;
            return (u >= 0) && (v >= 0) && (u + v < area2);
        }
        public static bool PointTriangle2DOptimized(Float2 p0, Float2 p1, Float2 p2, Float2 p) {
            float dx = p.x - p2.x;
            float dy = p.y - p2.y;
            float dx21 = p2.x - p1.x;
            float dy12 = p1.y - p2.y;
            float det = dy12 * (p0.x - p2.x) + dx21 * (p0.y - p2.y);
            float s = dy12 * dx + dx21 * dy;
            float t = (p2.y - p0.y) * dx + (p0.x - p2.x) * dy;
            if (det < 0) return s <= 0 && t <= 0 && s + t >= det;
            //in a degenerate situation where p0=p1=p2, s + t = 0 & det = 0
            //the s +t <= det check will fail
            //thus the euqal condition is removed
            return s >= 0 && t >= 0 && s + t < det;
        }
        public static bool OriginTriangle2DOptimized(Float2 p0, Float2 p1, Float2 p2) {
            float dx21 = p2.x - p1.x;
            float dy12 = p1.y - p2.y;
            float det = dy12 * (p0.x - p2.x) + dx21 * (p0.y - p2.y);
            float s = -dy12 * p2.x - dx21 * p2.y;
            float t = (p0.y - p2.y) * p2.x + (p2.x - p0.x) * p2.y;
            if (det < 0) return s <= 0 && t <= 0 && s + t >= det;
            return s >= 0 && t >= 0 && s + t < det;
        }
        public static bool OriginTriangle2DOptimized(Int2 p0, Int2 p1, Int2 p2) {
            int dx21 = p2.x - p1.x;
            int dy12 = p1.y - p2.y;
            Int64 det = dy12 * (p0.x - p2.x) + dx21 * (p0.y - p2.y);
            Int64 s = -dy12 * p2.x - dx21 * p2.y;
            Int64 t = (p0.y - p2.y) * p2.x + (p2.x - p0.x) * p2.y;
            if (det < 0) return s <= 0 && t <= 0 && s + t >= det;
            return s >= 0 && t >= 0 && s + t < det;
        }

        #endregion point-polygon 2d

        #region line intersection

        //when p1 = (0,0);
        public static Float2 LineIntersect(Float2 p2_, Float2 q1_, Float2 q2_) {
            var a2 = q2_.y - q1_.y;
            var b2 = q1_.x - q2_.x;
            var c2 = a2 * q1_.x + b2 * q1_.y;
            var det = p2_.y * b2 + a2 * p2_.x;
            if (det > float.Epsilon || det < -float.Epsilon) {
                return p2_ * (c2 / det);
            }
            return new Float2(0, 0);
        }

        //modified. From Mark Bayazit's convex decomposition algorithm
        public static bool LineIntersect(Float2 p1_, Float2 p2_, Float2 q1_, Float2 q2_, ref Float2 intersection_) {
            var a1 = p2_.y - p1_.y;
            var b1 = p1_.x - p2_.x;
            var c1 = a1 * p1_.x + b1 * p1_.y;
            var a2 = q2_.y - q1_.y;
            var b2 = q1_.x - q2_.x;
            var c2 = a2 * q1_.x + b2 * q1_.y;
            var det = a1 * b2 - a2 * b1;
            if (det > float.Epsilon || det < -float.Epsilon) {
                intersection_ = new Float2((b2 * c1 - b1 * c2) / det, (a1 * c2 - a2 * c1) / det);
                return true;
            }
            return false;
        }

        //modified. from Eric Jordan's convex decomposition library, it checks if the lines a0->a1 and b0->b1 cross. Grazing lines should not return true.
        public static bool LineIntersect2(Float2 a0_, Float2 a1_, Float2 b0_, Float2 b1_, ref Float2 intersection_) {
            if (a0_ == b0_ || a0_ == b1_ || a1_ == b0_ || a1_ == b1_) return false;
            if (Max(a0_.x, a1_.x) < Min(b0_.x, b1_.x)
                || Max(b0_.x, b1_.x) < Min(a0_.x, a1_.x)
                || Max(a0_.y, a1_.y) < Min(b0_.y, b1_.y)
                || Max(b0_.y, b1_.y) < Min(a0_.y, a1_.y)) return false; //AABB early exit
            var ua = ((b1_.x - b0_.x) * (a0_.y - b0_.y) - (b1_.y - b0_.y) * (a0_.x - b0_.x));
            var ub = ((a1_.x - a0_.x) * (a0_.y - b0_.y) - (a1_.y - a0_.y) * (a0_.x - b0_.x));
            var denom = (b1_.y - b0_.y) * (a1_.x - a0_.x) - (b1_.x - b0_.x) * (a1_.y - a0_.y);
            if (denom < float.Epsilon && denom > -float.Epsilon) return false;//parellel
            ua /= denom;
            ub /= denom;
            if ((0 < ua) && (ua < 1) && (0 < ub) && (ub < 1)) {
                intersection_.x = (a0_.x + ua * (a1_.x - a0_.x));
                intersection_.y = (a0_.y + ua * (a1_.y - a0_.y));
                return true;
            }
            return false;
        }

        #endregion line intersection
    }

    public static class MortonOrder {
        public static uint Interleave(ushort x_, ushort y_) {
            ulong z = ((ulong)x_ << 32) | y_;
            z = (z | (z << 8)) & 0x00FF_00FF_00FF_00FF;//0000000011111111...
            z = (z | (z << 4)) & 0x0F0F_0F0F_0F0F_0F0F;//0000111100001111...
            z = (z | (z << 2)) & 0x3333_3333_3333_3333;//0011001100110011...
            z = (z | (z << 1)) & 0x5555_5555_5555_5555;//0101010101010101...
            z = (z & 0x0000_0000_FFFF_FFFF) | (z >> 31);
            return (uint)z;
        }

        public static void Deinterleave(uint z_, out ushort x_, out ushort y_) {
            ulong n = z_;
            n = ((n & 0xAAAA_AAAA) << 31) | (n & 0x5555_5555);
            n = (n | (n >> 1)) & 0x3333_3333_3333_3333;
            n = (n | (n >> 2)) & 0x0F0F_0F0F_0F0F_0F0F;
            n = (n | (n >> 4)) & 0x00FF_00FF_00FF_00FF;
            n = (n | (n >> 8)) & 0x0000_FFFF_0000_FFFF;
            x_ = (ushort)(n >> 32);
            y_ = (ushort)(n & 0x0000_0000_FFFF_FFFF);
        }
    }
}