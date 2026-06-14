using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Dalamud.Utility.Numerics;
using KodakkuAssist.Data;
using KodakkuAssist.Module.Draw;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Script;

namespace PersonalKodakkuAssist.DancingMadUltimate
{
    [ScriptType(
        name: "P2 遗弃末世 偶数塔扇形互喷范围",
        territorys: [1363],
        guid: "31cf341e-893d-4ce7-97ba-a834a0ac913b",
        version: "1.4.1",
        note: "参考 Cicero 原始实现，仅显示偶数轮塔中扇形点名互相瞄准最近玩家的实时范围。",
        author: "Mamimi")]
    public class P2_EvenTower_FanTelegraph
    {
        private const string DrawPrefix = "P2_遗弃末世_偶数塔_扇形范围";
        private const string DrawRegex = "^P2_遗弃末世_偶数塔_扇形范围_.*$";
        private const int MaximumDuration = 7_200_000;
        private const int FinalCleanupDelay = 10_000;
        private const int TowerRadius = 4;
        private const int LastTowerEventCount = 16;

        private static readonly Vector3 ArenaCenter = new(100, 0, 100);
        private static readonly Vector3 RawTowerPosition = new(100, 0, 92);

        private readonly IconType[] iconTypes =
            Enumerable.Range(0, 8).Select(_ => IconType.Unknown).ToArray();
        private readonly List<int> towers = new();
        private readonly ConcurrentDictionary<ulong, int> drawingCounters = new();
        private readonly object towerCounterLock = new();

        private volatile bool mechanicActive;
        private volatile int towerEventCount;
        private int pullGeneration;

        private enum IconType
        {
            Fan,
            Spread,
            Stack,
            Unknown
        }

        [UserSetting("扇形范围颜色")]
        public ScriptColor FanColor { get; set; } = new()
        {
            V4 = new Vector4(1, 0.55f, 0.7f, 1)
        };

        [UserSetting("扇形显示延迟（毫秒，建议3000-4000）")]
        public int FanDrawDelay { get; set; } = 3500;

        public void Init(ScriptAccessory accessory)
        {
            ResetMechanic(accessory, false);
        }

        [ScriptMethod(
            name: "P2 遗弃末世 偶数塔扇形 (机制开始)",
            eventType: EventTypeEnum.StartCasting,
            eventCondition: ["ActionId:47804"],
            userControl: false)]
        public void StartMechanic(Event @event, ScriptAccessory accessory)
        {
            ResetMechanic(accessory, true);
        }

        [ScriptMethod(
            name: "P2 遗弃末世 偶数塔扇形 (头标收集与范围)",
            eventType: EventTypeEnum.TargetIcon,
            eventCondition: ["Id:regex:^(02CD|02CC|02CB|02cd|02cc|02cb)$"])]
        public void CollectTargetIconAndDraw(Event @event, ScriptAccessory accessory)
        {
            if (!mechanicActive
                || !TryParseObjectId(@event["TargetId"], out var targetId))
            {
                return;
            }

            int targetIndex = accessory.Data.PartyList.IndexOf((uint)targetId);

            if (!IsLegalPartyIndex(targetIndex))
            {
                return;
            }

            lock (iconTypes)
            {
                iconTypes[targetIndex] = ParseIconType(@event["Id"]);

                lock (drawingCounters)
                {
                    int lastDrawing = drawingCounters.GetOrAdd(targetId, 0);
                    RemoveDrawingGeneration(targetId, lastDrawing, accessory);

                    Interlocked.Increment(ref lastDrawing);
                    drawingCounters[targetId] = lastDrawing;

                    lock (towers)
                    {
                        if (!IsCurrentRoundEven())
                        {
                            return;
                        }

                        DrawFanForCurrentTowers(
                            targetId,
                            targetIndex,
                            lastDrawing,
                            accessory);
                    }
                }
            }
        }

        [ScriptMethod(
            name: "P2 遗弃末世 偶数塔扇形 (塔数据与范围)",
            eventType: EventTypeEnum.EnvControl,
            eventCondition: ["Flag:2"])]
        public void CollectTowerAndDraw(Event @event, ScriptAccessory accessory)
        {
            if (!mechanicActive
                || !TryParseInteger(@event["Index"], out var index)
                || index < 1
                || index > 8)
            {
                return;
            }

            bool pairCompleted;

            lock (towers)
            {
                if (towers.Count >= 2)
                {
                    towers.Clear();
                }

                // The two towers in one round must use different field indexes.
                if (towers.Contains(index))
                {
                    return;
                }

                towers.Add(index);
                pairCompleted = towers.Count == 2;
            }

            int currentTowerEventCount;

            lock (towerCounterLock)
            {
                currentTowerEventCount = ++towerEventCount;
            }

            if (pairCompleted)
            {
                RedrawAllPartyFans(accessory);
            }

            if (currentTowerEventCount == LastTowerEventCount)
            {
                ScheduleFinalCleanup(pullGeneration, accessory);
            }
        }

        [ScriptMethod(
            name: "P2 遗弃末世 偶数塔扇形 (机制结束)",
            eventType: EventTypeEnum.StartCasting,
            eventCondition: ["ActionId:47805"],
            userControl: false)]
        public void EndMechanic(Event @event, ScriptAccessory accessory)
        {
            mechanicActive = false;
        }

        private void RedrawAllPartyFans(ScriptAccessory accessory)
        {
            lock (iconTypes)
            {
                int memberCount = Math.Min(8, accessory.Data.PartyList.Count);

                for (int targetIndex = 0; targetIndex < memberCount; targetIndex++)
                {
                    ulong targetId = accessory.Data.PartyList[targetIndex];

                    lock (drawingCounters)
                    {
                        int lastDrawing = drawingCounters.GetOrAdd(targetId, 0);
                        RemoveDrawingGeneration(targetId, lastDrawing, accessory);

                        Interlocked.Increment(ref lastDrawing);
                        drawingCounters[targetId] = lastDrawing;

                        lock (towers)
                        {
                            if (!IsCurrentRoundEven())
                            {
                                continue;
                            }

                            DrawFanForCurrentTowers(
                                targetId,
                                targetIndex,
                                lastDrawing,
                                accessory);
                        }
                    }
                }
            }
        }

        private void DrawFanForCurrentTowers(
            ulong targetId,
            int targetIndex,
            int drawingGeneration,
            ScriptAccessory accessory)
        {
            if (iconTypes[targetIndex] != IconType.Fan)
            {
                return;
            }

            for (int towerSlot = 0; towerSlot < Math.Min(2, towers.Count); towerSlot++)
            {
                var properties = accessory.Data.GetDefaultDrawProperties();
                properties.Name =
                    $"{DrawPrefix}_{targetId}_{drawingGeneration}_{towerSlot}";
                properties.Scale = new Vector2(40);
                properties.Radian = MathF.PI / 2;
                properties.Owner = targetId;
                properties.TargetResolvePattern =
                    PositionResolvePatternEnum.PlayerNearestOrder;
                properties.TargetOrderIndex = 1;
                properties.FadeCentrePosition = RotatePosition(
                    RawTowerPosition,
                    ArenaCenter,
                    Math.PI / 4 * (towers[towerSlot] - 1));
                properties.FadeDistance = TowerRadius;
                properties.FadeMode = FadeMode.OmenCentre;
                properties.Color = FanColor.V4.WithW(1);
                properties.Delay = Math.Clamp(FanDrawDelay, 0, 5000);
                properties.DestoryAt = MaximumDuration;

                accessory.Method.SendDraw(
                    DrawModeEnum.Default,
                    DrawTypeEnum.Fan,
                    properties);
            }
        }

        private bool IsCurrentRoundEven()
        {
            return towers.Count == 2
                && towerEventCount >= 2
                && ((towerEventCount / 2) & 1) == 0;
        }

        private static void RemoveDrawingGeneration(
            ulong targetId,
            int drawingGeneration,
            ScriptAccessory accessory)
        {
            accessory.Method.RemoveDraw(
                $"{DrawPrefix}_{targetId}_{drawingGeneration}_0");
            accessory.Method.RemoveDraw(
                $"{DrawPrefix}_{targetId}_{drawingGeneration}_1");
        }

        private void ScheduleFinalCleanup(
            int generation,
            ScriptAccessory accessory)
        {
            System.Threading.Tasks.Task.Delay(FinalCleanupDelay).ContinueWith(_ =>
            {
                if (generation == pullGeneration)
                {
                    accessory.Method.RemoveDraw(DrawRegex);
                }
            });
        }

        private void ResetMechanic(
            ScriptAccessory accessory,
            bool activate)
        {
            pullGeneration++;
            mechanicActive = activate;

            lock (iconTypes)
            {
                for (int i = 0; i < iconTypes.Length; i++)
                {
                    iconTypes[i] = IconType.Unknown;
                }
            }

            lock (towers)
            {
                towers.Clear();
            }

            lock (drawingCounters)
            {
                drawingCounters.Clear();
            }

            lock (towerCounterLock)
            {
                towerEventCount = 0;
            }

            accessory.Method.RemoveDraw(DrawRegex);
        }

        private static IconType ParseIconType(string? rawIconId)
        {
            return rawIconId?.Trim().ToUpperInvariant() switch
            {
                "02CD" => IconType.Fan,
                "02CC" => IconType.Spread,
                "02CB" => IconType.Stack,
                _ => IconType.Unknown
            };
        }

        private static bool TryParseObjectId(
            string? rawObjectId,
            out ulong result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(rawObjectId))
            {
                return false;
            }

            string objectId = rawObjectId.Trim();
            objectId = objectId.StartsWith(
                "0x",
                StringComparison.OrdinalIgnoreCase)
                ? objectId.Substring(2)
                : objectId;

            return ulong.TryParse(
                objectId,
                System.Globalization.NumberStyles.HexNumber,
                null,
                out result);
        }

        private static bool TryParseInteger(
            string? rawString,
            out int result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(rawString))
            {
                return false;
            }

            return int.TryParse(
                rawString.Trim(),
                System.Globalization.NumberStyles.Integer,
                null,
                out result);
        }

        private static bool IsLegalPartyIndex(int partyIndex)
        {
            return partyIndex >= 0 && partyIndex <= 7;
        }

        private static Vector3 RotatePosition(
            Vector3 position,
            Vector3 center,
            double radian,
            bool preserveHeight = true)
        {
            Vector2 positionInVector2 =
                new(position.X - center.X, position.Z - center.Z);
            double polarAngleAfterRotation =
                Math.PI
                - Math.Atan2(positionInVector2.X, positionInVector2.Y)
                + radian;

            return new Vector3(
                (float)(
                    center.X
                    + Math.Sin(polarAngleAfterRotation)
                    * positionInVector2.Length()),
                preserveHeight ? position.Y : center.Y,
                (float)(
                    center.Z
                    - Math.Cos(polarAngleAfterRotation)
                    * positionInVector2.Length()));
        }
    }
}
