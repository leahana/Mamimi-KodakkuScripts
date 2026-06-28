using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using KodakkuAssist.Module.Draw;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Module.GameOperate;
using KodakkuAssist.Script;
using Newtonsoft.Json;

namespace ErrerScriptNamespace
{
    [ScriptType(
        name: "[妖星乱舞绝境战]P5全流程绘制（地火细分版）",
        territorys: [1363],
        guid: "b09b4ae6-20c7-4d33-8a18-749eae12950d",
        version: "0.0.22",
        author: "Mamimi",
        note: "个人自用版，基于 Errer 原 P5 脚本，新增地火细分开关（落点圈/路线连线/指路位移线/危险圈段数可独立控制）。\n" +
              "新增「只亮当前段」模式：隐藏点间连线、落点圈淡显作路点，仅分时亮箭头指引当前要走的一段，根治连线重叠。\n" +
              "P5全套：地火步进圈 + 钢铁月环 + 地水/洪水2穿1安全点 + 核爆/神圣分散 + 三星踩塔指路 + 地火安全点引导。")]
    public class YW_P5FireScript
    {
        public enum FireSafeStrategy
        {
            Auto,
            None,
            Center,
            Up,
            Down,
            Left,
            Right,
            LeftUp,
            RightUp,
            LeftDown,
            RightDown
        }

        #region Settings

        [UserSetting("-----地火步进圈-----")]
        public bool _____Fire_Settings_____ { get; set; } = true;

        [UserSetting("启用步进圈绘制")]
        public bool EnableFireDraw { get; set; } = true;

        [UserSetting("提前显示（读条结束前多少ms开始画）")]
        public int AdvanceDrawMs { get; set; } = 1500;

        [UserSetting("启用地火安全点引导（总开关）")]
        public bool EnableFireSafeGuide { get; set; } = true;

        [UserSetting("地火-显示落点圈")]
        public bool EnableFireSafePoint { get; set; } = true;

        [UserSetting("地火-显示走位路线连线")]
        public bool EnableFireRouteLine { get; set; } = true;

        [UserSetting("地火-显示指路位移线")]
        public bool EnableFireGuideLine { get; set; } = true;

        [UserSetting("地火-只亮当前段（隐藏点间连线、落点圈淡显，靠分时箭头指引）")]
        public bool FireRouteDimStatic { get; set; } = false;

        [UserSetting("地火-只亮当前段-落点圈淡显透明度(0~1)")]
        public float FireRouteDimAlpha { get; set; } = 0.25f;

        [UserSetting("地火-危险圈段数")]
        public int FireStepCount { get; set; } = 7;

        [UserSetting("地火安全点方向")]
        public FireSafeStrategy FireSafeDirection { get; set; } = FireSafeStrategy.Auto;

        [UserSetting("地火安全点颜色")]
        public ScriptColor FireSafeColor { get; set; } = new ScriptColor { V4 = new Vector4(0f, 1f, 0.4f, 1f) };

        [UserSetting("地火路线颜色")]
        public ScriptColor FireRouteColor { get; set; } = new ScriptColor { V4 = new Vector4(0f, 1f, 1f, 1f) };

        [UserSetting("地火安全点Debug")]
        public bool FireSafeDebug { get; set; } = false;

        [UserSetting("-----钢铁/月环（二选一的灾祟）-----")]
        public bool _____SteelDonut_Settings_____ { get; set; } = true;

        [UserSetting("启用钢铁月环绘制")]
        public bool EnableSteelDonut { get; set; } = true;

        [UserSetting("-----地水（矩形AOE）-----")]
        public bool _____Water_Settings_____ { get; set; } = true;

        [UserSetting("启用地水绘制")]
        public bool EnableWater { get; set; } = true;

        [UserSetting("地水颜色")]
        public ScriptColor WaterColor { get; set; } = new ScriptColor { V4 = new Vector4(1.0f, 1.0f, 0.0f, 0.35f) };

        [UserSetting("-----洪水2穿1安全点-----")]
        public bool _____FloodSafe_Settings_____ { get; set; } = true;

        [UserSetting("启用洪水2穿1安全点")]
        public bool EnableFloodSafe { get; set; } = true;

        [UserSetting("洪水安全点最大距离（米）")]
        public float FloodMaxDist { get; set; } = 8f;

        [UserSetting("启用洪水中间节点")]
        public bool EnableFloodMidNode { get; set; } = true;

        [UserSetting("洪水路线底色")]
        public ScriptColor FloodRouteInactiveColor { get; set; } = new ScriptColor { V4 = new Vector4(0.35f, 0.35f, 0.35f, 0.65f) };

        [UserSetting("洪水路线起点颜色")]
        public ScriptColor FloodRouteStartColor { get; set; } = new ScriptColor { V4 = new Vector4(0f, 1f, 1f, 1f) };

        [UserSetting("洪水路线中转颜色")]
        public ScriptColor FloodRouteMidColor { get; set; } = new ScriptColor { V4 = new Vector4(0f, 1f, 0.4f, 1f) };

        [UserSetting("洪水路线终点颜色")]
        public ScriptColor FloodRouteFinalColor { get; set; } = new ScriptColor { V4 = new Vector4(0f, 1f, 0f, 1f) };

        [UserSetting("Debug：打印洪水交点坐标")]
        public bool FloodDebug { get; set; } = false;

        [UserSetting("-----核爆/分散（癫狂交响曲）-----")]
        public bool _____BusterSpread_Settings_____ { get; set; } = true;

        [UserSetting("启用核爆分散绘制")]
        public bool EnableBusterSpread { get; set; } = true;

        [UserSetting("-----咏唱中分散圈-----")]
        public bool _____CastSpread_Settings_____ { get; set; } = true;

        [UserSetting("启用咏唱中分散圈")]
        public bool EnableCastSpread { get; set; } = true;

        [UserSetting("咏唱中分散圈颜色")]
        public ScriptColor CastSpreadColor { get; set; } = new ScriptColor { V4 = new Vector4(1.0f, 1.0f, 0.0f, 0.35f) };

        [UserSetting("-----三星踩塔指路-----")]
        public bool _____Tower_Settings_____ { get; set; } = true;

        [UserSetting("启用三星踩塔指路")]
        public bool EnableTowerGuide { get; set; } = true;

        [UserSetting("踩塔只指路自己")]
        public bool TowerGuideSelfOnly { get; set; } = true;

        [UserSetting("踩塔头部标记")]
        public bool TowerEnableMark { get; set; } = false;

        [UserSetting("踩塔Debug")]
        public bool TowerDebug { get; set; } = false;

        [UserSetting("踩塔分配最大等待ms")]
        public int TowerAssignMaxWaitMs { get; set; } = 2500;

        [UserSetting("踩塔线色")]
        public ScriptColor TowerGuideColor { get; set; } = new ScriptColor { V4 = new Vector4(0f, 1f, 1f, 1f) };

        [UserSetting("-----颜色-----")]
        public bool _____Color_Settings_____ { get; set; } = true;

        [UserSetting("地火危险色")]
        public ScriptColor FireDangerColor { get; set; } = new ScriptColor { V4 = new Vector4(1.0f, 0.3f, 0.0f, 0.35f) };

        [UserSetting("钢铁危险色")]
        public ScriptColor SteelColor { get; set; } = new ScriptColor { V4 = new Vector4(1.0f, 0.0f, 0.0f, 0.35f) };

        [UserSetting("月环安全色（内圈）")]
        public ScriptColor DonutSafeColor { get; set; } = new ScriptColor { V4 = new Vector4(0.0f, 1.0f, 0.0f, 0.25f) };

        [UserSetting("月环危险色（外圈）")]
        public ScriptColor DonutDangerColor { get; set; } = new ScriptColor { V4 = new Vector4(1.0f, 0.0f, 0.0f, 0.35f) };

        [UserSetting("核爆危险色")]
        public ScriptColor BusterColor { get; set; } = new ScriptColor { V4 = new Vector4(1.0f, 0.0f, 0.0f, 0.35f) };

        [UserSetting("神圣黄色")]
        public ScriptColor SpreadColor { get; set; } = new ScriptColor { V4 = new Vector4(1.0f, 1.0f, 0.0f, 0.35f) };

        #endregion

        #region State

        private const string DrawPrefix = "Errer_YW_P5Fire";
        private const string FireSafePrefix = DrawPrefix + "_Safe";
        private const string TowerPrefix = "Errer_P5T";
        private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);
        private static readonly Vector3 TowerAnchorA = new(100f, 0f, 85f);

        private const DrawModeEnum DM = DrawModeEnum.Default;
        private const DrawModeEnum DMG = DrawModeEnum.Imgui;
        private const int TowerGuideDurationMs = 6500;
        private const float FireCircleRadius = 6f;
        private const float FireStepDistance = 7f;
        private const int FireFirstExplosionDelayMs = 700;
        private const int FireExplosionIntervalMs = 700;
        private const int FireAfterExplosionKeepMs = 0;
        private const int FireHitBaseMs = 4069;
        private const int FireHitIntervalMs = 513;
        private const int FireWaveIntervalMs = 2500;
        private const int FireGuideDurationMs = 18000;
        private const int FireInitialPointDurationMs = 10000;
        private const float FireSafeCenterRadius = 1.6f;
        private const float FireRoutePointRadius = 1.25f;
        private const float FireRouteOffset = 5f;
        private const float FireDiagonalComponentOffset = 2.5f;
        private const float FireRouteLineWidth = 0.8f;
        private const float FireGuideWidth = 1.5f;
        private const float SteelRadius = 10f;
        private const float DonutInnerRadius = 10f;
        private const float DonutOuterRadius = 50f;
        private const int SteelDonutDurationMs = 8000;
        private const int WaterDelayMs = 4000;
        private const int WaterDurationMs = 1800;
        private const float WaterWidth = 10f;
        private const float WaterLength = 60f;
        private const float WaterBackOffset = 30f;
        private const float FloodSafeDistance = 13.5f;
        private const int Flood2DelayMs = 0;
        private const int Flood2DurationMs = 3500;
        private const int Flood1DelayMs = 3500;
        private const int Flood1DurationMs = 3000;
        private const int FloodMidNodeDurationMs = 350;
        private const float BusterRadius = 25f;
        private const int BusterDurationMs = 4000;
        private const float SpreadRadius = 5f;
        private const int SpreadDurationMs = 5000;
        private const float CastSpreadRadius = 5f;
        private const int TowerNewDebuffMinRemainMs = 16000;
        private const int TowerNewDebuffMaxRemainMs = 20000;
        private const int TowerAssignPollMs = 100;

        private int _fireCount;
        private int _fireCandidateLeftCol;
        private int _fireCandidateRightCol;
        private Vector3 _fireSafeCenter = ArenaCenter;
        private bool _fireGuideInitialized;
        private bool _fireGuideReady;
        private bool _fireInitialPointDrawn;
        private bool _fireRouteCompleted;
        private bool _fireFirstHalfDrawn;
        private readonly List<int> _fireGuideCols = new();
        private readonly List<int> _fireGuideRels = new();

        private enum TowerElement { Fire = 2015294, Ice = 2015295, Thunder = 2015296 }

        private static readonly FireSafeStrategy[] FireAutoStrategies =
        {
            FireSafeStrategy.Center, FireSafeStrategy.Center,
            FireSafeStrategy.Left, FireSafeStrategy.Right,
            FireSafeStrategy.Center, FireSafeStrategy.Center,
            FireSafeStrategy.Up, FireSafeStrategy.Down
        };

        private static readonly MarkType[] TowerMarks =
        {
            MarkType.Attack1, MarkType.Attack2, MarkType.Attack3, MarkType.Attack4,
            MarkType.Stop1, MarkType.Stop2, MarkType.Bind1, MarkType.Bind2
        };

        private static readonly string[] TowerLabels = { "MT", "ST", "H1", "H2", "D1", "D2", "D3", "D4" };

        private readonly object _towerLock = new();
        private readonly Dictionary<Vector3, TowerElement> _towerRoundTowers = new();
        private readonly Dictionary<int, (TowerElement Element, DateTime ExpireAt)> _towerDebuffs = new();
        private readonly Dictionary<int, Vector3> _towerRawTargets = new();
        private readonly Dictionary<int, int> _towerMarkOrder = new();
        private readonly HashSet<TowerElement> _towerSeenDouble = new();
        private readonly HashSet<int> _towerIdlePlayers = new();
        private bool _towerCooling, _towerIdleLocked;
        private int _towerMechanicSeq;
        private int _towerRound;

        // ── 洪水2穿1：每4个读条为一组，异向配对 ──
        private int _floodCount;
        private int _floodGroupCount;
        private readonly Vector3[] _floodNegPos = new Vector3[2]; private readonly float[] _floodNegRot = new float[2]; private int _negCount;
        private readonly Vector3[] _floodPosPos = new Vector3[2]; private readonly float[] _floodPosRot = new float[2]; private int _posCount;
        private readonly float[] _floodSeqRot = new float[4]; private int _seqCount;
        private Vector3? _savedFloodSafe1;
        private Vector3? _savedFloodMid1;

        #endregion

        #region Init

        public void Init(ScriptAccessory accessory)
        {
            _fireCount = 0;
            ClearFireSafeGuideState();
            _floodCount = 0;
            _floodGroupCount = 0;
            _negCount = 0; _posCount = 0; _seqCount = 0;
            _savedFloodSafe1 = null;
            _savedFloodMid1 = null;
            ResetTowerGuideState(true);
            accessory.Method.RemoveDraw($"{DrawPrefix}_.*");
            accessory.Method.RemoveDraw($"{TowerPrefix}_.*");
        }

        #endregion

        #region 地火步进圈

        [ScriptMethod(name: "地火安全点初始化", eventType: EventTypeEnum.StartCasting,
            eventCondition: ["ActionId:47931"], userControl: false)]
        public void OnFireSafeGuideStart(Event @event, ScriptAccessory accessory)
        {
            InitializeFireSafeGuide(accessory, true);
        }

        [ScriptMethod(name: "地火步进圈", eventType: EventTypeEnum.StartCasting,
            eventCondition: ["ActionId:47932"], userControl: false)]
        public void OnFireCast(Event @event, ScriptAccessory accessory)
        {
            _fireCount++;
            var round = _fireCount;

            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            var rot = float.Parse(@event["SourceRotation"]);
            var castDurationMs = DurationMs(@event, 5000);
            var delayMs = Math.Max(0, castDurationMs - AdvanceDrawMs);

            if (EnableFireDraw)
            {
                var dir = RotationToDirection(rot);
                var fireColor = FireDangerColor.V4;
                var steps = Math.Clamp(FireStepCount, 1, 12);
                for (var i = 0; i < steps; i++)
                {
                    var explodeAtMs = castDurationMs + FireFirstExplosionDelayMs + FireExplosionIntervalMs * i;
                    var visibleDurationMs = Math.Max(1, explodeAtMs - delayMs + FireAfterExplosionKeepMs);
                    var circlePos = pos + dir * (FireStepDistance * i);
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = $"{DrawPrefix}_Fire{round}_Step{i}";
                    dp.Position = circlePos;
                    dp.Scale = new Vector2(FireCircleRadius);
                    dp.Color = fireColor;
                    dp.Delay = delayMs;
                    dp.DestoryAt = visibleDurationMs;
                    dp.ScaleMode = ScaleMode.None;
                    accessory.Method.SendDraw(DM, DrawTypeEnum.Circle, dp);
                }
            }

            if (EnableFireSafeGuide)
            {
                if (!_fireGuideInitialized)
                    InitializeFireSafeGuide(accessory, false);
                HandleFireSafeGuideCast(accessory, pos);
            }
        }

        private void InitializeFireSafeGuide(ScriptAccessory accessory, bool resetFireCount)
        {
            if (resetFireCount)
                _fireCount = 0;

            ClearFireSafeGuideState();
            _fireGuideInitialized = true;
            RemoveFireSafeGuideDraws(accessory);

            if (!EnableFireSafeGuide)
                return;

            var strategy = ResolveFireSafeStrategy(accessory);
            (_fireCandidateLeftCol, _fireCandidateRightCol) = FireCandidateColumns(strategy);
            _fireGuideReady = _fireCandidateLeftCol != 0 && _fireCandidateRightCol != 0;

            if (!_fireGuideReady)
            {
                if (FireSafeDebug)
                    accessory.Method.SendChat($"/e [地火安全] 方向={strategy}，不启用安全点引导");
                return;
            }

            _fireSafeCenter = FireSafeCenter(_fireCandidateLeftCol, _fireCandidateRightCol);
            DrawFirePoint(accessory, "Center", _fireSafeCenter, FireSafeCenterRadius, 0, FireGuideDurationMs, FireSafeColor.V4);

            if (FireSafeDebug)
                accessory.Method.SendChat($"/e [地火安全] 方向={strategy} L={_fireCandidateLeftCol} R={_fireCandidateRightCol} center=({_fireSafeCenter.X:F1},{_fireSafeCenter.Z:F1})");
        }

        private void ClearFireSafeGuideState()
        {
            _fireCandidateLeftCol = 0;
            _fireCandidateRightCol = 0;
            _fireSafeCenter = ArenaCenter;
            _fireGuideInitialized = false;
            _fireGuideReady = false;
            _fireInitialPointDrawn = false;
            _fireRouteCompleted = false;
            _fireFirstHalfDrawn = false;
            _fireGuideCols.Clear();
            _fireGuideRels.Clear();
        }

        private void HandleFireSafeGuideCast(ScriptAccessory accessory, Vector3 pos)
        {
            if (!_fireGuideReady)
                return;

            var col = FireInitialColumn(pos.X);
            if (col > 3)
                return;

            var isLeft = pos.X < ArenaCenter.X;
            var candidateCol = isLeft ? _fireCandidateLeftCol : _fireCandidateRightCol;
            if (candidateCol == 0)
                return;

            var rel = PositiveMod(col + 1 - candidateCol, 3);
            _fireGuideCols.Add(col);
            _fireGuideRels.Add(rel);
            var n = _fireGuideRels.Count;

            if (FireSafeDebug)
                accessory.Method.SendChat($"/e [地火安全] n={n} side={(isLeft ? "左" : "右")} cols={string.Join(":", _fireGuideCols)} rels={string.Join(":", _fireGuideRels)}");

            if (!_fireInitialPointDrawn && n <= 3 && rel != 0 && _fireGuideRels.Take(n - 1).All(r => r == 0))
            {
                DrawFireInitialPoint(accessory, n, rel);
                _fireInitialPointDrawn = true;
            }

            if (_fireRouteCompleted || n < 2)
                return;

            if (n == 2)
            {
                var rel1 = _fireGuideRels[0];
                var rel2 = _fireGuideRels[1];
                if (rel1 != 0 && rel2 != 0)
                    DrawFireFullRouteFrom12(accessory, rel1, rel2);
                else if (rel1 != 0 && rel2 == 0)
                    DrawFireFirstHalfRoute(accessory, rel1);
            }
            else if (n == 3)
            {
                var rel1 = _fireGuideRels[0];
                var rel2 = _fireGuideRels[1];
                var rel3 = _fireGuideRels[2];
                if (rel1 == 0 && rel2 != 0 && rel3 != 0)
                    DrawFireFullRouteFrom23(accessory, rel2, rel3);
            }
            else if (n == 4)
            {
                var rel1 = _fireGuideRels[0];
                var rel2 = _fireGuideRels[1];
                var rel3 = _fireGuideRels[2];
                var rel4 = _fireGuideRels[3];
                if (rel1 == 0 && rel2 == 0 && rel3 != 0 && rel4 != 0)
                    DrawFireFullRouteFrom34(accessory, rel3, rel4);
                else if (_fireFirstHalfDrawn && rel1 != 0 && rel2 == 0 && rel4 != 0)
                    DrawFireSecondHalfRoute(accessory, rel1, rel4);
            }
        }

        private void DrawFireFullRouteFrom12(ScriptAccessory accessory, int rel1, int rel2)
        {
            var clockwise = rel1 == rel2;
            var dir4 = FireDir4FromRelPair(rel1, rel2);
            var lateSafeCol = _fireCandidateRightCol + (rel2 == 1 ? 1 : 0);
            var earlyDangerCol = _fireCandidateLeftCol + (rel1 == 1 ? 0 : 1);
            DrawFireFullRoute(accessory, "Full12", dir4, clockwise, FireDelayForColumn(lateSafeCol) - FireWaveIntervalMs, FireDelayForColumn(earlyDangerCol));
        }

        private void DrawFireFullRouteFrom23(ScriptAccessory accessory, int rel2, int rel3)
        {
            var clockwise = rel2 != rel3;
            var dir4 = FireDir4FromRelPair(rel3, rel2);
            var lateSafeCol = _fireCandidateLeftCol + (rel3 == 1 ? 1 : 0);
            var earlyDangerCol = _fireCandidateRightCol + (rel2 == 1 ? 0 : 1);
            DrawFireFullRoute(accessory, "Full23", dir4, clockwise, FireDelayForColumn(lateSafeCol) - FireWaveIntervalMs, FireDelayForColumn(earlyDangerCol));
        }

        private void DrawFireFullRouteFrom34(ScriptAccessory accessory, int rel3, int rel4)
        {
            var clockwise = rel3 == rel4;
            var dir4 = FireDir4FromRelPair(rel3, rel4);
            var lateSafeCol = _fireCandidateRightCol + (rel4 == 1 ? 1 : 0);
            var earlyDangerCol = _fireCandidateLeftCol + (rel3 == 1 ? 0 : 1);
            DrawFireFullRoute(accessory, "Full34", dir4, clockwise, FireDelayForColumn(lateSafeCol) - FireWaveIntervalMs, FireDelayForColumn(earlyDangerCol));
        }

        private void DrawFireFirstHalfRoute(ScriptAccessory accessory, int rel1)
        {
            var dirN4 = rel1 == 1 ? 3 : 1;
            var from = FireDiagonalPoint(dirN4);
            var to = _fireSafeCenter + (_fireSafeCenter - from);
            var delay = Math.Max(0, FireDelayForColumn(_fireCandidateRightCol + 1) - FireWaveIntervalMs);
            var total = Math.Max(FireGuideDurationMs, delay + 5000);

            DrawFirePoint(accessory, "FirstHalf_From", from, FireRoutePointRadius, 0, total, FireRouteColor.V4);
            DrawFirePoint(accessory, "FirstHalf_To", to, FireRoutePointRadius, 0, total, FireSafeColor.V4);
            DrawFireRouteLine(accessory, "FirstHalf_Line", from, to, 0, total, FireRouteColor.V4);
            DrawFireGuideTo(accessory, "FirstHalf_Wait", from, 0, Math.Max(1, delay), FireRouteColor.V4);
            DrawFireGuideTo(accessory, "FirstHalf_Cross", to, delay, Math.Max(1, total - delay), FireSafeColor.V4);
            _fireFirstHalfDrawn = true;

            if (FireSafeDebug)
                accessory.Method.SendChat($"/e [地火安全] 路线14前半 dirN4={dirN4} delay={delay}ms");
        }

        private void DrawFireSecondHalfRoute(ScriptAccessory accessory, int rel1, int rel4)
        {
            var clockwise = rel1 == rel4;
            var dir4 = FireDir4FromRelPair(rel1, rel4);
            var route = FireFullRouteCardinals(dir4, clockwise);
            var p2 = FireCardinalPoint(route[1]);
            var p3 = FireCardinalPoint(route[2]);
            var delay = Math.Max(0, FireDelayForColumn(_fireCandidateLeftCol + (rel1 == 1 ? 0 : 1)));
            var total = Math.Max(FireGuideDurationMs, delay + 5000);

            DrawFirePoint(accessory, "SecondHalf_P2", p2, FireRoutePointRadius, 0, total, FireRouteColor.V4);
            DrawFirePoint(accessory, "SecondHalf_P3", p3, FireRoutePointRadius, 0, total, FireSafeColor.V4);
            DrawFireRouteLine(accessory, "SecondHalf_Line", p2, p3, 0, total, FireRouteColor.V4);
            DrawFireGuideTo(accessory, "SecondHalf_Wait", p2, 0, Math.Max(1, delay), FireRouteColor.V4);
            DrawFireGuideTo(accessory, "SecondHalf_Cross", p3, delay, Math.Max(1, total - delay), FireSafeColor.V4);
            _fireRouteCompleted = true;

            if (FireSafeDebug)
                accessory.Method.SendChat($"/e [地火安全] 路线14后半 dir4={dir4} clockwise={clockwise} delay={delay}ms");
        }

        private void DrawFireInitialPoint(ScriptAccessory accessory, int n, int rel)
        {
            var dirN4 = n == 2 ? (rel == 1 ? 2 : 0) : (rel == 1 ? 3 : 1);
            var pos = FireDiagonalPoint(dirN4);
            DrawFirePoint(accessory, "InitialPoint", pos, FireRoutePointRadius, 0, FireInitialPointDurationMs, FireRouteColor.V4);
            DrawFireGuideTo(accessory, "InitialGuide", pos, 0, FireInitialPointDurationMs, FireRouteColor.V4);

            if (FireSafeDebug)
                accessory.Method.SendChat($"/e [地火安全] 初始斜点 n={n} rel={rel} dirN4={dirN4}");
        }

        private void DrawFireFullRoute(ScriptAccessory accessory, string suffix, int dir4, bool clockwise, int delay1, int delay2)
        {
            RemoveFireInitialPointDraws(accessory);

            delay1 = Math.Max(0, delay1);
            delay2 = Math.Max(delay1 + 1, delay2);
            var total = Math.Max(FireGuideDurationMs, delay2 + 5000);
            var route = FireFullRouteCardinals(dir4, clockwise);
            var p1 = FireCardinalPoint(route[0]);
            var p2 = FireCardinalPoint(route[1]);
            var p3 = FireCardinalPoint(route[2]);

            DrawFirePoint(accessory, $"{suffix}_P1", p1, FireRoutePointRadius, 0, total, FireRouteColor.V4);
            DrawFirePoint(accessory, $"{suffix}_P2", p2, FireRoutePointRadius, 0, total, FireRouteColor.V4);
            DrawFirePoint(accessory, $"{suffix}_P3", p3, FireRoutePointRadius, 0, total, FireSafeColor.V4);
            DrawFireRouteLine(accessory, $"{suffix}_Line12", p1, p2, 0, total, FireRouteColor.V4);
            DrawFireRouteLine(accessory, $"{suffix}_Line23", p2, p3, 0, total, FireSafeColor.V4);
            DrawFireGuideTo(accessory, $"{suffix}_Guide1", p1, 0, Math.Max(1, delay1), FireRouteColor.V4);
            DrawFireGuideTo(accessory, $"{suffix}_Guide2", p2, delay1, Math.Max(1, delay2 - delay1), FireRouteColor.V4);
            DrawFireGuideTo(accessory, $"{suffix}_Guide3", p3, delay2, Math.Max(1, total - delay2), FireSafeColor.V4);
            _fireRouteCompleted = true;

            if (FireSafeDebug)
                accessory.Method.SendChat($"/e [地火安全] {suffix} dir4={dir4} clockwise={clockwise} delay1={delay1}ms delay2={delay2}ms");
        }

        // 「只亮当前段」模式：点间连线直接不画（见 DrawFireRouteLine），落点圈按透明度淡显作路点参考，
        // 动态分时亮箭头（DrawFireGuideTo）保持原色 → 任一时刻只有当前段醒目，且无连线重叠。
        private Vector4 MaybeDimStatic(Vector4 color)
        {
            if (!FireRouteDimStatic)
                return color;
            return new Vector4(color.X, color.Y, color.Z, color.W * Math.Clamp(FireRouteDimAlpha, 0f, 1f));
        }

        private void DrawFirePoint(ScriptAccessory accessory, string suffix, Vector3 pos, float radius, int delayMs, int durationMs, Vector4 color)
        {
            if (!EnableFireSafePoint)
                return;
            if (durationMs <= 0)
                return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"{FireSafePrefix}_{suffix}";
            dp.Position = pos;
            dp.Scale = new Vector2(radius);
            dp.Color = MaybeDimStatic(color);
            dp.Delay = delayMs;
            dp.DestoryAt = durationMs;
            dp.ScaleMode = ScaleMode.None;
            accessory.Method.SendDraw(DM, DrawTypeEnum.Circle, dp);
        }

        private void DrawFireGuideTo(ScriptAccessory accessory, string suffix, Vector3 target, int delayMs, int durationMs, Vector4 color)
        {
            if (!EnableFireGuideLine)
                return;
            if (durationMs <= 0)
                return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"{FireSafePrefix}_{suffix}";
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = target;
            dp.Scale = new Vector2(FireGuideWidth);
            dp.ScaleMode = ScaleMode.YByDistance;
            dp.Color = color;
            dp.Delay = delayMs;
            dp.DestoryAt = durationMs;
            accessory.Method.SendDraw(DMG, DrawTypeEnum.Displacement, dp);
        }

        private void DrawFireRouteLine(ScriptAccessory accessory, string suffix, Vector3 from, Vector3 to, int delayMs, int durationMs, Vector4 color)
        {
            // 「只亮当前段」模式：点间静态连线是 ②③④ 共线折返重叠的根源，直接不画，只靠分时亮箭头指引。
            if (!EnableFireRouteLine || FireRouteDimStatic)
                return;
            if (durationMs <= 0)
                return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"{FireSafePrefix}_{suffix}";
            dp.Position = from;
            dp.TargetPosition = to;
            dp.Scale = new Vector2(FireRouteLineWidth);
            dp.ScaleMode = ScaleMode.YByDistance;
            dp.Color = color;
            dp.Delay = delayMs;
            dp.DestoryAt = durationMs;
            accessory.Method.SendDraw(DMG, DrawTypeEnum.Displacement, dp);
        }

        private void RemoveFireSafeGuideDraws(ScriptAccessory accessory)
        {
            accessory.Method.RemoveDraw($"{FireSafePrefix}_.*");
        }

        private void RemoveFireInitialPointDraws(ScriptAccessory accessory)
        {
            accessory.Method.RemoveDraw($"{FireSafePrefix}_Initial.*");
        }

        private FireSafeStrategy ResolveFireSafeStrategy(ScriptAccessory accessory)
        {
            if (FireSafeDirection != FireSafeStrategy.Auto)
                return FireSafeDirection;

            var partyIndex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            return partyIndex >= 0 && partyIndex < FireAutoStrategies.Length ? FireAutoStrategies[partyIndex] : FireSafeStrategy.Center;
        }

        private static (int Left, int Right) FireCandidateColumns(FireSafeStrategy strategy)
        {
            return strategy switch
            {
                FireSafeStrategy.Center => (3, 3),
                FireSafeStrategy.Up => (4, 2),
                FireSafeStrategy.Down => (2, 4),
                FireSafeStrategy.Left => (2, 2),
                FireSafeStrategy.Right => (4, 4),
                FireSafeStrategy.LeftUp => (3, 1),
                FireSafeStrategy.RightUp => (5, 3),
                FireSafeStrategy.LeftDown => (1, 3),
                FireSafeStrategy.RightDown => (3, 5),
                _ => (0, 0)
            };
        }

        private static Vector3 FireSafeCenter(int leftCol, int rightCol)
        {
            return new Vector3(70f + 5f * leftCol + 5f * rightCol, 0f, 100f - 5f * leftCol + 5f * rightCol);
        }

        private static int FireInitialColumn(float x)
        {
            return PositiveMod((int)MathF.Round((x - 70f) / 5f), 7) + 1;
        }

        private static int FireDelayForColumn(int col)
        {
            return FireHitBaseMs + FireHitIntervalMs * col;
        }

        private static int FireDir4FromRelPair(int firstRel, int secondRel)
        {
            return (firstRel, secondRel) switch
            {
                (1, 1) => 3,
                (1, 2) => 0,
                (2, 1) => 2,
                (2, 2) => 1,
                _ => 0
            };
        }

        private static int[] FireFullRouteCardinals(int dir4, bool clockwise)
        {
            return (dir4, clockwise) switch
            {
                (0, false) => new[] { 0, 1, 2 },
                (0, true) => new[] { 0, 3, 2 },
                (1, false) => new[] { 1, 2, 3 },
                (1, true) => new[] { 1, 0, 3 },
                (2, false) => new[] { 2, 3, 0 },
                (2, true) => new[] { 2, 1, 0 },
                (3, false) => new[] { 3, 0, 1 },
                (3, true) => new[] { 3, 2, 1 },
                _ => new[] { 0, 1, 2 }
            };
        }

        private Vector3 FireCardinalPoint(int cardinal)
        {
            return _fireSafeCenter + FireCardinalOffset(cardinal);
        }

        private static Vector3 FireCardinalOffset(int cardinal)
        {
            return cardinal switch
            {
                0 => new Vector3(0f, 0f, -FireRouteOffset),
                1 => new Vector3(-FireRouteOffset, 0f, 0f),
                2 => new Vector3(0f, 0f, FireRouteOffset),
                3 => new Vector3(FireRouteOffset, 0f, 0f),
                _ => Vector3.Zero
            };
        }

        private Vector3 FireDiagonalPoint(int dirN4)
        {
            var offset = dirN4 switch
            {
                0 => new Vector3(-FireDiagonalComponentOffset, 0f, -FireDiagonalComponentOffset),
                1 => new Vector3(-FireDiagonalComponentOffset, 0f, FireDiagonalComponentOffset),
                2 => new Vector3(FireDiagonalComponentOffset, 0f, FireDiagonalComponentOffset),
                3 => new Vector3(FireDiagonalComponentOffset, 0f, -FireDiagonalComponentOffset),
                _ => Vector3.Zero
            };
            return _fireSafeCenter + offset;
        }

        #endregion

        #region 钢铁/月环（二选一的灾祟）

        [ScriptMethod(name: "钢铁（地震）", eventType: EventTypeEnum.StartCasting,
            eventCondition: ["ActionId:49742"], userControl: false)]
        public void OnSteel(Event @event, ScriptAccessory accessory)
        {
            if (!EnableSteelDonut) return;
            if (!TryParseObjectId(@event["SourceId"], out var sid)) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"{DrawPrefix}_Steel";
            dp.Owner = sid;
            dp.Scale = new Vector2(SteelRadius);
            dp.Color = SteelColor.V4;
            dp.DestoryAt = SteelDonutDurationMs;
            dp.ScaleMode = ScaleMode.None;
            accessory.Method.SendDraw(DM, DrawTypeEnum.Circle, dp);
        }

        [ScriptMethod(name: "月环（龙卷）安全+危险", eventType: EventTypeEnum.StartCasting,
            eventCondition: ["ActionId:49743"], userControl: false)]
        public void OnDonut(Event @event, ScriptAccessory accessory)
        {
            if (!EnableSteelDonut) return;
            if (!TryParseObjectId(@event["SourceId"], out var sid)) return;

            // 内圈安全区（绿色空心）
            var dpSafe = accessory.Data.GetDefaultDrawProperties();
            dpSafe.Name = $"{DrawPrefix}_DonutSafe";
            dpSafe.Owner = sid;
            dpSafe.Scale = new Vector2(DonutInnerRadius);
            dpSafe.Color = DonutSafeColor.V4;
            dpSafe.DestoryAt = SteelDonutDurationMs;
            dpSafe.ScaleMode = ScaleMode.None;
            accessory.Method.SendDraw(DM, DrawTypeEnum.Circle, dpSafe);

            // 外圈危险区（红色月环）
            var dpDanger = accessory.Data.GetDefaultDrawProperties();
            dpDanger.Name = $"{DrawPrefix}_DonutDanger";
            dpDanger.Owner = sid;
            dpDanger.Scale = new Vector2(DonutOuterRadius);
            dpDanger.InnerScale = new Vector2(DonutInnerRadius);
            dpDanger.Radian = MathF.PI * 2f;
            dpDanger.Color = DonutDangerColor.V4;
            dpDanger.DestoryAt = SteelDonutDurationMs;
            dpDanger.ScaleMode = ScaleMode.None;
            accessory.Method.SendDraw(DM, DrawTypeEnum.Donut, dpDanger);
        }

        #endregion

        #region 咏唱中分散圈

        [ScriptMethod(name: "咏唱中分散圈", eventType: EventTypeEnum.StartCasting,
            eventCondition: ["ActionId:47934"], userControl: false)]
        public void OnCastSpread(Event @event, ScriptAccessory accessory)
        {
            RemoveFireSafeGuideDraws(accessory);

            if (!EnableCastSpread) return;

            var durationMs = DurationMs(@event, 4700) + 1000;
            for (var i = 0; i < Math.Min(8, accessory.Data.PartyList.Count); i++)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = $"{DrawPrefix}_CastSpread{i}";
                dp.Owner = accessory.Data.PartyList[i];
                dp.Scale = new Vector2(CastSpreadRadius);
                dp.Color = CastSpreadColor.V4;
                dp.DestoryAt = durationMs;
                dp.ScaleMode = ScaleMode.None;
                accessory.Method.SendDraw(DM, DrawTypeEnum.Circle, dp);
            }
        }

        #endregion

        #region 三星踩塔指路

        [ScriptMethod(name: "三星踩塔机制开始", eventType: EventTypeEnum.StartCasting,
            eventCondition: ["ActionId:47938"], userControl: false)]
        public void OnTowerMechanicStart(Event @event, ScriptAccessory accessory)
        {
            if (!EnableTowerGuide) return;

            int seq;
            lock (_towerLock)
            {
                ClearTowerGuideState(false);
                _towerCooling = false;
                _towerIdleLocked = false;
                seq = ++_towerMechanicSeq;
                _towerRound = 0;
            }

            accessory.Method.RemoveDraw($"{TowerPrefix}_.*");
            var delay = DurationMs(@event, 4700) + 1000;
            if (TowerDebug) accessory.Method.SendChat($"/e [塔] 机制开始，{delay}ms 后锁定闲人");
            _ = Task.Run(async () => { await Task.Delay(delay); LockTowerIdlePlayers(accessory, seq); });
        }

        [ScriptMethod(name: "三星踩塔塔收集", eventType: EventTypeEnum.ObjectEffect,
            eventCondition: ["Id1:16"], userControl: false)]
        public void OnTowerObjectEffect(Event @event, ScriptAccessory accessory)
        {
            if (!EnableTowerGuide) return;

            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            if (!TryParseObjectId(@event["SourceId"], out var sourceId)) return;
            var obj = accessory.Data.Objects.SearchById(sourceId);
            if (obj == null) return;
            var dataId = obj.DataId;
            if (dataId is not (2015294 or 2015295 or 2015296)) return;

            bool shouldAssign;
            int count;
            lock (_towerLock)
            {
                _towerRoundTowers.TryAdd(RoundTowerPos(pos), (TowerElement)dataId);
                count = _towerRoundTowers.Count;
                shouldAssign = count >= 4 && !_towerCooling;
                if (shouldAssign) _towerCooling = true;
            }

            if (TowerDebug) accessory.Method.SendChat($"/e [塔] {DateTime.Now:HH:mm:ss.fff} {TowerElementLabel((TowerElement)dataId)} ({pos.X:F1},{pos.Z:F1}) n={count}");
            if (shouldAssign) _ = Task.Run(async () => await WaitAndAssignTowerGuides(accessory));
        }

        [ScriptMethod(name: "三星踩塔易伤", eventType: EventTypeEnum.StatusAdd,
            eventCondition: ["StatusID:regex:^(2902|2903|2998)$"], userControl: false)]
        public void OnTowerDebuff(Event @event, ScriptAccessory accessory)
        {
            if (!EnableTowerGuide) return;
            if (!TryParseObjectId(@event["TargetId"], out var targetId)) return;

            var partyIndex = accessory.Data.PartyList.IndexOf(targetId);
            if (partyIndex < 0) return;

            var element = int.Parse(@event["StatusID"]) switch
            {
                2902 => TowerElement.Fire,
                2903 => TowerElement.Ice,
                2998 => TowerElement.Thunder,
                _ => TowerElement.Fire
            };
            var expireAt = DateTime.Now.AddMilliseconds(DurationMs(@event, 20000));
            lock (_towerLock) _towerDebuffs[partyIndex] = (element, expireAt);
            if (TowerDebug) accessory.Method.SendChat($"/e [易伤] {TowerPlayerLabel(partyIndex)} → {TowerElementLabel(element)} until {expireAt:HH:mm:ss.fff}");
        }

        private async Task WaitAndAssignTowerGuides(ScriptAccessory accessory)
        {
            var waited = 0;
            var pollMs = Math.Max(50, TowerAssignPollMs);
            var maxWaitMs = Math.Max(0, TowerAssignMaxWaitMs);
            while (waited < maxWaitMs)
            {
                if (CountCurrentTowerDebuffs() >= 6) break;
                await Task.Delay(pollMs);
                waited += pollMs;
            }

            if (TowerDebug) accessory.Method.SendChat($"/e [塔] 等待新易伤 {waited}ms count={CountCurrentTowerDebuffs()}/6");
            AssignTowerGuides(accessory);
        }

        private void AssignTowerGuides(ScriptAccessory accessory)
        {
            List<(Vector3 Pos, TowerElement Element, float Angle)> towers;
            Dictionary<int, (TowerElement Element, DateTime ExpireAt)> debuffs;
            int round;
            lock (_towerLock)
            {
                towers = _towerRoundTowers.Select(kv => (kv.Key, kv.Value, TowerAngle(kv.Key))).OrderBy(t => t.Item3).ToList();
                _towerRoundTowers.Clear();
                debuffs = _towerDebuffs.ToDictionary(kv => kv.Key, kv => kv.Value);
                round = ++_towerRound;
                _towerRawTargets.Clear();
                _towerMarkOrder.Clear();
                _towerCooling = false;
            }

            if (towers.Count != 4)
            {
                if (TowerDebug) accessory.Method.SendChat($"/e [塔] 第{round}轮塔数异常 {towers.Count}，跳过");
                return;
            }

            var groups = towers.GroupBy(t => t.Element).ToDictionary(g => g.Key, g => g.ToList());
            if (groups.Count != 3 || groups.Values.Count(g => g.Count == 2) != 1)
            {
                if (TowerDebug) accessory.Method.SendChat($"/e [塔] 第{round}轮元素分布异常 {string.Join(" ", groups.Select(g => $"{TowerElementLabel(g.Key)}x{g.Value.Count}"))}");
                return;
            }

            var doubleElement = groups.First(g => g.Value.Count == 2).Key;
            if (!_towerSeenDouble.Add(doubleElement) && TowerDebug) accessory.Method.SendChat($"/e [塔] 双塔元素重复：{TowerElementLabel(doubleElement)}");
            if (TowerDebug)
            {
                accessory.Method.SendChat($"/e ═══ 第{round}轮 塔{towers.Count} 双{TowerElementLabel(doubleElement)} ═══");
                foreach (var t in towers) accessory.Method.SendChat($"/e   {TowerElementLabel(t.Element)}塔 @({t.Pos.X:F1},{t.Pos.Z:F1}) ang={t.Angle:F2}");
            }

            var used = new HashSet<int>();
            var markIndex = 0;
            void Put(int partyIndex, Vector3 pos, string label)
            {
                if (markIndex >= TowerMarks.Length || partyIndex < 0 || partyIndex >= accessory.Data.PartyList.Count || !used.Add(partyIndex)) return;
                lock (_towerLock)
                {
                    _towerRawTargets[partyIndex] = pos;
                    _towerMarkOrder[partyIndex] = markIndex;
                }
                DrawTowerGuide(accessory, partyIndex, pos);
                if (TowerDebug) accessory.Method.SendChat($"/e   分配 {TowerPlayerLabel(partyIndex)} debuff:{(debuffs.TryGetValue(partyIndex, out var d) ? TowerElementLabel(d.Element) : "无")} → {label}");
                markIndex++;
            }

            var idlePlayers = GetTowerIdlePlayers();
            var now = DateTime.Now;
            var activeDebuffs = debuffs
                .Where(kv => !idlePlayers.Contains(kv.Key))
                .Where(kv =>
                {
                    var remainMs = (kv.Value.ExpireAt - now).TotalMilliseconds;
                    return remainMs >= TowerNewDebuffMinRemainMs && remainMs <= TowerNewDebuffMaxRemainMs;
                })
                .OrderBy(kv => kv.Key)
                .ToList();
            if (TowerDebug) accessory.Method.SendChat($"/e [塔] 第{round}轮使用新易伤 {activeDebuffs.Count}/6 remain={TowerNewDebuffMinRemainMs}-{TowerNewDebuffMaxRemainMs}ms");
            foreach (var kv in activeDebuffs)
            {
                var target = ClockwiseFirstDifferentTower(towers, kv.Value.Element);
                if (target.HasValue) Put(kv.Key, target.Value.Pos, $"顺异:{TowerElementLabel(target.Value.Element)}");
            }

            var idleTarget = CounterClockwiseFromTowerAnchor(towers, doubleElement);
            foreach (var partyIndex in idlePlayers)
            {
                if (markIndex >= TowerMarks.Length) break;
                if (!used.Contains(partyIndex)) Put(partyIndex, idleTarget.Pos, $"逆双:{TowerElementLabel(doubleElement)}");
            }

            if (TowerDebug) TowerSummary(accessory, towers);
        }

        private int CountCurrentTowerDebuffs()
        {
            var idlePlayers = GetTowerIdlePlayers();
            var now = DateTime.Now;
            lock (_towerLock)
            {
                return _towerDebuffs
                    .Where(kv => !idlePlayers.Contains(kv.Key))
                    .Count(kv =>
                    {
                        var remainMs = (kv.Value.ExpireAt - now).TotalMilliseconds;
                        return remainMs >= TowerNewDebuffMinRemainMs && remainMs <= TowerNewDebuffMaxRemainMs;
                    });
            }
        }

        private void DrawTowerGuide(ScriptAccessory accessory, int partyIndex, Vector3 pos)
        {
            if (partyIndex >= accessory.Data.PartyList.Count) return;
            var owner = accessory.Data.PartyList[partyIndex];
            if (TowerGuideSelfOnly && owner != accessory.Data.Me) return;

            int markIndex;
            lock (_towerLock) markIndex = _towerMarkOrder.GetValueOrDefault(partyIndex, 0);
            if (TowerEnableMark) accessory.Method.Mark(owner, TowerMarks[Math.Clamp(markIndex, 0, TowerMarks.Length - 1)]);

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"{TowerPrefix}_g{partyIndex}";
            dp.Owner = owner;
            dp.TargetPosition = pos;
            dp.Scale = new Vector2(1.5f);
            dp.ScaleMode = ScaleMode.YByDistance;
            dp.Color = TowerGuideColor.V4;
            dp.DestoryAt = TowerGuideDurationMs;
            accessory.Method.SendDraw(DMG, DrawTypeEnum.Displacement, dp);
        }

        private void LockTowerIdlePlayers(ScriptAccessory accessory, int seq)
        {
            List<int> idle;
            int debuffCount;
            lock (_towerLock)
            {
                if (seq != _towerMechanicSeq || _towerIdleLocked) return;
                var now = DateTime.Now.AddMilliseconds(3000);
                var active = _towerDebuffs.Where(kv => kv.Value.ExpireAt > now).Select(kv => kv.Key).ToHashSet();
                idle = Enumerable.Range(0, Math.Min(8, accessory.Data.PartyList.Count)).Where(i => !active.Contains(i)).OrderBy(i => i).ToList();
                debuffCount = active.Count;
                _towerIdlePlayers.Clear();
                foreach (var i in idle) _towerIdlePlayers.Add(i);
                _towerIdleLocked = true;
            }

            if (TowerDebug) accessory.Method.SendChat($"/e [塔] 锁定闲人：{string.Join(" ", idle.Select(TowerPlayerLabel))} debuffs={debuffCount} idle={idle.Count}");
        }

        private List<int> GetTowerIdlePlayers()
        {
            lock (_towerLock)
            {
                return _towerIdlePlayers.OrderBy(i => i).ToList();
            }
        }

        private void ResetTowerGuideState(bool advanceSeq)
        {
            lock (_towerLock)
            {
                ClearTowerGuideState(advanceSeq);
                _towerCooling = false;
                _towerIdleLocked = false;
                _towerRound = 0;
            }
        }

        private void ClearTowerGuideState(bool advanceSeq)
        {
            _towerRoundTowers.Clear();
            _towerDebuffs.Clear();
            _towerRawTargets.Clear();
            _towerMarkOrder.Clear();
            _towerSeenDouble.Clear();
            _towerIdlePlayers.Clear();
            if (advanceSeq) _towerMechanicSeq++;
        }

        private void TowerSummary(ScriptAccessory accessory, List<(Vector3 Pos, TowerElement Element, float Angle)> towers)
        {
            Dictionary<int, Vector3> raw;
            lock (_towerLock) raw = new Dictionary<int, Vector3>(_towerRawTargets);
            accessory.Method.SendChat("/e ── 踩塔汇总 ──");
            foreach (var t in towers)
            {
                var who = string.Join(" ", Enumerable.Range(0, 8).Where(i => raw.ContainsKey(i) && RoundTowerPos(raw[i]) == RoundTowerPos(t.Pos)).Select(TowerPlayerLabel));
                accessory.Method.SendChat($"/e   {TowerElementLabel(t.Element)}塔 @({t.Pos.X:F1},{t.Pos.Z:F1}) → {who}");
            }
        }

        private static (Vector3 Pos, TowerElement Element, float Angle)? ClockwiseFirstDifferentTower(List<(Vector3 Pos, TowerElement Element, float Angle)> towers, TowerElement element)
        {
            var anchors = towers.Where(t => t.Element == element).OrderBy(t => t.Angle).ToList();
            if (anchors.Count == 2 && ClockwiseTowerDelta(anchors[1].Angle, anchors[0].Angle) < ClockwiseTowerDelta(anchors[0].Angle, anchors[1].Angle))
                anchors.Reverse();
            foreach (var anchor in anchors)
            {
                var target = towers.Where(t => t.Element != element).OrderBy(t => ClockwiseTowerDelta(anchor.Angle, t.Angle)).FirstOrDefault();
                if (target != default) return target;
            }
            return null;
        }

        private static (Vector3 Pos, TowerElement Element, float Angle) CounterClockwiseFromTowerAnchor(List<(Vector3 Pos, TowerElement Element, float Angle)> towers, TowerElement element)
        {
            var anchorAngle = TowerAngle(TowerAnchorA);
            return towers.Where(t => t.Element == element).OrderBy(t => CounterClockwiseTowerDelta(anchorAngle, t.Angle)).First();
        }

        private static float ClockwiseTowerDelta(float from, float to) => (to - from + MathF.PI * 2f) % (MathF.PI * 2f);

        private static float CounterClockwiseTowerDelta(float from, float to) => (from - to + MathF.PI * 2f) % (MathF.PI * 2f);

        private static Vector3 RoundTowerPos(Vector3 pos) => new(MathF.Round(pos.X, 1), 0f, MathF.Round(pos.Z, 1));

        private static float TowerAngle(Vector3 pos)
        {
            var angle = MathF.Atan2(pos.X - ArenaCenter.X, ArenaCenter.Z - pos.Z);
            return angle < 0f ? angle + MathF.PI * 2f : angle;
        }

        private static string TowerElementLabel(TowerElement element) => element switch
        {
            TowerElement.Fire => "火",
            TowerElement.Ice => "冰",
            TowerElement.Thunder => "雷",
            _ => "?"
        };

        private static string TowerPlayerLabel(int index) => index >= 0 && index < TowerLabels.Length ? TowerLabels[index] : $"P{index}";

        #endregion

        #region 核爆/分散（癫狂交响曲）

        [ScriptMethod(name: "核爆（双T死刑）", eventType: EventTypeEnum.StatusAdd,
            eventCondition: ["StatusID:5350"], userControl: false)]
        public void OnBuster(Event @event, ScriptAccessory accessory)
        {
            if (!EnableBusterSpread) return;
            if (!TryParseObjectId(@event["TargetId"], out var tid)) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"{DrawPrefix}_Buster";
            dp.Owner = tid;
            dp.Scale = new Vector2(BusterRadius);
            dp.Color = BusterColor.V4;
            dp.DestoryAt = BusterDurationMs;
            dp.ScaleMode = ScaleMode.None;
            accessory.Method.SendDraw(DM, DrawTypeEnum.Circle, dp);
        }

        [ScriptMethod(name: "神圣（随机分散）", eventType: EventTypeEnum.StatusAdd,
            eventCondition: ["StatusID:5351"], userControl: false)]
        public void OnSpread(Event @event, ScriptAccessory accessory)
        {
            if (!EnableBusterSpread) return;
            if (!TryParseObjectId(@event["TargetId"], out var tid)) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"{DrawPrefix}_Spread";
            dp.Owner = tid;
            dp.Scale = new Vector2(SpreadRadius);
            dp.Color = SpreadColor.V4;
            dp.DestoryAt = SpreadDurationMs;
            dp.ScaleMode = ScaleMode.None;
            accessory.Method.SendDraw(DM, DrawTypeEnum.Circle, dp);
        }

        #endregion

        #region 地水（矩形AOE）

        [ScriptMethod(name: "地水", eventType: EventTypeEnum.StartCasting,
            eventCondition: ["ActionId:49539"], userControl: false)]
        public void OnWater(Event @event, ScriptAccessory accessory)
        {
            if (!EnableWater) return;

            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            var rot = float.Parse(@event["SourceRotation"]);

            _floodCount++;

            if (FloodDebug)
                accessory.Method.SendChat($"/e [洪水] #{_floodCount} 读条 pos=({pos.X:F1},{pos.Z:F1}) rot={rot:F3}");

            // 矩形中心 = Boss位置 - 前方*偏移（矩形向后方延伸）
            var dir = new Vector3(MathF.Sin(rot), 0f, MathF.Cos(rot));
            var center = pos - dir * WaterBackOffset;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"{DrawPrefix}_Water";
            dp.Position = center;
            dp.Rotation = rot;
            dp.Scale = new Vector2(WaterWidth, WaterLength);
            dp.Color = WaterColor.V4;
            dp.Delay = WaterDelayMs;
            dp.DestoryAt = WaterDurationMs;
            dp.ScaleMode = ScaleMode.None;
            accessory.Method.SendDraw(DM, DrawTypeEnum.Rect, dp);

            // ── 洪水2穿1：每4个读条异向配对 ──
            if (EnableFloodSafe)
            {
                if (_seqCount >= 4)
                    _seqCount = 0;

                _floodSeqRot[_seqCount] = rot;
                _seqCount++;

                if (rot < 0f && _negCount < 2)
                {
                    _floodNegPos[_negCount] = pos;
                    _floodNegRot[_negCount] = rot;
                    _negCount++;
                }
                else if (rot >= 0f && _posCount < 2)
                {
                    _floodPosPos[_posCount] = pos;
                    _floodPosRot[_posCount] = rot;
                    _posCount++;
                }

                if (_negCount >= 1 && _posCount >= 1 && _negCount + _posCount >= 4)
                {
                    _floodGroupCount++;

                    // 遍历所有 neg×pos 组合，取距场中最近的交点
                    Vector3? best = null; float bestDist = float.MaxValue;
                    int bestNegIndex = -1, bestPosIndex = -1;
                    for (int ni = 0; ni < _negCount; ni++)
                    for (int pi = 0; pi < _posCount; pi++)
                    {
                        var dn = new Vector3(MathF.Sin(_floodNegRot[ni]), 0f, MathF.Cos(_floodNegRot[ni]));
                        var dpo = new Vector3(MathF.Sin(_floodPosRot[pi]), 0f, MathF.Cos(_floodPosRot[pi]));
                        var pt = LineIntersection2D(_floodNegPos[ni], dn, _floodPosPos[pi], dpo);
                        if (pt == null) continue;
                        var d = Vector3.Distance(pt.Value, ArenaCenter);
                        if (d < bestDist)
                        {
                            best = pt.Value;
                            bestDist = d;
                            bestNegIndex = ni;
                            bestPosIndex = pi;
                        }
                    }

                    if (best == null || bestDist > FloodMaxDist)
                    {
                        if (FloodDebug)
                            accessory.Method.SendChat($"/e [洪水] 组{_floodGroupCount} 交点距场中={bestDist:F1}m 超出阈值{FloodMaxDist:F1}m ✗");
                        _negCount = 0; _posCount = 0;
                        _seqCount = 0;
                        return;
                    }
                    var safePos = best.Value;

                    _negCount = 0; _posCount = 0;

                    // 向场中偏移4米
                    var toCenter = ArenaCenter - safePos;
                    var toCenterLen = toCenter.Length();
                    if (toCenterLen > 0.001f)
                        safePos += toCenter / toCenterLen * 4f;

                    var midPos = TryBuildFloodMidNode(safePos, bestNegIndex, bestPosIndex, out var node) ? node : (Vector3?)null;

                    if (FloodDebug)
                    {
                        accessory.Method.SendChat($"/e [洪水] 组{_floodGroupCount} 交点=({safePos.X:F1},{safePos.Z:F1}) 距场中={bestDist:F1}m ✓");
                        if (midPos.HasValue)
                            accessory.Method.SendChat($"/e [洪水] 离散中间节点=({midPos.Value.X:F1},{midPos.Value.Z:F1})");
                    }

                    if (_floodGroupCount == 1)
                    {
                        _savedFloodSafe1 = safePos;
                        _savedFloodMid1 = midPos;
                    }
                    else if (_floodGroupCount == 2)
                    {
                        if (_savedFloodSafe1.HasValue && EnableFloodMidNode)
                        {
                            var routeMid = _savedFloodMid1 ?? BuildFloodRouteMidFallback(safePos, _savedFloodSafe1.Value);
                            DrawFloodRoute(accessory, safePos, routeMid, _savedFloodSafe1.Value);
                        }
                        else
                        {
                            DrawFloodSafe(accessory, safePos, 2, Flood2DelayMs, Flood2DurationMs);
                            if (_savedFloodSafe1.HasValue)
                                DrawFloodSafe(accessory, _savedFloodSafe1.Value, 1, Flood1DelayMs, Flood1DurationMs);
                        }

                        _savedFloodSafe1 = null;
                        _savedFloodMid1 = null;
                    }

                    _seqCount = 0;
                }
            }
        }

        private bool TryBuildFloodMidNode(Vector3 safePos, int bestNegIndex, int bestPosIndex, out Vector3 node)
        {
            node = default;
            if (!EnableFloodMidNode || _seqCount < 1 || bestNegIndex < 0 || bestPosIndex < 0) return false;

            var radius = Vector3.Distance(safePos, ArenaCenter);
            if (radius < 0.001f) return false;

            var firstWaveIsNeg = _floodSeqRot[0] < 0f;
            var source = firstWaveIsNeg ? _floodNegPos[bestNegIndex] : _floodPosPos[bestPosIndex];
            var delta = safePos - ArenaCenter;

            if (MathF.Abs(delta.X) >= MathF.Abs(delta.Z))
            {
                var north = new Vector3(ArenaCenter.X, 0f, ArenaCenter.Z - radius);
                var south = new Vector3(ArenaCenter.X, 0f, ArenaCenter.Z + radius);
                node = Vector3.Distance(source, north) <= Vector3.Distance(source, south) ? north : south;
            }
            else
            {
                var west = new Vector3(ArenaCenter.X - radius, 0f, ArenaCenter.Z);
                var east = new Vector3(ArenaCenter.X + radius, 0f, ArenaCenter.Z);
                node = Vector3.Distance(source, west) <= Vector3.Distance(source, east) ? west : east;
            }

            return true;
        }

        private static Vector3 BuildFloodRouteMidFallback(Vector3 from, Vector3 to)
        {
            var fromDelta = from - ArenaCenter;
            var toDelta = to - ArenaCenter;
            var guideDelta = fromDelta + toDelta;
            if (guideDelta.Length() < 0.001f)
                guideDelta = toDelta.Length() >= fromDelta.Length() ? toDelta : fromDelta;

            var radius = MathF.Max(fromDelta.Length(), toDelta.Length());
            if (radius < 0.001f)
                return ArenaCenter;

            if (MathF.Abs(guideDelta.X) >= MathF.Abs(guideDelta.Z))
            {
                var north = new Vector3(ArenaCenter.X, 0f, ArenaCenter.Z - radius);
                var south = new Vector3(ArenaCenter.X, 0f, ArenaCenter.Z + radius);
                return Vector3.Distance(from, north) <= Vector3.Distance(from, south) ? north : south;
            }

            var west = new Vector3(ArenaCenter.X - radius, 0f, ArenaCenter.Z);
            var east = new Vector3(ArenaCenter.X + radius, 0f, ArenaCenter.Z);
            return Vector3.Distance(from, west) <= Vector3.Distance(from, east) ? west : east;
        }

        private void DrawFloodRoute(ScriptAccessory a, Vector3 safe2, Vector3 mid, Vector3 safe1)
        {
            var secondStart = Math.Max(0, Flood2DelayMs);
            var firstResolve = Math.Max(secondStart, Flood1DelayMs);
            var resolveInterval = Math.Max(1, Flood1DurationMs / 3);
            var secondResolve = firstResolve + resolveInterval;
            var endAt = Math.Max(secondStart + Flood2DurationMs, Flood1DelayMs + Flood1DurationMs);
            if (secondResolve > endAt)
                secondResolve = endAt;
            var displayDuration = endAt;

            var inactive = FloodRouteInactiveColor.V4;
            var active = FloodRouteStartColor.V4;
            var midActive = FloodRouteMidColor.V4;
            var finalActive = FloodRouteFinalColor.V4;

            DrawFloodRoutePoint(a, "Route2_Base", safe2, 1.35f, 0, displayDuration, inactive);
            DrawFloodRoutePoint(a, "RouteMid_Base", mid, 1.2f, 0, displayDuration, inactive);
            DrawFloodRoutePoint(a, "Route1_Base", safe1, 1.35f, 0, displayDuration, inactive);
            DrawFloodRouteLine(a, "Route_2_To_Mid_Base", safe2, mid, 0, displayDuration, inactive);
            DrawFloodRouteLine(a, "Route_Mid_To_1_Base", mid, safe1, 0, displayDuration, inactive);

            DrawFloodRoutePoint(a, "Route2_Active", safe2, 1.55f, secondStart, Math.Max(0, firstResolve - secondStart), active);
            DrawFloodGuideTo(a, "RouteGuide_To_2", safe2, secondStart, Math.Max(0, firstResolve - secondStart), active);

            DrawFloodRoutePoint(a, "RouteMid_Active", mid, 1.4f, firstResolve, Math.Max(0, secondResolve - firstResolve), midActive);
            DrawFloodRouteLine(a, "Route_2_To_Mid_Active", safe2, mid, firstResolve, Math.Max(0, secondResolve - firstResolve), midActive);
            DrawFloodGuideTo(a, "RouteGuide_To_Mid", mid, firstResolve, Math.Max(0, secondResolve - firstResolve), midActive);

            DrawFloodRoutePoint(a, "Route1_Active", safe1, 1.55f, secondResolve, Math.Max(0, endAt - secondResolve), finalActive);
            DrawFloodRouteLine(a, "Route_Mid_To_1_Active", mid, safe1, secondResolve, Math.Max(0, endAt - secondResolve), finalActive);
        }

        private void DrawFloodRoutePoint(ScriptAccessory a, string suffix, Vector3 pos, float radius, int delayMs, int durationMs, Vector4 color)
        {
            if (durationMs <= 0) return;

            var dp = a.Data.GetDefaultDrawProperties();
            dp.Name = $"{DrawPrefix}_Flood{suffix}";
            dp.Position = pos;
            dp.Scale = new Vector2(radius);
            dp.Color = color;
            dp.Delay = delayMs;
            dp.DestoryAt = durationMs;
            dp.ScaleMode = ScaleMode.None;
            a.Method.SendDraw(DM, DrawTypeEnum.Circle, dp);
        }

        private void DrawFloodGuideTo(ScriptAccessory a, string suffix, Vector3 target, int delayMs, int durationMs, Vector4 color)
        {
            if (durationMs <= 0) return;

            var dp = a.Data.GetDefaultDrawProperties();
            dp.Name = $"{DrawPrefix}_Flood{suffix}";
            dp.Owner = a.Data.Me;
            dp.TargetPosition = target;
            dp.Scale = new Vector2(1.5f);
            dp.ScaleMode = ScaleMode.YByDistance;
            dp.Color = color;
            dp.Delay = delayMs;
            dp.DestoryAt = durationMs;
            a.Method.SendDraw(DMG, DrawTypeEnum.Displacement, dp);
        }

        private void DrawFloodRouteLine(ScriptAccessory a, string suffix, Vector3 from, Vector3 to, int delayMs, int durationMs, Vector4 color)
        {
            if (durationMs <= 0) return;

            var dp = a.Data.GetDefaultDrawProperties();
            dp.Name = $"{DrawPrefix}_Flood{suffix}";
            dp.Position = from;
            dp.TargetPosition = to;
            dp.Scale = new Vector2(0.8f);
            dp.ScaleMode = ScaleMode.YByDistance;
            dp.Color = color;
            dp.Delay = delayMs;
            dp.DestoryAt = durationMs;
            a.Method.SendDraw(DMG, DrawTypeEnum.Displacement, dp);
        }

        private void DrawFloodSafe(ScriptAccessory a, Vector3 pos, int idx, int delayMs, int durationMs, Vector3? midPos = null)
        {
            if (FloodDebug)
                a.Method.SendChat($"/e [洪水] 绘制安全点{idx} pos=({pos.X:F1},{pos.Z:F1}) 延迟={delayMs}ms 持续={durationMs}ms");

            var dpSafe = a.Data.GetDefaultDrawProperties();
            dpSafe.Name = $"{DrawPrefix}_FloodSafe{idx}";
            dpSafe.Position = pos;
            dpSafe.Scale = new Vector2(1.5f);
            dpSafe.Color = new Vector4(0f, 1f, 0f, 1f);
            dpSafe.Delay = delayMs;
            dpSafe.DestoryAt = durationMs;
            dpSafe.ScaleMode = ScaleMode.None;
            a.Method.SendDraw(DM, DrawTypeEnum.Circle, dpSafe);

            var guideDelay = delayMs;
            var guideDuration = durationMs;

            if (midPos.HasValue && EnableFloodMidNode)
            {
                var midDuration = Math.Min(Math.Max(0, FloodMidNodeDurationMs), durationMs);
                if (midDuration > 0)
                {
                    var dpMid = a.Data.GetDefaultDrawProperties();
                    dpMid.Name = $"{DrawPrefix}_FloodMid{idx}";
                    dpMid.Position = midPos.Value;
                    dpMid.Scale = new Vector2(1.2f);
                    dpMid.Color = new Vector4(0f, 1f, 0.4f, 1f);
                    dpMid.Delay = delayMs;
                    dpMid.DestoryAt = midDuration;
                    dpMid.ScaleMode = ScaleMode.None;
                    a.Method.SendDraw(DM, DrawTypeEnum.Circle, dpMid);

                    var dpMidGuide = a.Data.GetDefaultDrawProperties();
                    dpMidGuide.Name = $"{DrawPrefix}_FloodMidGuide{idx}";
                    dpMidGuide.Owner = a.Data.Me;
                    dpMidGuide.TargetPosition = midPos.Value;
                    dpMidGuide.Scale = new Vector2(1.5f);
                    dpMidGuide.ScaleMode = ScaleMode.YByDistance;
                    dpMidGuide.Color = new Vector4(0f, 1f, 0.4f, 1f);
                    dpMidGuide.Delay = delayMs;
                    dpMidGuide.DestoryAt = midDuration;
                    a.Method.SendDraw(DMG, DrawTypeEnum.Displacement, dpMidGuide);

                    guideDelay += midDuration;
                    guideDuration -= midDuration;
                }
            }

            if (guideDuration <= 0) return;

            var dpGuide = a.Data.GetDefaultDrawProperties();
            dpGuide.Name = $"{DrawPrefix}_FloodGuide{idx}";
            dpGuide.Owner = a.Data.Me;
            dpGuide.TargetPosition = pos;
            dpGuide.Scale = new Vector2(1.5f);
            dpGuide.ScaleMode = ScaleMode.YByDistance;
            dpGuide.Color = new Vector4(0f, 1f, 1f, 1f);
            dpGuide.Delay = guideDelay;
            dpGuide.DestoryAt = guideDuration;
            a.Method.SendDraw(DMG, DrawTypeEnum.Displacement, dpGuide);
        }

        #endregion

        #region Helpers

        private static Vector3 RotationToDirection(float radians)
        {
            return new Vector3(MathF.Sin(radians), 0f, MathF.Cos(radians));
        }

        private static int PositiveMod(int value, int mod)
        {
            return (value % mod + mod) % mod;
        }

        private static bool TryParseObjectId(string str, out uint id)
        {
            id = 0;
            if (string.IsNullOrEmpty(str)) return false;
            str = str.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? str[2..] : str;
            return uint.TryParse(str, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out id);
        }

        private static Vector3? LineIntersection2D(Vector3 p1, Vector3 d1, Vector3 p2, Vector3 d2)
        {
            var crossD = d1.X * d2.Z - d1.Z * d2.X;
            if (MathF.Abs(crossD) < 0.0001f) return null;
            var delta = p2 - p1;
            var t = (delta.X * d2.Z - delta.Z * d2.X) / crossD;
            return new Vector3(p1.X + t * d1.X, 0f, p1.Z + t * d1.Z);
        }

        private static Vector3 ClampToDistance(Vector3 p, Vector3 c, float maxD)
        {
            var d = p - c;
            var len = d.Length();
            if (len < 0.001f || len <= maxD) return p;
            return c + d / len * maxD;
        }

        private Vector3 FloodFallback(Vector3 p1, Vector3 p2)
        {
            var mid = (p1 + p2) * 0.5f;
            var dir = mid - ArenaCenter;
            if (dir.Length() < 0.001f) dir = new Vector3(1f, 0f, 0f);
            else dir = Vector3.Normalize(dir);
            return ArenaCenter + dir * FloodSafeDistance;
        }

        private static int DurationMs(Event @event, int fallback = 5000)
        {
            return int.TryParse(@event["DurationMilliseconds"], out var d) && d > 0 ? d : fallback;
        }

        #endregion
    }
}
