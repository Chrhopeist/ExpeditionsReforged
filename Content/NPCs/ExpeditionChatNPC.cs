using ExpeditionsReforged.Systems;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Content.NPCs;

/// <summary>
/// Expedition Coordinator NPC that opens the expedition UI from the chat buttons.
/// </summary>
public class ExpeditionChatNPC : ModNPC
{
    // Use a vanilla texture as a placeholder until custom art is added.
    public override string Texture => $"Terraria/Images/NPC_{NPCID.Guide}";

    public override void SetStaticDefaults()
    {
        // Acts like a town NPC without being bound to town housing or the happiness system.
        NPCID.Sets.ActsLikeTownNPC[Type] = true;
        NPCID.Sets.NoTownNPCHappiness[Type] = true;
        NPCID.Sets.SpawnsWithCustomName[Type] = true;

        // Use town NPC animation frames and behaviors.
        Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Guide];
    }

    public override void SetDefaults()
    {
        NPC.friendly = true;
        NPC.width = 18;
        NPC.height = 40;
        NPC.aiStyle = NPCAIStyleID.Passive;
        NPC.damage = 10;
        NPC.defense = 15;
        NPC.lifeMax = 250;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
        NPC.knockBackResist = 0.5f;

        // Reuse a town NPC animation profile so the placeholder texture animates correctly.
        AnimationType = NPCID.Guide;
    }

    public override bool CanChat()
    {
        // ActsLikeTownNPC does not automatically allow chat.
        return true;
    }

    public override List<string> SetNPCNameList()
    {
        // Provide a consistent name for the coordinator.
        return new List<string> { "Expedition Coordinator" };
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        // Spawn the coordinator on the surface in peaceful conditions if one is not already present.
        if (NPC.AnyNPCs(Type) || !spawnInfo.Player.ZoneOverworldHeight)
        {
            return 0f;
        }

        return 0.08f;
    }

    public override void SetChatButtons(ref string button, ref string button2)
    {
        // Always show the expedition button when chatting with the coordinator.
        button2 = "Expedition";
    }

    public override void OnChatButtonClicked(bool firstButton, ref string shop)
    {
        if (firstButton)
        {
            return;
        }

        // Client-only UI: open the expedition list for this NPC without mutating gameplay state.
        if (Main.dedServ)
        {
            return;
        }

        ModContent.GetInstance<ExpeditionsSystem>().OpenNpcExpeditionUI(NPC.type);
    }
}
