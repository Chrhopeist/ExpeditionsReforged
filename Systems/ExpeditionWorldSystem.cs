using ExpeditionsReforged.Content.NPCs;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Systems;

/// <summary>
/// World-level system responsible for spawning the Expedition Coordinator on new worlds.
/// </summary>
public class ExpeditionWorldSystem : ModSystem
{
    public override void OnWorldLoad()
    {
        // Multiplayer safety: only the server/world host should create NPC instances.
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            return;
        }

        int coordinatorType = ModContent.NPCType<ExpeditionChatNPC>();

        // Ensure we only ever have one coordinator at a time.
        if (NPC.AnyNPCs(coordinatorType))
        {
            return;
        }

        // Spawn near the world spawn with a small offset so we do not overlap the Guide.
        int spawnX = (Main.spawnTileX + 3) * 16;
        int spawnY = Main.spawnTileY * 16;
        IEntitySource source = new EntitySource_WorldEvent();

        NPC.NewNPC(source, spawnX, spawnY, coordinatorType);
    }
}
