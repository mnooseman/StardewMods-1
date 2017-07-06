using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
// using System.Threading.Tasks;

namespace SelfServe
{
    public class ModEntry : Mod
    {
        private List<Vector2> seedShopCounterTiles = new List<Vector2>();
        private List<Vector2> animalShopCounterTiles = new List<Vector2>();
        private List<Vector2> CarpentersShopCounterTiles = new List<Vector2>();
        private Dictionary<String, NPC> npcRefs = new Dictionary<string, NPC>();

        private bool inited = false;

        private void Bootstrap(object Sender, EventArgs e)
        {
            seedShopCounterTiles.Add(new Vector2(4f, 19f));
            seedShopCounterTiles.Add(new Vector2(5f, 19f));

            animalShopCounterTiles.Add(new Vector2(12f, 16f));
            animalShopCounterTiles.Add(new Vector2(13f, 16f));

            CarpentersShopCounterTiles.Add(new Vector2(8f, 20f));

            foreach(NPC npc in Utility.getAllCharacters())
            {
                switch (npc.name)
                {
                    case "Pierre":
                    case "Robin":
                    case "Marnie":
                       npcRefs[npc.name] = npc;
                        break;
                }
            }

            foreach(var item in npcRefs)
            {
                Monitor.Log(item.ToString());
            }

            this.inited = true;
        }

        public override void Entry(IModHelper helper)
        {
            ControlEvents.KeyPressed += this.KeyEventHandler;
            ControlEvents.ControllerButtonPressed += this.ControllerEventHandler;
            // ControlEvents.MouseChanged += this.mouseEventHandler; // this would be too ugly to implement with current version of SMAPI
            SaveEvents.AfterLoad += this.Bootstrap;
        }

        private void KeyEventHandler(object sender, EventArgsKeyPressed e)
        {   
            if (inited && OpenMenuHandler(Array.Exists(Game1.options.actionButton, item => e.KeyPressed.Equals(item.key))))
            {
                Game1.oldKBState = Keyboard.GetState();
            }
        }

        private void ControllerEventHandler(object sender, EventArgsControllerButtonPressed e)
        {
            // NOTE: looks like the game has hard coded  button to key mappings, isActionKey() is subject to change if customized key mapipng is allowed in the future
            // See code below:
            //public static Keys mapGamePadButtonToKey(Buttons b)
            //{
            //    if (b == Buttons.A)
            //        return Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.actionButton);

            if (inited && OpenMenuHandler(e.ButtonPressed.Equals(Buttons.A)))
            {
                Game1.oldPadState = GamePad.GetState(PlayerIndex.One);
            }
        }

        private bool OpenMenuHandler(bool isActionKey)
        {
            // returns true if menu is opened, otherwise false

            String locationString = Game1.player.currentLocation.name;
            Vector2 playerPosition = Game1.player.getTileLocation();
            int faceDirection = Game1.player.getFacingDirection();

            bool result = false; // default

            if (ShouldOpen(isActionKey, Game1.player.getFacingDirection(), locationString, playerPosition))
            {
                // NOTE: the game won't set dialogue if there's an active menu, so no more warpping magics lol

                result = true;
                switch (locationString)
                {
                    case "SeedShop":
                        Game1.player.currentLocation.createQuestionDialogue(
                            Game1.content.LoadString("Strings\\JarvieK_SelfService:SeedShop_Menu"),
                            new Response[2]
                            {
                                new Response("Shop", Game1.content.LoadString("Strings\\JarvieK_SelfService:SeedShopMenu_Shop")),
                                new Response("Leave", Game1.content.LoadString("Strings\\JarvieK_SelfService:SeedShopMenu_Leave"))
                            },
                            delegate(StardewValley.Farmer who, string whichAnswer)
                            {
                                switch (whichAnswer)
                                {
                                    case "Shop":
                                        Game1.activeClickableMenu = (IClickableMenu)new ShopMenu(Utility.getShopStock(true), 0, "Pierre");
                                        break;
                                    case "Leave":
                                        // do nothing
                                        break;
                                    default:
                                        Monitor.Log($"invalid dialogue answer: {whichAnswer}", LogLevel.Info);
                                        break;
                                }
                            }

                        );
                        break;
                    case "AnimalShop":
                        Game1.player.currentLocation.createQuestionDialogue(
                            Game1.content.LoadString("Strings\\JarvieK_SelfService:AnimalShop_Menu"),
                            new Response[3]
                            {
                                new Response("Supplies", Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Supplies")),
                                new Response("Purchase", Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Animals")),
                                new Response("Leave", Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Leave"))
                            },
                            "Marnie"
                        );
                        break;
                    case "ScienceHouse":
                        if (Game1.player.daysUntilHouseUpgrade < 0 && !Game1.getFarm().isThereABuildingUnderConstruction())
                        {
                            Response[] answerChoices;
                            if (Game1.player.houseUpgradeLevel < 3)
                                answerChoices = new Response[4]
                                {
                                    new Response("Shop", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Shop")),
                                    new Response("Upgrade", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_UpgradeHouse")),
                                    new Response("Construct", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Construct")),
                                    new Response("Leave", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Leave"))
                                };
                            else
                                answerChoices = new Response[3]
                                {
                                    new Response("Shop", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Shop")),
                                    new Response("Construct", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Construct")),
                                    new Response("Leave", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Leave"))
                                };

                            Game1.player.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\JarvieK_SelfService:ScienceHouse_CarpenterMenu"), answerChoices, "carpenter");
                        }
                        else
                        {
                            Game1.activeClickableMenu = (IClickableMenu)new ShopMenu(Utility.getCarpenterStock(), 0, "Robin");
                        }
                        break;
                    default:
                        Monitor.Log($"invalid location: {locationString}", LogLevel.Info);
                        break;
                }
            }

            return result;

        }

        private bool ShouldOpen(bool isActionKey, int facingDirection, String locationString, Vector2 playerLocation)
        {
            Monitor.Log($"{locationString} {playerLocation.ToString()}");
            bool result = false;
            if (Game1.activeClickableMenu == null && isActionKey && facingDirection == 3) // somehow SMAPI doesn't provide enum for facing directions?
            {
                // TODO: refactor this part to avoid hard coded tile locations
                switch (locationString)
                {
                    case "SeedShop":
                        result = npcRefs["Pierre"].currentLocation.name != locationString || !npcRefs["Pierre"].getTileLocation().Equals(new Vector2(4f, 17f)) && this.seedShopCounterTiles.Contains(playerLocation);
                        break;
                    case "AnimalShop":
                        result = npcRefs["Marnie"].currentLocation.name != locationString || !npcRefs["Marnie"].getTileLocation().Equals(new Vector2(12f, 14f)) && this.animalShopCounterTiles.Contains(playerLocation);
                        break;
                    case "ScienceHouse":
                        result = npcRefs["Robin"].currentLocation.name != locationString || !npcRefs["Robin"].getTileLocation().Equals(new Vector2(8f, 18f)) && this.CarpentersShopCounterTiles.Contains(playerLocation);
                        break;
                    default:
                        // Monitor.Log($"no shop at location {locationString}", LogLevel.Info);
                        break;
                }
            }

            return result;
        }
    }
}
