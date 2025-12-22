using ExpeditionsReforged.Content.NPCs;
using Terraria.ID;

namespace ExpeditionsReforged.Content.NPCs.ExpeditionGivers;

/// <summary>
/// Hooks expedition chat into the vanilla Guide without changing any gameplay behavior.
/// </summary>
public sealed class GuideExpeditionNPC : ExpeditionChatNPC
{
    // Use the vanilla Guide type so the NPC appears exactly as the base game version.
    public override int NPCType => NPCID.Guide;
}
