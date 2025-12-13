using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ExpeditionsReforged.Players
{
    public class ExpeditionsPlayer : ModPlayer
    {
        public bool ExpeditionUIOpen;
        public bool TrackerUIOpen;

        public override void SaveData(TagCompound tag)
        {
            tag[nameof(ExpeditionUIOpen)] = ExpeditionUIOpen;
            tag[nameof(TrackerUIOpen)] = TrackerUIOpen;
        }

        public override void LoadData(TagCompound tag)
        {
            ExpeditionUIOpen = tag.GetBool(nameof(ExpeditionUIOpen));
            TrackerUIOpen = tag.GetBool(nameof(TrackerUIOpen));
        }
    }
}
