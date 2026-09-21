using System;

namespace MountainWardBarrier.Core
{
    /// <summary>
    /// 确定性随机数发生器（xorshift32）。
    /// 同一 seed 的对局会产生完全一致的结果，方便复现 bug 与做平衡性回归测试。
    /// </summary>
    public class SimRandom
    {
        private uint _state;

        public SimRandom()
        {
            _state = 2463534242u;
        }

        public SimRandom(int seed)
        {
            Seed(seed);
        }

        public void Seed(int seed)
        {
            uint s = (uint)seed;
            if (s == 0u)
            {
                s = 2463534242u;
            }
            _state = s;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>[0,1) 的浮点数。</summary>
        public float NextFloat()
        {
            return (float)(NextUInt() & 0x00FFFFFFu) / 16777216f;
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>[minInclusive, maxExclusive) 的整数。</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }
            int span = maxExclusive - minInclusive;
            return minInclusive + (int)(NextUInt() % (uint)span);
        }

        public bool Chance(float probability)
        {
            if (probability <= 0f)
            {
                return false;
            }
            if (probability >= 1f)
            {
                return true;
            }
            return NextFloat() < probability;
        }
    }
}
