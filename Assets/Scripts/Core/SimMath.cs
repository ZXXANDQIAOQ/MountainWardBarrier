using System;

namespace MountainWardBarrier.Core
{
    /// <summary>
    /// 格子坐标（整数）。纯逻辑层使用，刻意不依赖 UnityEngine。
    /// </summary>
    [Serializable]
    public struct GridPos : IEquatable<GridPos>
    {
        public int x;
        public int y;

        public GridPos(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(GridPos other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            if (obj is GridPos)
            {
                return Equals((GridPos)obj);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (x * 397) ^ y;
        }

        public override string ToString()
        {
            return "(" + x + "," + y + ")";
        }

        public static bool operator ==(GridPos a, GridPos b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(GridPos a, GridPos b)
        {
            return !(a == b);
        }
    }

    /// <summary>
    /// 二维浮点向量。用来表示妖魔 / 弹道在格子空间里的连续坐标。
    /// </summary>
    [Serializable]
    public struct Float2
    {
        public float x;
        public float y;

        public Float2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Float2 Zero
        {
            get { return new Float2(0f, 0f); }
        }

        public float SqrMagnitude
        {
            get { return x * x + y * y; }
        }

        public float Magnitude
        {
            get { return (float)Math.Sqrt((double)(x * x + y * y)); }
        }

        public bool IsZero
        {
            get { return Math.Abs(x) < 0.000001f && Math.Abs(y) < 0.000001f; }
        }

        public static Float2 operator +(Float2 a, Float2 b)
        {
            return new Float2(a.x + b.x, a.y + b.y);
        }

        public static Float2 operator -(Float2 a, Float2 b)
        {
            return new Float2(a.x - b.x, a.y - b.y);
        }

        public static Float2 operator *(Float2 a, float s)
        {
            return new Float2(a.x * s, a.y * s);
        }

        public static Float2 operator *(float s, Float2 a)
        {
            return new Float2(a.x * s, a.y * s);
        }

        public static Float2 operator /(Float2 a, float s)
        {
            if (Math.Abs(s) < 0.0000001f)
            {
                return Zero;
            }
            return new Float2(a.x / s, a.y / s);
        }

        public static float Distance(Float2 a, Float2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return (float)Math.Sqrt((double)(dx * dx + dy * dy));
        }

        public static float SqrDistance(Float2 a, Float2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        public static Float2 Lerp(Float2 a, Float2 b, float t)
        {
            if (t < 0f)
            {
                t = 0f;
            }
            if (t > 1f)
            {
                t = 1f;
            }
            return new Float2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        }

        public static Float2 Normalized(Float2 v)
        {
            float m = v.Magnitude;
            if (m < 0.000001f)
            {
                return Zero;
            }
            return new Float2(v.x / m, v.y / m);
        }

        /// <summary>朝目标移动至多 maxDelta 距离，不会越过目标点。</summary>
        public static Float2 MoveTowards(Float2 current, Float2 target, float maxDelta)
        {
            float dx = target.x - current.x;
            float dy = target.y - current.y;
            float dist = (float)Math.Sqrt((double)(dx * dx + dy * dy));
            if (dist <= maxDelta || dist < 0.000001f)
            {
                return target;
            }
            float k = maxDelta / dist;
            return new Float2(current.x + dx * k, current.y + dy * k);
        }

        public override string ToString()
        {
            return "(" + x.ToString("F2") + "," + y.ToString("F2") + ")";
        }
    }

    /// <summary>通用数学工具。</summary>
    public static class SimMath
    {
        public static float Clamp01(float v)
        {
            if (v < 0f)
            {
                return 0f;
            }
            if (v > 1f)
            {
                return 1f;
            }
            return v;
        }

        public static float Clamp(float v, float min, float max)
        {
            if (v < min)
            {
                return min;
            }
            if (v > max)
            {
                return max;
            }
            return v;
        }

        public static int ClampInt(int v, int min, int max)
        {
            if (v < min)
            {
                return min;
            }
            if (v > max)
            {
                return max;
            }
            return v;
        }

        /// <summary>曼哈顿距离（格）。</summary>
        public static int Manhattan(GridPos a, GridPos b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        /// <summary>把秒数格式化成 mm:ss。</summary>
        public static string FormatTime(float seconds)
        {
            if (seconds < 0f)
            {
                seconds = 0f;
            }
            int total = (int)seconds;
            int m = total / 60;
            int s = total % 60;
            return m.ToString("00") + ":" + s.ToString("00");
        }
    }
}
