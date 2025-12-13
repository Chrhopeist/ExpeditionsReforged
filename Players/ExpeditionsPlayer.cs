using Terraria.ModLoader;

namespace ExpeditionsReforged.Players
{
    public class ExpeditionsPlayer : ModPlayer
    {
        public bool ExpeditionUIOpen;
        public bool TrackerUIOpen;

        public override void OnEnterWorld()
        {
            ExpeditionUIOpen = false;
            TrackerUIOpen = false;
        }
    }
}
