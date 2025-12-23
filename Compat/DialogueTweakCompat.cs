using System;
using ExpeditionsReforged.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Compat
{
    internal static class DialogueTweakCompat
    {
        internal static void RegisterDialogueButton(Mod mod)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (!ModLoader.TryGetMod("DialogueTweak", out Mod dialogueTweak))
            {
                return;
            }

            bool warnedOnce = false;
            bool supportsAvailabilityPredicate = true;

            void LogWarningOnce(Exception exception)
            {
                if (warnedOnce)
                {
                    return;
                }

                warnedOnce = true;
                mod.Logger.Warn("DialogueTweak compatibility call failed; skipping optional dialogue button.", exception);
            }

            for (int npcId = 0; npcId < NPCID.Count; npcId++)
            {
                if (!NPCID.Sets.TownNPC[npcId])
                {
                    continue;
                }

                // DialogueTweak Mod.Call string: "AddButton".
                // The Action argument is labeled "Hover Action" in DialogueTweak's ModCall implementation.
                // "Head" uses DialogueTweak's NPC head placeholder icon identifier.
                Action hoverAction = () => Main.instance.MouseText("View available expeditions");
                Func<bool> availabilityPredicate = () => ExpeditionService.IsExpeditionGiver(npcId, Main.LocalPlayer);

                // DialogueTweak may change its Mod.Call signature, so we attempt the predicate overload first
                // and fall back to the base overload if it fails.
                try
                {
                    if (supportsAvailabilityPredicate)
                    {
                        dialogueTweak.Call(
                            "AddButton",
                            npcId,
                            "Expedition",
                            "Head",
                            hoverAction,
                            availabilityPredicate
                        );
                    }
                    else
                    {
                        dialogueTweak.Call(
                            "AddButton",
                            npcId,
                            "Expedition",
                            "Head",
                            hoverAction
                        );
                    }
                }
                catch (Exception ex)
                {
                    bool loggedFailure = false;

                    if (supportsAvailabilityPredicate)
                    {
                        supportsAvailabilityPredicate = false;

                        try
                        {
                            dialogueTweak.Call(
                                "AddButton",
                                npcId,
                                "Expedition",
                                "Head",
                                hoverAction
                            );
                        }
                        catch (Exception retryException)
                        {
                            LogWarningOnce(retryException);
                            loggedFailure = true;
                        }
                    }

                    if (!loggedFailure)
                    {
                        LogWarningOnce(ex);
                    }
                }
            }
        }
    }
}
