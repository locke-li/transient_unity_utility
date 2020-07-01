using System;

namespace Transient.Mathematical {
    //http://xoroshiro.di.unimi.it/splitmix64.c
    public class RandomSplitMix64 {
        UInt64 s;

        public RandomSplitMix64(UInt64 seed) {
            s = seed;
        }

        public UInt64 Next() {
            return Next(ref s);
        }

        public static UInt64 Next(ref UInt64 s_) {
            UInt64 z = (s_ += 0x9e3779b97f4a7c15);
            z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9;
            z = (z ^ (z >> 27)) * 0x94d049bb133111eb;
            return z ^ (z >> 31);
        }
    }
    //http://xoroshiro.di.unimi.it/xorshift128plus.c
    public class RandomXorshift128plus {
        UInt64 s_0;
        UInt64 s_1;

        public RandomXorshift128plus(UInt64 seed) {
            s_0 = RandomSplitMix64.Next(ref seed);
            s_1 = RandomSplitMix64.Next(ref seed);
        }

        public UInt32 Next() {
            UInt64 s1 = s_0;
            UInt64 s0 = s_1;
            UInt32 result = (UInt32)((s0 + s1) >> 32);
            s_0 = s0;
            s1 ^= s1 << 23; // a
            s_1 = s1 ^ s0 ^ (s1 >> 18) ^ (s0 >> 5); // b, c
            return result;
        }
    }
    //http://xoroshiro.di.unimi.it/xoroshiro128plus.c
    public class RandomXoroshiro128plus {
        UInt64 s_0;
        UInt64 s_1;

        public RandomXoroshiro128plus(UInt64 seed) {
            s_0 = RandomSplitMix64.Next(ref seed);
            s_1 = RandomSplitMix64.Next(ref seed);
        }

        static UInt64 rotl(UInt64 x, int k) {
            return (x << k) | (x >> (64 - k));
        }

        public UInt32 Next() {
            UInt64 s0 = s_0;
            UInt64 s1 = s_1;
            UInt32 result = (UInt32)((s0 + s1) >> 32);
            s1 ^= s0;
            s_0 = rotl(s0, 55) ^ s1 ^ (s1 << 14); // a, b
            s_1 = rotl(s1, 36); // c
            return result;
        }
    }

    public class RandomVariant31 {
        UInt64 s_0;
        UInt64 s_1;

        public RandomVariant31(UInt64 seed) {
            s_0 = RandomSplitMix64.Next(ref seed);
            s_1 = RandomSplitMix64.Next(ref seed);
        }

        static UInt64 rotl(UInt64 x, int k) {
            return (x << k) | (x >> (64 - k));
        }

        public int Next() {
            UInt64 s0 = s_0;
            UInt64 s1 = s_1;
            int result = (int)((s0 + s1) >> 33);
            s1 ^= s0;
            s_0 = rotl(s0, 55) ^ s1 ^ (s1 << 14); // a, b
            s_1 = rotl(s1, 36); // c
            return result;
        }

        public int Next(int range_) {
            return Next() % range_;
        }

        public int Next(int min_, int range_) {
            return Next() % range_ + min_;
        }

        public int Deviation(int deviation_) {
            return Next() % (deviation_ * 2) - deviation_;
        }

        public float Deviation(float deviation_) {
            int d = (int)(deviation_ * 10000);
            if(d == 0)
                return 0;
            return (Next() % (d * 2) - d) * 0.0001f;
        }
    }
}