using ExpeditionsReforged.Content.NPCs;
using Terraria.ID;

namespace ExpeditionsReforged.Content.NPCs.ExpeditionGivers;

/// <summary>
/// Hooks expedition chat into the vanilla Demolitionist without changing any gameplay behavior.
/// </summary>
public sealed class DemolitionistExpeditionNPC : ExpeditionChatNPC
{
    // Use the vanilla Demolitionist type so the NPC appears exactly as the base game version.
    public override int NPCType => NPCID.Demolitionist;
}
