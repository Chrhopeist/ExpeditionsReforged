using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpeditionsReforged.Content.Expeditions;
using ExpeditionsReforged.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ExpeditionsReforged.Players
{
    public class ExpeditionsPlayer : ModPlayer
    {
        public bool ExpeditionUIOpen;
        public bool TrackerUIOpen;
        public string TrackedExpeditionId { get; private set; } = string.Empty;

        private readonly List<ExpeditionProgress> _expeditionProgressEntries = new();
        private readonly Dictionary<string, ExpeditionProgress> _progressByExpeditionId = new(StringComparer.OrdinalIgnoreCase);
        private bool _lastDaytime;

        public IReadOnlyList<ExpeditionProgress> ExpeditionProgressEntries => _expeditionProgressEntries;

        public override void OnEnterWorld()
        {
            ExpeditionUIOpen = false;
            TrackerUIOpen = false;
            TrackedExpeditionId = string.Empty;

            _lastDaytime = Main.dayTime;
            ReconcileDefinitions();
        }

        public bool TryGetExpeditionProgress(string expeditionId, out ExpeditionProgress progress)
        {
            if (string.IsNullOrWhiteSpace(expeditionId))
            {
                progress = null;
                return false;
            }

            return _progressByExpeditionId.TryGetValue(expeditionId, out progress);
        }

        public override void Initialize()
        {
            ExpeditionUIOpen = false;
            TrackerUIOpen = false;
            TrackedExpeditionId = string.Empty;
            _expeditionProgressEntries.Clear();
            _progressByExpeditionId.Clear();
            _lastDaytime = Main.dayTime;
        }

        public override void PostUpdate()
        {
            if (_lastDaytime != Main.dayTime)
            {
                string conditionId = Main.dayTime ? "time:day" : "time:night";
                ReportConditionProgress(conditionId, 1);
                _lastDaytime = Main.dayTime;
            }
        }

        public override void SaveData(TagCompound tag)
        {
            if (_expeditionProgressEntries.Count == 0)
            {
                tag["TrackedExpeditionId"] = TrackedExpeditionId;
                return;
            }

            tag["TrackedExpeditionId"] = TrackedExpeditionId;
            tag["ExpeditionProgress"] = _expeditionProgressEntries
                .Select(progress => new TagCompound
                {
                    ["expeditionId"] = progress.ExpeditionId,
                    ["stableKey"] = progress.StableProgressKey,
                    ["startGameTick"] = progress.StartGameTick,
                    ["isActive"] = progress.IsActive,
                    ["isCompleted"] = progress.IsCompleted,
                    ["rewardsClaimed"] = progress.RewardsClaimed,
                    ["isOrphan"] = progress.IsOrphaned,
                    ["conditions"] = progress.ConditionProgress.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                })
                .ToList();
        }

        public override void LoadData(TagCompound tag)
        {
            _expeditionProgressEntries.Clear();
            _progressByExpeditionId.Clear();
            TrackedExpeditionId = tag.GetString("TrackedExpeditionId") ?? string.Empty;

            if (!tag.TryGet("ExpeditionProgress", out List<TagCompound> savedProgressEntries))
            {
                return;
            }

            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();

            foreach (TagCompound entry in savedProgressEntries)
            {
                string expeditionId = entry.GetString("expeditionId");
                if (string.IsNullOrWhiteSpace(expeditionId))
                {
                    continue;
                }

                ExpeditionProgress progress = new()
                {
                    ExpeditionId = expeditionId,
                    StableProgressKey = entry.GetString("stableKey"),
                    StartGameTick = entry.GetLong("startGameTick"),
                    IsActive = entry.GetBool("isActive"),
                    IsOrphaned = entry.GetBool("isOrphan")
                };

                if (string.IsNullOrWhiteSpace(progress.StableProgressKey) && registry.TryGetDefinition(expeditionId, out ExpeditionDefinition definition))
                {
                    progress.StableProgressKey = definition.GetStableProgressKey(Player.whoAmI);
                }

                if (entry.GetBool("isCompleted"))
                {
                    progress.Complete();
                }

                if (entry.GetBool("rewardsClaimed"))
                {
                    progress.ClaimRewards();
                }

                if (entry.TryGet("conditions", out Dictionary<string, int> savedConditions))
                {
                    foreach ((string conditionId, int value) in savedConditions)
                    {
                        if (string.IsNullOrWhiteSpace(conditionId))
                        {
                            continue;
                        }

                        progress.ConditionProgress[conditionId] = Math.Max(0, value);
                    }
                }

                AddOrReplaceProgress(progress);
            }
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                return;
            }

            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)Systems.ExpeditionPacketType.SyncPlayer);
            packet.Write((byte)Player.whoAmI);
            WriteProgressToPacket(packet);
            packet.Send(toWho, fromWho);
        }

        public void ReportItemPickup(Item item)
        {
            if (item is null)
            {
                return;
            }

            ReportConditionProgress($"item:{item.type}", item.stack);
        }

        // Craft tracking is handled elsewhere.
        public void ReportCraft(Item item)
        {
            if (item is null)
            {
                return;
            }

            ReportConditionProgress($"craft:{item.type}", item.stack);
        }

        public void ReportKill(NPC npc)
        {
            if (npc is null)
            {
                return;
            }

            ReportConditionProgress($"npc:{npc.type}", 1);
        }

        public bool TryStartExpedition(string expeditionId)
        {
            if (string.IsNullOrWhiteSpace(expeditionId))
            {
                return false;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ExpeditionsReforged.RequestStart(expeditionId);
                return false;
            }

            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();
            if (!registry.TryGetDefinition(expeditionId, out ExpeditionDefinition definition))
            {
                return false;
            }

            return TryStartExpedition(definition);
        }

        public bool TryStartExpedition(ExpeditionDefinition definition)
        {
            if (definition is null)
            {
                return false;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ExpeditionsReforged.RequestStart(definition.Id);
                return false;
            }

            if (!definition.IsRepeatable && _progressByExpeditionId.TryGetValue(definition.Id, out ExpeditionProgress existing) && existing.IsCompleted)
            {
                return false;
            }

            ExpeditionProgress progress = GetOrCreateProgress(definition);
            progress.IsOrphaned = false;
            progress.IsActive = true;
            progress.IsCompleted = false;
            progress.RewardsClaimed = false;
            progress.StartGameTick = Main.GameUpdateCount;
            progress.ConditionProgress.Clear();

            foreach (DeliverableDefinition deliverable in definition.Deliverables)
            {
                progress.ConditionProgress[deliverable.Id] = 0;
            }

            return true;
        }

        public bool TryCompleteExpedition(string expeditionId)
        {
            if (!TryGetExpeditionProgress(expeditionId, out ExpeditionProgress progress) || progress.IsOrphaned || !progress.IsActive)
            {
                return false;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ExpeditionsReforged.RequestCompletion(expeditionId);
                return false;
            }

            progress.Complete();
            return true;
        }

        public bool TryTrackExpedition(string expeditionId)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                TrackedExpeditionId = expeditionId ?? string.Empty;
                ExpeditionsReforged.RequestTrack(expeditionId);
                return true;
            }

            if (string.IsNullOrWhiteSpace(expeditionId))
            {
                TrackedExpeditionId = string.Empty;
                return true;
            }

            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();
            if (!registry.TryGetDefinition(expeditionId, out _))
            {
                return false;
            }

            TrackedExpeditionId = expeditionId;
            return true;
        }

        public bool TryClaimRewards(string expeditionId)
        {
            if (!TryGetExpeditionProgress(expeditionId, out ExpeditionProgress progress) || progress.IsOrphaned || !progress.IsCompleted || progress.RewardsClaimed)
            {
                return false;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ExpeditionsReforged.RequestClaim(expeditionId);
                return false;
            }

            progress.ClaimRewards();
            return true;
        }

        public void ReportConditionProgress(string conditionId, int amount)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(conditionId))
            {
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)Systems.ExpeditionPacketType.ConditionProgress);
                packet.Write(conditionId);
                packet.Write(amount);
                packet.Send();
                return;
            }

            if (ApplyConditionProgress(conditionId, amount) && Main.netMode == NetmodeID.Server)
            {
                ExpeditionsReforged.SendProgressSync(-1, Player.whoAmI, this);
            }
        }

        public bool ReportedFromServer(string conditionId, int amount)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(conditionId))
            {
                return false;
            }

            return ApplyConditionProgress(conditionId, amount);
        }

        public void ReceiveProgressSync(BinaryReader reader)
        {
            TrackedExpeditionId = reader.ReadString();
            _expeditionProgressEntries.Clear();
            _progressByExpeditionId.Clear();

            ushort entryCount = reader.ReadUInt16();
            for (int i = 0; i < entryCount; i++)
            {
                ExpeditionProgress progress = new()
                {
                    ExpeditionId = reader.ReadString(),
                    StableProgressKey = reader.ReadString(),
                    StartGameTick = reader.ReadInt64(),
                    IsActive = reader.ReadBoolean(),
                    IsCompleted = reader.ReadBoolean(),
                    RewardsClaimed = reader.ReadBoolean(),
                    IsOrphaned = reader.ReadBoolean()
                };

                ushort conditionCount = reader.ReadUInt16();
                for (int conditionIndex = 0; conditionIndex < conditionCount; conditionIndex++)
                {
                    string conditionId = reader.ReadString();
                    int value = reader.ReadInt32();
                    if (!string.IsNullOrWhiteSpace(conditionId))
                    {
                        progress.ConditionProgress[conditionId] = value;
                    }
                }

                AddOrReplaceProgress(progress);
            }

            ReconcileDefinitions();
        }

        public void WriteProgressToPacket(ModPacket packet)
        {
            packet.Write(TrackedExpeditionId ?? string.Empty);
            packet.Write((ushort)_expeditionProgressEntries.Count);
            foreach (ExpeditionProgress progress in _expeditionProgressEntries)
            {
                packet.Write(progress.ExpeditionId ?? string.Empty);
                packet.Write(progress.StableProgressKey ?? string.Empty);
                packet.Write(progress.StartGameTick);
                packet.Write(progress.IsActive);
                packet.Write(progress.IsCompleted);
                packet.Write(progress.RewardsClaimed);
                packet.Write(progress.IsOrphaned);

                packet.Write((ushort)progress.ConditionProgress.Count);
                foreach ((string conditionId, int value) in progress.ConditionProgress)
                {
                    packet.Write(conditionId ?? string.Empty);
                    packet.Write(value);
                }
            }
        }

        private void AddOrReplaceProgress(ExpeditionProgress progress)
        {
            _progressByExpeditionId[progress.ExpeditionId] = progress;

            int existingIndex = _expeditionProgressEntries.FindIndex(p => string.Equals(p.ExpeditionId, progress.ExpeditionId, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                _expeditionProgressEntries[existingIndex] = progress;
            }
            else
            {
                _expeditionProgressEntries.Add(progress);
            }
        }

        private ExpeditionProgress GetOrCreateProgress(ExpeditionDefinition definition)
        {
            if (_progressByExpeditionId.TryGetValue(definition.Id, out ExpeditionProgress progress))
            {
                return progress;
            }

            progress = ExpeditionProgress.Create(definition, Player.whoAmI);
            AddOrReplaceProgress(progress);
            return progress;
        }

        private bool ApplyConditionProgress(string conditionId, int amount)
        {
            bool changed = false;
            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();

            foreach (ExpeditionProgress progress in _expeditionProgressEntries.Where(p => p.IsActive && !p.IsOrphaned))
            {
                if (!registry.TryGetDefinition(progress.ExpeditionId, out ExpeditionDefinition definition))
                {
                    progress.IsOrphaned = true;
                    continue;
                }

                foreach (DeliverableDefinition deliverable in definition.Deliverables)
                {
                    if (!string.Equals(deliverable.Id, conditionId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    int current = progress.ConditionProgress.TryGetValue(conditionId, out int value) ? value : 0;
                    int updated = Math.Min(deliverable.RequiredCount, current + amount);
                    if (updated != current)
                    {
                        progress.ConditionProgress[conditionId] = updated;
                        changed = true;
                    }
                }

                if (!progress.IsCompleted && definition.Deliverables.Any() && definition.Deliverables.All(d => progress.ConditionProgress.TryGetValue(d.Id, out int value) && value >= d.RequiredCount))
                {
                    progress.Complete();
                    changed = true;
                }
            }

            return changed;
        }

        private void ReconcileDefinitions()
        {
            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();
            foreach (ExpeditionProgress progress in _expeditionProgressEntries)
            {
                progress.IsOrphaned = !registry.TryGetDefinition(progress.ExpeditionId, out ExpeditionDefinition definition);
                if (progress.IsOrphaned)
                {
                    continue;
                }

                foreach (DeliverableDefinition deliverable in definition.Deliverables)
                {
                    if (!progress.ConditionProgress.ContainsKey(deliverable.Id))
                    {
                        progress.ConditionProgress[deliverable.Id] = 0;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(TrackedExpeditionId) && !registry.TryGetDefinition(TrackedExpeditionId, out _))
            {
                TrackedExpeditionId = string.Empty;
            }
        }
    }
}
