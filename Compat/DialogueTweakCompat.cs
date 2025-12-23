using System;
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

            try
            {
                // DialogueTweak Mod.Call string: "AddButton".
                // The Action argument is labeled "Hover Action" in DialogueTweak's ModCall implementation.
                dialogueTweak.Call(
                    "AddButton",
                    NPCID.Guide,
                    "Expedition",
                    "Head",
                    (Action)(() => Main.instance.MouseText("Expedition (test)"))
                );
            }
            catch (Exception ex)
            {
                mod.Logger.Warn("DialogueTweak compatibility call failed; skipping optional dialogue button.", ex);
            }
        }
    }
}
