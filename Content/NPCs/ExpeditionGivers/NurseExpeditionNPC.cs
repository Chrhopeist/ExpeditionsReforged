using ExpeditionsReforged.Content.NPCs;
using Terraria.ID;

namespace ExpeditionsReforged.Content.NPCs.ExpeditionGivers;

/// <summary>
/// Hooks expedition chat into the vanilla Nurse without changing any gameplay behavior.
/// </summary>
public sealed class NurseExpeditionNPC : ExpeditionChatNPC
{
    // Use the vanilla Nurse type so the NPC appears exactly as the base game version.
    public override int NPCType => NPCID.Nurse;
}
