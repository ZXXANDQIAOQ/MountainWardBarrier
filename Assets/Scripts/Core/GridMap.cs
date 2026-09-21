using System;
using System.Collections.Generic;

namespace MountainWardBarrier.Core
{
    /// <summary>一条路径的完整数据：拐点、栅格化后的格子、总长度。</summary>
    public class PathData
    {
        /// <summary>连续空间的拐点（格子中心坐标），妖魔沿着它移动。</summary>
        public List<Float2> Waypoints = new List<Float2>();
        /// <summary>栅格化后路径覆盖的全部格子（有序，从入口到山门）。</summary>
        public List<GridPos> Cells = new List<GridPos>();
        /// <summary>路径总长度（格）。</summary>
        public float Length;

        /// <summary>根据已走过的距离求当前位置。</summary>
        public Float2 SampleAt(float travelled)
        {
            if (Waypoints.Count == 0)
            {
                return Float2.Zero;
            }
            if (Waypoints.Count == 1 || travelled <= 0f)
            {
                return Waypoints[0];
            }
            float remaining = travelled;
            for (int i = 0; i < Waypoints.Count - 1; i++)
            {
                Float2 a = Waypoints[i];
                Float2 b = Waypoints[i + 1];
                float segLen = Float2.Distance(a, b);
                if (remaining <= segLen)
                {
                    float t = segLen <= 0.0001f ? 0f : remaining / segLen;
                    return Float2.Lerp(a, b, t);
                }
                remaining -= segLen;
            }
            return Waypoints[Waypoints.Count - 1];
        }
    }

    /// <summary>
    /// 棋盘地图。负责把「路径拐点」栅格化成格子，并回答「这个格子能不能放阵法」。
    /// 逻辑层与表现层共用它，保证画出来的道路和妖魔走的路完全一致。
    /// </summary>
    public class GridMap
    {
        public readonly int Columns;
        public readonly int Rows;

        private readonly bool[] _path;
        private readonly bool[] _core;

        public readonly PathData PathA;
        public readonly PathData PathB;

        public GridMap(GameDatabase db)
        {
            Columns = db.columns > 0 ? db.columns : DefaultConfig.Columns;
            Rows = db.rows > 0 ? db.rows : DefaultConfig.Rows;
            _path = new bool[Columns * Rows];
            _core = new bool[Columns * Rows];

            PathA = BuildPath(db.pathA);
            PathB = BuildPath(db.pathB);

            for (int i = 0; i < PathA.Cells.Count; i++)
            {
                Mark(_path, PathA.Cells[i]);
            }
            for (int i = 0; i < PathB.Cells.Count; i++)
            {
                Mark(_path, PathB.Cells[i]);
            }
            for (int i = 0; i < db.coreCells.Count; i++)
            {
                Mark(_core, db.coreCells[i]);
            }
        }

        private void Mark(bool[] arr, GridPos p)
        {
            if (!InBounds(p.x, p.y))
            {
                return;
            }
            arr[p.y * Columns + p.x] = true;
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Columns && y < Rows;
        }

        public bool IsPath(int x, int y)
        {
            if (!InBounds(x, y))
            {
                return false;
            }
            return _path[y * Columns + x];
        }

        public bool IsCore(int x, int y)
        {
            if (!InBounds(x, y))
            {
                return false;
            }
            return _core[y * Columns + x];
        }

        /// <summary>空地：既不是路，也不是山门核心，可以布置阵法 / 仙灵。</summary>
        public bool IsBuildable(int x, int y)
        {
            if (!InBounds(x, y))
            {
                return false;
            }
            int idx = y * Columns + x;
            return !_path[idx] && !_core[idx];
        }

        public CellType GetCellType(int x, int y)
        {
            if (IsCore(x, y))
            {
                return CellType.Core;
            }
            if (IsPath(x, y))
            {
                return CellType.Path;
            }
            return CellType.Buildable;
        }

        /// <summary>把连续坐标换算成所在格子。</summary>
        public GridPos WorldToCell(Float2 world)
        {
            return new GridPos((int)Math.Floor((double)world.x), (int)Math.Floor((double)world.y));
        }

        /// <summary>格子中心坐标。</summary>
        public static Float2 CellCenter(int x, int y)
        {
            return new Float2(x + 0.5f, y + 0.5f);
        }

        public static Float2 CellCenter(GridPos p)
        {
            return CellCenter(p.x, p.y);
        }

        public List<GridPos> CoreCells
        {
            get
            {
                List<GridPos> list = new List<GridPos>();
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Columns; x++)
                    {
                        if (IsCore(x, y))
                        {
                            list.Add(new GridPos(x, y));
                        }
                    }
                }
                return list;
            }
        }

        /// <summary>山门核心的中心点（用于绘制与受伤特效）。</summary>
        public Float2 CoreCenter
        {
            get
            {
                List<GridPos> cells = CoreCells;
                if (cells.Count == 0)
                {
                    return new Float2(Columns * 0.5f, Rows - 0.5f);
                }
                float sx = 0f;
                float sy = 0f;
                for (int i = 0; i < cells.Count; i++)
                {
                    Float2 c = CellCenter(cells[i]);
                    sx += c.x;
                    sy += c.y;
                }
                return new Float2(sx / cells.Count, sy / cells.Count);
            }
        }

        public PathData GetPath(int index)
        {
            return index == 1 ? PathB : PathA;
        }

        // ------------------------------------------------------------------ 内部

        private PathData BuildPath(List<GridPos> waypoints)
        {
            PathData data = new PathData();
            if (waypoints == null || waypoints.Count == 0)
            {
                return data;
            }

            for (int i = 0; i < waypoints.Count; i++)
            {
                data.Waypoints.Add(CellCenter(waypoints[i]));
            }

            float total = 0f;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                total += Float2.Distance(data.Waypoints[i], data.Waypoints[i + 1]);
            }
            data.Length = total;

            // 栅格化：保证「画出来的路」和「妖魔走的路」是同一批格子。
            List<GridPos> cells = new List<GridPos>();
            cells.Add(waypoints[0]);
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                RasterizeSegment(waypoints[i], waypoints[i + 1], cells);
            }
            for (int i = 0; i < cells.Count; i++)
            {
                if (!InBounds(cells[i].x, cells[i].y))
                {
                    continue;
                }
                bool dup = false;
                for (int j = 0; j < data.Cells.Count; j++)
                {
                    if (data.Cells[j] == cells[i])
                    {
                        dup = true;
                        break;
                    }
                }
                if (!dup)
                {
                    data.Cells.Add(cells[i]);
                }
            }

            return data;
        }

        private static void RasterizeSegment(GridPos a, GridPos b, List<GridPos> outCells)
        {
            int dx = b.x - a.x;
            int dy = b.y - a.y;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (steps <= 0)
            {
                return;
            }
            for (int i = 1; i <= steps; i++)
            {
                int x = a.x + (int)Math.Round((double)dx * i / steps, MidpointRounding.AwayFromZero);
                int y = a.y + (int)Math.Round((double)dy * i / steps, MidpointRounding.AwayFromZero);
                outCells.Add(new GridPos(x, y));
            }
        }
    }
}
