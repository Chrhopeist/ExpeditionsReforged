using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExpeditionsReforged.Systems
{
    public class ExpeditionsSystem : ModSystem
    {
        private UserInterface _expeditionInterface;
        private UserInterface _trackerInterface;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            _expeditionInterface = new UserInterface();
            _trackerInterface = new UserInterface();
        }

        public override void Unload()
        {
            _expeditionInterface = null;
            _trackerInterface = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            _expeditionInterface?.Update(gameTime);
            _trackerInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // UI layers will be inserted here once UIState implementations exist.
        }
    }
}
