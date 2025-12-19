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
        // Expedition data must live on ModPlayer so it is tied to the character file, travels with the player in multiplayer,
        // and participates in the built-in save/sync lifecycle. All state transitions happen through server-owned flows; the
        // client UI should only read this state and use the dedicated request packets to ask the server to mutate it.
        public bool ExpeditionUIOpen;
        public bool TrackerUIOpen;

        // Optional client-facing selection. The actual expedition state remains server-authoritative.
        public string TrackedExpeditionId { get; private set; } = string.Empty;

        // Full history of expedition progress for this player (active + completed). Active/complete/track separation is
        // expressed through the helper methods below rather than mutating this collection directly from UI code.
        private readonly List<ExpeditionProgress> _expeditionProgressEntries = new();
        private readonly Dictionary<string, ExpeditionProgress> _progressByExpeditionId = new(StringComparer.OrdinalIgnoreCase);
        private bool _lastDaytime;

        public IReadOnlyList<ExpeditionProgress> ExpeditionProgressEntries => _expeditionProgressEntries;

        /// <summary>
        /// Returns true when the expedition is present, not orphaned, and marked active on this player.
        /// </summary>
        public bool IsExpeditionActive(string expeditionId)
        {
            return TryGetExpeditionProgress(expeditionId, out ExpeditionProgress progress) && progress.IsActive && !progress.IsOrphaned;
        }

        /// <summary>
        /// Returns true when the expedition has been recorded as completed on this player.
        /// </summary>
        public bool IsExpeditionCompleted(string expeditionId)
        {
            return TryGetExpeditionProgress(expeditionId, out ExpeditionProgress progress) && progress.IsCompleted;
        }

        /// <summary>
        /// Returns a snapshot of active expeditions, excluding orphaned entries for removed definitions.
        /// </summary>
        public IReadOnlyList<ExpeditionProgress> GetActiveExpeditions()
        {
            return _expeditionProgressEntries
                .Where(progress => progress.IsActive && !progress.IsOrphaned)
                .ToList()
                .AsReadOnly();
        }

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
            tag["TrackedExpeditionId"] = TrackedExpeditionId ?? string.Empty;

            TagCompound expeditionsTag = new();

            foreach (ExpeditionProgress progress in _expeditionProgressEntries)
            {
                if (progress is null || string.IsNullOrWhiteSpace(progress.ExpeditionId))
                {
                    continue;
                }

                TagCompound conditionsTag = new();
                foreach ((string conditionId, int value) in progress.ConditionProgress)
                {
                    if (string.IsNullOrWhiteSpace(conditionId))
                    {
                        continue;
                    }

                    // TagCompound requires primitives; nested compounds keep condition data compatible with tModLoader saves.
                    conditionsTag[conditionId] = Math.Max(0, value);
                }

                TagCompound expeditionTag = new()
                {
                    ["stableKey"] = progress.StableProgressKey ?? string.Empty,
                    ["startGameTick"] = progress.StartGameTick,
                    ["isActive"] = progress.IsActive,
                    ["isCompleted"] = progress.IsCompleted,
                    ["rewardsClaimed"] = progress.RewardsClaimed,
                    ["isOrphan"] = progress.IsOrphaned,
                    ["conditions"] = conditionsTag
                };

                expeditionsTag[progress.ExpeditionId] = expeditionTag;
            }

            if (expeditionsTag.Count > 0)
            {
                tag["expeditions"] = expeditionsTag;
            }
        }

        public override void LoadData(TagCompound tag)
        {
            _expeditionProgressEntries.Clear();
            _progressByExpeditionId.Clear();
            TrackedExpeditionId = tag.GetString("TrackedExpeditionId") ?? string.Empty;

            if (!tag.TryGet("expeditions", out TagCompound savedExpeditions) || savedExpeditions is null)
            {
                return;
            }

            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();

            foreach ((string expeditionId, object expeditionEntry) in savedExpeditions)
            {
                if (string.IsNullOrWhiteSpace(expeditionId) || expeditionEntry is not TagCompound expeditionTag)
                {
                    continue;
                }

                expeditionTag.TryGet("stableKey", out string stableKey);
                expeditionTag.TryGet("startGameTick", out long startGameTick);
                bool isActive = expeditionTag.GetBool("isActive");
                bool isCompleted = expeditionTag.GetBool("isCompleted");
                bool rewardsClaimed = expeditionTag.GetBool("rewardsClaimed");
                bool isOrphaned = expeditionTag.GetBool("isOrphan");

                ExpeditionProgress progress = new()
                {
                    ExpeditionId = expeditionId,
                    StableProgressKey = stableKey ?? string.Empty,
                    StartGameTick = startGameTick,
                    IsActive = isActive,
                    IsOrphaned = isOrphaned
                };

                bool hasDefinition = registry.TryGetDefinition(expeditionId, out var definition);
                if (string.IsNullOrWhiteSpace(progress.StableProgressKey) && hasDefinition)
                {
                    progress.StableProgressKey = definition.GetStableProgressKey(Player.whoAmI);
                }

                if (expeditionTag.GetBool("isCompleted") && isCompleted)
                {
                    progress.Complete();
                }

                if (rewardsClaimed)
                {
                    progress.ClaimRewards();
                }

                if (expeditionTag.TryGet("conditions", out TagCompound savedConditions) && savedConditions is not null)
                {
                    foreach ((string conditionId, object value) in savedConditions)
                    {
                        if (string.IsNullOrWhiteSpace(conditionId))
                        {
                            continue;
                        }

                        if (value is int intValue)
                        {
                            progress.ConditionProgress[conditionId] = Math.Max(0, intValue);
                        }
                    }
                }

                if (hasDefinition)
                {
                    progress.IsOrphaned = false;
                    if (string.IsNullOrWhiteSpace(progress.StableProgressKey))
                    {
                        progress.StableProgressKey = definition.GetStableProgressKey(Player.whoAmI);
                    }
                }

                if (string.IsNullOrWhiteSpace(progress.StableProgressKey))
                {
                    progress.StableProgressKey = string.Empty;
                }

                AddOrReplaceProgress(progress);
            }

            // Validate tracked id and newly loaded entries against the registry to stay resilient to mod updates.
            ReconcileDefinitions();
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

        // All mutation helpers below should only execute their core logic on the server. Clients exit early after
        // dispatching a request packet, keeping the server as the authoritative owner of expedition state.
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
            if (!registry.TryGetExpedition(expeditionId, out ExpeditionDefinition definition))
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
            if (!registry.TryGetExpedition(expeditionId, out _))
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

            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();
            if (!registry.TryGetExpedition(expeditionId, out ExpeditionDefinition definition))
            {
                return false;
            }

            if (!ExpeditionRewardService.TryPayCompletionRewards(Player, definition))
            {
                Mod.Logger.Warn($"Failed to pay rewards for expedition '{expeditionId}' on player {Player.name}. Claim aborted.");
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
                if (!registry.TryGetExpedition(progress.ExpeditionId, out ExpeditionDefinition definition))
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
                progress.IsOrphaned = !registry.TryGetExpedition(progress.ExpeditionId, out ExpeditionDefinition definition);
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

            if (!string.IsNullOrWhiteSpace(TrackedExpeditionId) && !registry.TryGetExpedition(TrackedExpeditionId, out _))
            {
                TrackedExpeditionId = string.Empty;
            }
        }
        // SERVER-ONLY: starts an expedition safely
        internal void StartExpedition(string expeditionId, long startGameTick)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
        
            if (string.IsNullOrWhiteSpace(expeditionId))
                return;
        
            var registry = ModContent.GetInstance<ExpeditionRegistry>();
            if (!registry.TryGetExpedition(expeditionId, out var definition))
                return;
        
            ExpeditionProgress progress = GetOrCreateProgress(definition);
        
            progress.IsOrphaned = false;
            progress.IsActive = true;
            progress.IsCompleted = false;
            progress.RewardsClaimed = false;
            progress.StartGameTick = startGameTick;
        
            progress.ConditionProgress.Clear();
            foreach (var deliverable in definition.Deliverables)
                progress.ConditionProgress[deliverable.Id] = 0;
        }
    }
}
