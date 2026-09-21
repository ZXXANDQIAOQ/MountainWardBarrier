using System.Collections.Generic;
using MountainWardBarrier.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// 棋盘视图。
    ///
    /// 只用 uGUI 画（不用世界空间 SpriteRenderer），好处是坐标系简单、
    /// 自适应任何分辨率都不需要额外处理，触摸命中判定也直接复用 uGUI 的射线。
    ///
    /// 格子坐标约定（和 Core 一致）：
    ///     y = 0 在最上面（妖魔从这里进场），y 越大越靠近山门。
    /// 转换到 uGUI 的本地坐标时，y 轴要翻转。
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        private const int MaxFx = 56;
        private const int MaxTexts = 40;

        public RectTransform Root;

        private BattleSimulation _sim;
        private GridMap _map;
        private float _cell;
        private float _boardW;
        private float _boardH;

        private readonly List<Image> _cells = new List<Image>();
        private Image _coreGate;
        private Image _rangeRing;
        private Image _ghost;

        private UiKit.IdPool _towers;
        private UiKit.IdPool _spirits;
        private UiKit.IdPool _traps;
        private UiKit.IdPool _enemies;
        private UiKit.IdPool _projectiles;

        private readonly List<BarView> _barPool = new List<BarView>();
        private readonly Dictionary<int, BarView> _bars = new Dictionary<int, BarView>();
        private readonly List<int> _barSeen = new List<int>();
        private readonly List<int> _barStale = new List<int>();

        private readonly List<FxView> _fx = new List<FxView>();
        private readonly List<Text> _textPool = new List<Text>();
        private readonly List<DmgView> _texts = new List<DmgView>();

        private float _shakeTime;
        private float _shakeStrength;
        private Vector2 _boardBasePos;

        private class BarView
        {
            public Image Bg;
            public Image Fill;
            public bool Active;
        }

        private class FxView
        {
            public Image Img;
            public float Life;
            public float Duration;
            public float StartSize;
            public float EndSize;
            public Color Tint;
            public bool Spinning;
        }

        private class DmgView
        {
            public Text Label;
            public float Life;
            public float Duration;
            public Vector2 Start;
            public Vector2 Drift;
            public Color Tint;
        }

        // ------------------------------------------------------------ 构建

        public static BoardView Build(RectTransform parent, BattleSimulation sim, float boardWidth,
            float boardHeight)
        {
            RectTransform rt = UiKit.Node("BoardPanel", parent);
            BoardView view = rt.gameObject.AddComponent<BoardView>();
            view.Root = rt;
            view._sim = sim;
            view._map = sim.Map;
            view._boardW = boardWidth;
            view._boardH = boardHeight;
            view._cell = boardHeight / sim.Map.Rows;
            rt.sizeDelta = new Vector2(boardWidth, boardHeight);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            view.BuildInputCatcher();
            view.BuildLayers();
            return view;
        }

        /// <summary>
        /// 铺一层透明接收层。
        ///
        /// uGUI 的点击事件必须先命中一个 raycastTarget 才会向父级冒泡，
        /// 而地砖和单位都是 raycastTarget = false（免得挡住卡片的拖拽），
        /// 所以需要这么一层专门吃触摸。它不画任何东西，也不影响层次。
        /// </summary>
        private void BuildInputCatcher()
        {
            RectTransform catcher = UiKit.Node("InputCatcher", Root);
            UiKit.Stretch(catcher);
            catcher.SetAsFirstSibling();
            UiKit.EmptyGraphic graphic = catcher.gameObject.AddComponent<UiKit.EmptyGraphic>();
            graphic.raycastTarget = true;
            graphic.color = new Color(0f, 0f, 0f, 0f);
        }

        private void BuildLayers()
        {
            Transform cellsLayer = UiKit.Node("Cells", Root).transform;
            UiKit.Stretch((RectTransform)cellsLayer);
            Transform coreLayer = UiKit.Node("Core", Root).transform;
            UiKit.Stretch((RectTransform)coreLayer);
            Transform trapsLayer = UiKit.Node("Traps", Root).transform;
            UiKit.Stretch((RectTransform)trapsLayer);
            Transform towersLayer = UiKit.Node("Towers", Root).transform;
            UiKit.Stretch((RectTransform)towersLayer);
            Transform spiritsLayer = UiKit.Node("Spirits", Root).transform;
            UiKit.Stretch((RectTransform)spiritsLayer);
            Transform enemiesLayer = UiKit.Node("Enemies", Root).transform;
            UiKit.Stretch((RectTransform)enemiesLayer);
            Transform projectilesLayer = UiKit.Node("Projectiles", Root).transform;
            UiKit.Stretch((RectTransform)projectilesLayer);
            Transform fxLayer = UiKit.Node("Fx", Root).transform;
            UiKit.Stretch((RectTransform)fxLayer);
            Transform ghostLayer = UiKit.Node("Ghost", Root).transform;
            UiKit.Stretch((RectTransform)ghostLayer);
            Transform textLayer = UiKit.Node("Texts", Root).transform;
            UiKit.Stretch((RectTransform)textLayer);

            // 地砖
            Sprite pathTile = SpriteLibrary.Get("tile_path");
            Sprite buildTile = SpriteLibrary.Get("tile_buildable");
            Sprite buildTileAlt = SpriteLibrary.Get("tile_buildable_alt");
            for (int y = 0; y < _map.Rows; y++)
            {
                for (int x = 0; x < _map.Columns; x++)
                {
                    CellType type = _map.GetCellType(x, y);
                    Sprite sprite;
                    Color tint;
                    if (type == CellType.Path)
                    {
                        sprite = pathTile;
                        tint = Color.white;
                    }
                    else if (type == CellType.Core)
                    {
                        sprite = pathTile;
                        tint = new Color(0.72f, 0.66f, 0.62f, 1f);
                    }
                    else
                    {
                        bool alt = ((x + y) % 2) == 0;
                        sprite = alt ? buildTile : buildTileAlt;
                        tint = Color.white;
                    }
                    Image img = UiKit.NewImage(cellsLayer, "Cell_" + x + "_" + y,
                        sprite != null ? sprite : SpriteLibrary.MakeRoundedSprite(new Color(0.2f, 0.24f, 0.28f)),
                        sprite != null ? tint : Color.white);
                    RectTransform ert = img.rectTransform;
                    ert.anchorMin = new Vector2(0.5f, 0.5f);
                    ert.anchorMax = new Vector2(0.5f, 0.5f);
                    ert.pivot = new Vector2(0.5f, 0.5f);
                    ert.sizeDelta = new Vector2(_cell + 0.5f, _cell + 0.5f);
                    ert.anchoredPosition = CellToLocal(new GridPos(x, y));
                    _cells.Add(img);
                }
            }

            // 山门
            Float2 coreCenter = _map.CoreCenter;
            _coreGate = UiKit.NewImage(coreLayer, "CoreGate", SpriteLibrary.Get("core_gate"),
                Color.white);
            _coreGate.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _coreGate.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _coreGate.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _coreGate.rectTransform.sizeDelta = new Vector2(_cell * 2.35f, _cell * 1.2f);
            _coreGate.rectTransform.anchoredPosition = WorldToLocal(coreCenter);
            _coreGate.preserveAspect = true;

            // 两路入口
            for (int p = 0; p < 2; p++)
            {
                PathData path = _map.GetPath(p);
                if (path.Waypoints.Count == 0)
                {
                    continue;
                }
                Float2 start = path.Waypoints[0];
                Image portal = UiKit.NewImage(coreLayer, "Portal_" + p,
                    SpriteLibrary.Get(p == 0 ? "portal_a" : "portal_b"), Color.white);
                portal.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                portal.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                portal.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                portal.rectTransform.sizeDelta = new Vector2(_cell * 1.5f, _cell * 0.95f);
                portal.rectTransform.anchoredPosition = WorldToLocal(start);
                portal.preserveAspect = true;
            }

            // 射程指示环（复用一个）
            _rangeRing = UiKit.NewImage(fxLayer, "RangeRing", SpriteLibrary.Get("fx_ring"),
                new Color(0.55f, 1f, 0.85f, 0.30f));
            _rangeRing.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rangeRing.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rangeRing.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _rangeRing.gameObject.SetActive(false);

            // 放置预览
            _ghost = UiKit.NewImage(ghostLayer, "Ghost", null, new Color(1f, 1f, 1f, 0.6f));
            _ghost.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _ghost.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _ghost.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _ghost.rectTransform.sizeDelta = new Vector2(_cell * 0.9f, _cell * 0.9f);
            _ghost.gameObject.SetActive(false);

            _towers = new UiKit.IdPool(towersLayer, "Tower", null, Color.white);
            _spirits = new UiKit.IdPool(spiritsLayer, "Spirit", null, Color.white);
            _traps = new UiKit.IdPool(trapsLayer, "Trap", null, Color.white);
            _enemies = new UiKit.IdPool(enemiesLayer, "Enemy", null, Color.white);
            _projectiles = new UiKit.IdPool(projectilesLayer, "Proj",
                SpriteLibrary.Get("fx_bolt"), Color.white);

            _boardBasePos = Root.anchoredPosition;
        }

        // ------------------------------------------------------------ 坐标换算

        public float CellSize
        {
            get { return _cell; }
        }

        public Vector2 CellToLocal(GridPos cell)
        {
            return new Vector2(
                (cell.x + 0.5f) * _cell - _boardW * 0.5f,
                _boardH * 0.5f - (cell.y + 0.5f) * _cell);
        }

        public Vector2 WorldToLocal(Float2 world)
        {
            return new Vector2(world.x * _cell - _boardW * 0.5f,
                _boardH * 0.5f - world.y * _cell);
        }

        /// <summary>本地坐标 → 格子；返回 null 表示落在棋盘之外。</summary>
        public bool LocalToCell(Vector2 local, out GridPos cell)
        {
            int x = Mathf.FloorToInt((local.x + _boardW * 0.5f) / _cell);
            int y = Mathf.FloorToInt((_boardH * 0.5f - local.y) / _cell);
            cell = new GridPos(x, y);
            return _map.InBounds(x, y);
        }

        // ------------------------------------------------------------ 放置预览

        public void ShowGhost(GridPos? cell, string spriteName, bool valid, float rangeInCells)
        {
            if (_ghost == null)
            {
                return;
            }
            if (!cell.HasValue)
            {
                _ghost.gameObject.SetActive(false);
                _rangeRing.gameObject.SetActive(false);
                return;
            }
            _ghost.gameObject.SetActive(true);
            _ghost.sprite = SpriteLibrary.GetOrFallback(spriteName, Palette.Jade);
            _ghost.color = valid
                ? new Color(0.75f, 1f, 0.9f, 0.65f)
                : new Color(1f, 0.5f, 0.45f, 0.55f);
            _ghost.rectTransform.anchoredPosition = CellToLocal(cell.Value);

            if (rangeInCells > 0.01f)
            {
                _rangeRing.gameObject.SetActive(true);
                _rangeRing.rectTransform.anchoredPosition = CellToLocal(cell.Value);
                float d = rangeInCells * 2f * _cell;
                _rangeRing.rectTransform.sizeDelta = new Vector2(d, d);
                _rangeRing.color = valid
                    ? new Color(0.55f, 1f, 0.85f, 0.26f)
                    : new Color(1f, 0.5f, 0.45f, 0.20f);
            }
            else
            {
                _rangeRing.gameObject.SetActive(false);
            }
        }

        public void HideGhost()
        {
            if (_ghost != null)
            {
                _ghost.gameObject.SetActive(false);
            }
            if (_rangeRing != null)
            {
                _rangeRing.gameObject.SetActive(false);
            }
        }

        /// <summary>选中某个阵法时，只显示它的射程圈。</summary>
        public void ShowRange(GridPos cell, float rangeInCells, Color color)
        {
            if (_rangeRing == null)
            {
                return;
            }
            _rangeRing.gameObject.SetActive(true);
            _rangeRing.rectTransform.anchoredPosition = CellToLocal(cell);
            float d = Mathf.Max(rangeInCells, 0.6f) * 2f * _cell;
            _rangeRing.rectTransform.sizeDelta = new Vector2(d, d);
            _rangeRing.color = color;
        }

        // ------------------------------------------------------------ 特效

        public void SpawnFx(string spriteName, Vector2 local, float startSize, float endSize,
            float duration, Color tint, bool spin)
        {
            Image img = null;
            for (int i = 0; i < _fx.Count; i++)
            {
                if (i < MaxFx && _fx[i].Life <= 0f && _fx[i].Img != null)
                {
                    img = _fx[i].Img;
                    break;
                }
            }
            if (img == null && _fx.Count < MaxFx)
            {
                img = UiKit.NewImage(Root, "Fx_" + _fx.Count, null, Color.white);
                img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                FxView fresh = new FxView();
                fresh.Img = img;
                fresh.Life = 0f;
                _fx.Add(fresh);
            }
            if (img == null)
            {
                return;
            }

            img.sprite = SpriteLibrary.GetOrFallback(spriteName, tint);
            img.gameObject.SetActive(true);
            img.rectTransform.anchoredPosition = local;
            img.rectTransform.localRotation = Quaternion.identity;

            for (int i = 0; i < _fx.Count; i++)
            {
                if (_fx[i].Img == img)
                {
                    _fx[i].Life = duration;
                    _fx[i].Duration = duration;
                    _fx[i].StartSize = startSize;
                    _fx[i].EndSize = endSize;
                    _fx[i].Tint = tint;
                    _fx[i].Spinning = spin;
                    break;
                }
            }
        }

        public void SpawnDamageText(Vector2 local, float amount, Color color)
        {
            Text label = null;
            for (int i = 0; i < _texts.Count; i++)
            {
                if (_texts[i].Life <= 0f)
                {
                    label = _texts[i].Label;
                    break;
                }
            }
            if (label == null && _textPool.Count > 0)
            {
                label = _textPool[_textPool.Count - 1];
                _textPool.RemoveAt(_textPool.Count - 1);
            }
            if (label == null && _texts.Count < MaxTexts)
            {
                label = UiKit.NewLabel(Root, "Dmg_" + _texts.Count, "0", 30, Color.white,
                    TextAnchor.MiddleCenter);
                RectTransform rt = label.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(140f, 44f);
            }
            if (label == null)
            {
                return;
            }

            DmgView view = null;
            for (int i = 0; i < _texts.Count; i++)
            {
                if (_texts[i].Label == label)
                {
                    view = _texts[i];
                    break;
                }
            }
            if (view == null)
            {
                view = new DmgView();
                view.Label = label;
                _texts.Add(view);
            }

            string text = amount >= 100f ? Mathf.RoundToInt(amount).ToString()
                : Mathf.RoundToInt(amount).ToString();
            label.text = text;
            label.color = color;
            label.gameObject.SetActive(true);

            view.Life = 0.75f;
            view.Duration = 0.75f;
            view.Start = local + new Vector2(Random.Range(-14f, 14f), _cell * 0.28f);
            view.Drift = new Vector2(Random.Range(-16f, 16f), 62f);
            view.Tint = color;
            label.rectTransform.anchoredPosition = view.Start;
        }

        public void Shake(float strength)
        {
            _shakeStrength = Mathf.Max(_shakeStrength, strength);
            _shakeTime = Mathf.Max(_shakeTime, 0.22f);
        }

        // ------------------------------------------------------------ 每帧同步

        public void Sync(float dt, bool showDamageNumbers)
        {
            SyncEnemies(dt);
            SyncTowers(dt);
            SyncSpirits(dt);
            SyncTraps();
            SyncProjectiles();
            SyncEliteBars();
            SyncFx(dt);
            SyncTexts(dt);
            SyncShake(dt);
        }

        private void SyncEnemies(float dt)
        {
            _enemies.Begin();
            for (int i = 0; i < _sim.Enemies.Count; i++)
            {
                EnemyUnit e = _sim.Enemies[i];
                if (!e.alive)
                {
                    continue;
                }
                Image img = _enemies.Use(e.id);
                img.sprite = SpriteLibrary.GetOrFallback(e.config.sprite, Palette.Demon);
                img.rectTransform.anchoredPosition = WorldToLocal(e.position);
                float size = _cell * Mathf.Max(0.5f, e.visualScale) * 1.55f;
                img.rectTransform.sizeDelta = new Vector2(size, size);

                Color tint = Palette.EnemyTint(e.HpRatio);
                if (e.IsFrozen)
                {
                    tint = Color.Lerp(tint, new Color(0.62f, 0.88f, 1f, 1f), 0.72f);
                }
                else if (e.slowFactor < 0.999f)
                {
                    tint = Color.Lerp(tint, new Color(0.72f, 0.92f, 1f, 1f), 0.30f);
                }
                if (e.hitFlash > 0f)
                {
                    tint = Color.Lerp(tint, Color.white, 0.65f);
                }
                img.color = tint;

                if (e.shieldHp > 0f && e.shieldMax > 0f)
                {
                    float pulse = 0.35f + 0.28f * Mathf.Sin(Time.unscaledTime * 6f + e.id);
                    img.color = Color.Lerp(img.color, new Color(1f, 0.92f, 0.6f, 1f), pulse * 0.5f);
                }
            }
            _enemies.End();
        }

        private void SyncTowers(float dt)
        {
            _towers.Begin();
            for (int i = 0; i < _sim.Towers.Count; i++)
            {
                TowerUnit t = _sim.Towers[i];
                Image img = _towers.Use(t.id);
                img.sprite = SpriteLibrary.GetOrFallback(t.config.sprite, Palette.Jade);
                img.rectTransform.anchoredPosition = CellToLocal(t.cell);
                float size = _cell * 0.96f;
                if (t.fireFlash > 0f)
                {
                    size *= 1f + t.fireFlash * 0.6f;
                }
                img.rectTransform.sizeDelta = new Vector2(size, size);

                Color tint = Palette.TowerTint(t.HpRatio);
                if (t.level > 1)
                {
                    // 等级越高越亮（带一点金）
                    float k = (t.level - 1) / 4f;
                    tint = Color.Lerp(tint, Palette.GoldLight, k * 0.34f);
                }
                if (t.hitFlash > 0f)
                {
                    tint = Color.Lerp(tint, Color.white, 0.7f);
                }
                img.color = tint;
            }
            _towers.End();
        }

        private void SyncSpirits(float dt)
        {
            _spirits.Begin();
            for (int i = 0; i < _sim.Spirits.Count; i++)
            {
                SpiritUnit s = _sim.Spirits[i];
                Image img = _spirits.Use(s.id);
                img.sprite = SpriteLibrary.GetOrFallback(s.config.sprite, Palette.JadeLight);
                img.rectTransform.anchoredPosition = CellToLocal(s.cell);
                float size = _cell * 0.92f;
                if (s.fireFlash > 0f)
                {
                    size *= 1f + s.fireFlash * 0.6f;
                }
                img.rectTransform.sizeDelta = new Vector2(size, size);
                Color tint = Palette.TowerTint(s.HpRatio);
                // 快消失了就闪烁提醒
                if (s.LifeRatio < 0.25f)
                {
                    float blink = Mathf.PingPong(Time.unscaledTime * 3f, 1f);
                    tint = Color.Lerp(tint, new Color(1f, 0.85f, 0.6f, 1f), blink * 0.5f);
                }
                if (s.hitFlash > 0f)
                {
                    tint = Color.Lerp(tint, Color.white, 0.7f);
                }
                img.color = tint;
            }
            _spirits.End();
        }

        private void SyncTraps()
        {
            _traps.Begin();
            for (int i = 0; i < _sim.Traps.Count; i++)
            {
                TrapUnit t = _sim.Traps[i];
                Image img = _traps.Use(t.id);
                img.sprite = SpriteLibrary.GetOrFallback(t.config.sprite, Palette.Gold);
                img.rectTransform.anchoredPosition = CellToLocal(t.cell);
                float size = _cell * 0.62f;
                img.rectTransform.sizeDelta = new Vector2(size, size);
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f + t.id);
                img.color = new Color(1f, 1f, 1f, 0.72f + pulse * 0.3f);
            }
            _traps.End();
        }

        private void SyncProjectiles()
        {
            _projectiles.Begin();
            for (int i = 0; i < _sim.Projectiles.Count; i++)
            {
                ProjectileUnit p = _sim.Projectiles[i];
                Image img = _projectiles.Use(p.id);
                img.sprite = SpriteLibrary.GetOrFallback("fx_bolt", Palette.GoldLight);
                img.rectTransform.anchoredPosition = WorldToLocal(p.position);
                float size = _cell * 0.34f;
                img.rectTransform.sizeDelta = new Vector2(size, size);
                // 格子空间 y 朝下，本地空间 y 朝上，所以旋转角要取反
                img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -p.angle * Mathf.Rad2Deg);
                img.color = p.fromTower ? Palette.GoldLight : Palette.JadeLight;
            }
            _projectiles.End();
        }

        private void SyncEliteBars()
        {
            _barSeen.Clear();
            for (int i = 0; i < _sim.Enemies.Count; i++)
            {
                EnemyUnit e = _sim.Enemies[i];
                if (!e.alive || e.kind < EnemyKind.Greater)
                {
                    continue;
                }
                BarView bar = GetBar(e.id);
                bar.Active = true;
                _barSeen.Add(e.id);

                Vector2 local = WorldToLocal(e.position);
                float w = _cell * 0.86f;
                bool boss = e.kind == EnemyKind.Boss;
                float h = boss ? 12f : 8f;
                bar.Bg.rectTransform.sizeDelta = new Vector2(w, h);
                bar.Bg.rectTransform.anchoredPosition =
                    local + new Vector2(0f, _cell * (boss ? 0.62f : 0.52f));
                bar.Fill.rectTransform.sizeDelta = new Vector2(w - 3f, h - 3f);
                bar.Fill.fillAmount = e.HpRatio;
                bar.Fill.color = boss ? Palette.Cinnabar
                    : Color.Lerp(Palette.Gold, Palette.Cinnabar, 1f - e.HpRatio);
                bar.Bg.gameObject.SetActive(true);
                bar.Fill.gameObject.SetActive(true);
            }

            _barStale.Clear();
            foreach (KeyValuePair<int, BarView> kv in _bars)
            {
                if (!kv.Value.Active)
                {
                    _barStale.Add(kv.Key);
                }
            }
            for (int i = 0; i < _barStale.Count; i++)
            {
                BarView bar = _bars[_barStale[i]];
                bar.Bg.gameObject.SetActive(false);
                bar.Fill.gameObject.SetActive(false);
                bar.Active = false;
                bar.Bg.transform.SetParent(Root, false);
                _barPool.Add(bar);
                _bars.Remove(_barStale[i]);
            }
            for (int i = 0; i < _barSeen.Count; i++)
            {
                BarView bar = _bars[_barSeen[i]];
                if (bar != null)
                {
                    bar.Active = false;
                }
            }
        }

        private BarView GetBar(int id)
        {
            BarView bar;
            if (_bars.TryGetValue(id, out bar))
            {
                return bar;
            }
            if (_barPool.Count > 0)
            {
                bar = _barPool[_barPool.Count - 1];
                _barPool.RemoveAt(_barPool.Count - 1);
            }
            else
            {
                bar = new BarView();
                Image bg = UiKit.NewImage(Root, "BarBg", SpriteLibrary.Get("ui_bar_bg"),
                    new Color(0.05f, 0.07f, 0.11f, 0.85f));
                bg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                bg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                bg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                Image fill = UiKit.NewImage(bg.transform, "Fill", SpriteLibrary.Get("ui_bar_fill"),
                    Palette.Gold);
                fill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                fill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                fill.rectTransform.pivot = new Vector2(0f, 0.5f);
                fill.rectTransform.anchoredPosition = Vector2.zero;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                bar.Bg = bg;
                bar.Fill = fill;
            }
            _bars[id] = bar;
            return bar;
        }

        private void SyncFx(float dt)
        {
            for (int i = 0; i < _fx.Count; i++)
            {
                FxView f = _fx[i];
                if (f.Img == null)
                {
                    continue;
                }
                if (f.Life <= 0f)
                {
                    if (f.Img.gameObject.activeSelf)
                    {
                        f.Img.gameObject.SetActive(false);
                    }
                    continue;
                }
                f.Life -= dt;
                float t = f.Duration > 0f ? 1f - Mathf.Clamp01(f.Life / f.Duration) : 1f;
                float size = Mathf.Lerp(f.StartSize, f.EndSize, t);
                f.Img.rectTransform.sizeDelta = new Vector2(size, size);
                Color c = f.Tint;
                c.a = f.Tint.a * (1f - t);
                f.Img.color = c;
                if (f.Spinning)
                {
                    f.Img.rectTransform.localRotation =
                        Quaternion.Euler(0f, 0f, t * 180f);
                }
            }
        }

        private void SyncTexts(float dt)
        {
            for (int i = 0; i < _texts.Count; i++)
            {
                DmgView v = _texts[i];
                if (v.Label == null)
                {
                    continue;
                }
                if (v.Life <= 0f)
                {
                    if (v.Label.gameObject.activeSelf)
                    {
                        v.Label.gameObject.SetActive(false);
                    }
                    continue;
                }
                v.Life -= dt;
                float t = v.Duration > 0f ? 1f - Mathf.Clamp01(v.Life / v.Duration) : 1f;
                v.Label.rectTransform.anchoredPosition = v.Start + v.Drift * t;
                Color c = v.Tint;
                c.a = 1f - t * t;
                v.Label.color = c;
            }
        }

        private void SyncShake(float dt)
        {
            if (_shakeTime <= 0f)
            {
                if (Root.anchoredPosition != _boardBasePos)
                {
                    Root.anchoredPosition = _boardBasePos;
                }
                return;
            }
            _shakeTime -= dt;
            float k = Mathf.Clamp01(_shakeTime / 0.22f) * _shakeStrength;
            Root.anchoredPosition = _boardBasePos + new Vector2(
                Random.Range(-k, k), Random.Range(-k, k));
            if (_shakeTime <= 0f)
            {
                _shakeStrength = 0f;
                Root.anchoredPosition = _boardBasePos;
            }
        }

        public void SetCoreAlarm(float ratio)
        {
            if (_coreGate == null)
            {
                return;
            }
            if (ratio > 0.999f)
            {
                _coreGate.color = Color.white;
                return;
            }
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (ratio < 0.35f ? 8f : 3f));
            Color warn = Color.Lerp(new Color(1f, 0.55f, 0.5f, 1f), Color.white, pulse * 0.3f);
            _coreGate.color = Color.Lerp(Color.white, warn, (1f - ratio) * 0.9f);
        }
    }
}
