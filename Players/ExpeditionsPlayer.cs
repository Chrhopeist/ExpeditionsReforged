using System.Collections.Generic;
using System.Linq;
using ExpeditionsReforged.Content.Expeditions;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ExpeditionsReforged.Players
{
    public class ExpeditionsPlayer : ModPlayer
    {
        public bool ExpeditionUIOpen;
        public bool TrackerUIOpen;

        private readonly List<ExpeditionProgress> _expeditionProgressEntries = new();

        public IReadOnlyList<ExpeditionProgress> ExpeditionProgressEntries => _expeditionProgressEntries;

        public override void OnEnterWorld()
        {
            ExpeditionUIOpen = false;
            TrackerUIOpen = false;
        }

        public bool TryGetExpeditionProgress(string expeditionId, out ExpeditionProgress progress)
        {
            progress = _expeditionProgressEntries.FirstOrDefault(progressEntry => progressEntry.ExpeditionId == expeditionId);
            return progress is not null;
        }

        public override void SaveData(TagCompound tag)
        {
            if (_expeditionProgressEntries.Count == 0)
            {
                return;
            }

            tag["ExpeditionProgress"] = _expeditionProgressEntries
                .Select(progress => new TagCompound
                {
                    ["expeditionId"] = progress.ExpeditionId,
                    ["startGameTick"] = progress.StartGameTick,
                    ["isCompleted"] = progress.IsCompleted,
                    ["rewardsClaimed"] = progress.RewardsClaimed
                })
                .ToList();
        }

        public override void LoadData(TagCompound tag)
        {
            _expeditionProgressEntries.Clear();

            if (!tag.TryGet("ExpeditionProgress", out List<TagCompound> savedProgressEntries))
            {
                return;
            }

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
                    StartGameTick = entry.GetLong("startGameTick")
                };

                if (entry.GetBool("isCompleted"))
                {
                    progress.Complete();
                }

                if (entry.GetBool("rewardsClaimed"))
                {
                    progress.ClaimRewards();
                }

                _expeditionProgressEntries.Add(progress);
            }
        }
    }
}
