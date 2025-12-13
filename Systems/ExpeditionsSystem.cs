using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using ExpeditionsReforged.UI;
using ExpeditionsReforged.Players;

namespace ExpeditionsReforged.Systems
{
    public class ExpeditionsSystem : ModSystem
    {
        private UserInterface _expeditionInterface;
        private UserInterface _trackerInterface;

        private ExpeditionUI _expeditionUI;
        private TrackerUI _trackerUI;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            _expeditionInterface = new UserInterface();
            _trackerInterface = new UserInterface();

            _expeditionUI = new ExpeditionUI();
            _expeditionUI.Activate();
            _expeditionInterface.SetState(_expeditionUI);

            _trackerUI = new TrackerUI();
            _trackerUI.Activate();
            _trackerInterface.SetState(_trackerUI);
        }

        public override void Unload()
        {
            _expeditionInterface = null;
            _trackerInterface = null;

            _expeditionUI = null;
            _trackerUI = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            _expeditionInterface?.Update(gameTime);
            _trackerInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            ExpeditionsPlayer expeditionsPlayer = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();

            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "ExpeditionsReforged: Tracker UI",
                    delegate
                    {
                        if (expeditionsPlayer != null && expeditionsPlayer.TrackerUIOpen && _trackerInterface?.CurrentState != null)
                        {
                            _trackerInterface.Draw(Main.spriteBatch, new GameTime());
                        }

                        return true;
                    },
                    InterfaceScaleType.UI));

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "ExpeditionsReforged: Expedition UI",
                    delegate
                    {
                        if (expeditionsPlayer != null && expeditionsPlayer.ExpeditionUIOpen && _expeditionInterface?.CurrentState != null)
                        {
                            _expeditionInterface.Draw(Main.spriteBatch, new GameTime());
                        }

                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }
    }
}
