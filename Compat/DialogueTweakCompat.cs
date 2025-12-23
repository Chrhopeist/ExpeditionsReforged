using System;
using ExpeditionsReforged.Players;
using ExpeditionsReforged.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Compat
{
    internal static class DialogueTweakCompat
    {
        private const string ExpeditionButtonId = "Expedition";
        private const string ExpeditionButtonText = "Expedition";

        internal static void TryRegisterDialogueButtons()
        {
            if (Main.dedServ)
            {
                return;
            }

            if (!ModLoader.TryGetMod("DialogueTweak", out Mod dialogueTweak))
            {
                return;
            }

            // DialogueTweak integration is optional; fail silently if its API changes.
            try
            {
                dialogueTweak.Call(
                    "RegisterButton",
                    ExpeditionButtonId,
                    ExpeditionButtonText,
                    null,
                    (Func<NPC, bool>)IsNpcEligibleForExpeditions,
                    (Action<NPC>)HandleExpeditionButtonClick
                );
            }
            catch (Exception)
            {
                // DialogueTweak is optional; Mod.Call errors should not block mod loading.
            }
        }

        private static bool IsNpcEligibleForExpeditions(NPC npc)
        {
            // DialogueTweak should only show the Expeditions button for the Guide.
            return npc != null && npc.type == NPCID.Guide;
        }

        private static void HandleExpeditionButtonClick(NPC npc)
        {
            if (Main.dedServ)
            {
                return;
            }

            ExpeditionsPlayer expeditionsPlayer = Main.LocalPlayer.GetModPlayer<ExpeditionsPlayer>();
            // Track the requested UI state even if the UI layer is recreated later.
            expeditionsPlayer.ExpeditionUIOpen = true;

            // Open the Expeditions UI without interfering with the NPC chat window.
            ModContent.GetInstance<ExpeditionsSystem>().OpenExpeditionUi();
        }
    }
}
