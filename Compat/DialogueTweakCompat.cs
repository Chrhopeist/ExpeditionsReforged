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
        private const string TestButtonId = "DialogueTweakTest";
        private const string TestButtonText = "Test";

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

                // Proof-of-concept UI-only button for DialogueTweak integration.
                dialogueTweak.Call(
                    "RegisterButton",
                    TestButtonId,
                    TestButtonText,
                    null,
                    (Func<NPC, bool>)IsNpcEligibleForExpeditions,
                    (Action<NPC>)HandleTestButtonClick
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

        private static void HandleTestButtonClick(NPC npc)
        {
            // No-op: this button is a UI-only proof of concept.
        }
    }
}
