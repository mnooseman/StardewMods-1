using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using StardewValley.Menus;

namespace RentedTools
{   
    public class ModEntry : Mod
    {
        private bool inited;
        private StardewValley.Farmer player;
        private NPC blacksmithNpc;
        private bool shuoldCreateFailedToRentTools;
        private bool rentedToolsOffered;
        private bool recycleOffered;

        private Dictionary<Tuple<List<Item>, int>, Item> rentedToolRefs;


        private List<Vector2> blackSmithCounterTiles = new List<Vector2>();

        public override void Entry(IModHelper helper)
        {
            SaveEvents.BeforeSave += this.BeforeSaveHandler;
            SaveEvents.AfterSave += this.AfterSaveHandler;
            SaveEvents.AfterLoad += this.Bootstrap;
            MenuEvents.MenuClosed += this.MenuCloseHandler;
        }

        private void Bootstrap(object sender, EventArgs e)
        {
            // params reset
            this.inited = false;
            this.player = null;
            this.blacksmithNpc = null;

            this.shuoldCreateFailedToRentTools = false;
            this.rentedToolsOffered = false;
            this.recycleOffered = false;

            this.rentedToolRefs = new Dictionary<Tuple<List<Item>, int>, Item>();
            this.blackSmithCounterTiles = new List<Vector2>();

            // params init
            this.player = Game1.player;
            this.blackSmithCounterTiles.Add(new Vector2(3f, 15f));
            foreach(NPC npc in Utility.getAllCharacters())
            {
                if (npc.name == "Clint")
                {
                    this.blacksmithNpc = npc;
                    break;
                }
            }

            if (this.blacksmithNpc == null)
            {
                Monitor.Log("blacksmith NPC not found", LogLevel.Info);
            }

            // init done
            this.inited = true;
        }

        private void MenuCloseHandler(object sender, EventArgsClickableMenuClosed e)
        {

            if (this.shuoldCreateFailedToRentTools)
            {
                this.SetupFailedToRentDialog(this.player);
                this.shuoldCreateFailedToRentTools = false;
                return;
            }

            if (this.rentedToolsOffered)
            {
                this.rentedToolsOffered = false;
                return;
            }

            if (this.recycleOffered)
            {
                this.recycleOffered = false;
                return;
            }
            
            if (this.inited && this.IsPlayerAtCounter(this.player))
            {
                if (this.ShouldRecycleTools(this.player))
                {
                    this.SetupRentToolsRemovalDialog(this.player);
                }
                else if (this.ShouldOfferTools(this.player))
                {
                    this.SetupRentToolsOfferDialog(this.player);
                }
            }
        }

        private bool IsPlayerAtCounter(StardewValley.Farmer who)
        {
            return who.currentLocation.name == "Blacksmith" && this.blackSmithCounterTiles.Contains(who.getTileLocation());
        }

        private bool ShouldRecycleTools(StardewValley.Farmer who)
        {
            bool result = false;

            List<Item> inventory = who.items;
            List<Item> tools = inventory
                .Where(tool => tool is Axe || tool is Pickaxe || tool is WateringCan || tool is Hoe)
                .ToList();
            List<Item> rentedTools = inventory
                .Where(tool => tool is IRentedTool)
                .ToList();

            if (rentedTools.Any())
            {
                if (who.toolBeingUpgraded == null)
                {
                    result = true;
                }
                else
                {
                    foreach (Item item in rentedTools)
                    {
                        if (
                            (item is RentedAxe && tools.Contains(new Axe())) ||
                            (item is RentedPickaxe && tools.Contains(new Pickaxe())) ||
                            (item is RentedWateringCan && tools.Contains(new WateringCan())) ||
                            (item is RentedHoe && tools.Contains(new Hoe()))
                           )
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private bool ShouldOfferTools(StardewValley.Farmer who)
        {
            List<Item> inventory = who.items;
            List<Item> tools = inventory
                .Where(tool => tool is Axe || tool is Pickaxe || tool is WateringCan || tool is Hoe)
                .ToList();
            List<Item> rentedTools = inventory
                .Where(tool => tool is IRentedTool)
                .ToList();

            return (!rentedToolsOffered && who.toolBeingUpgraded != null && !rentedTools.Any());
        }

        private void SetupRentToolsRemovalDialog(StardewValley.Farmer who)
        {
            who.currentLocation.createQuestionDialogue(
                Game1.content.LoadString("Strings\\JarvieK_RentedTools:Blacksmith_RecycleTools_Menu"),
                new Response[2]
                {
                    new Response("Confirm", Game1.content.LoadString("Strings\\JarvieK_RentedTools:Blacksmith_RecycleToolsMenu_Confirm")),
                    new Response("Leave", Game1.content.LoadString("Strings\\JarvieK_RentedTools:Blacksmith_RecycleToolsMenu_Leave")),
                },
                (StardewValley.Farmer whoInCallback, String whichAnswer) =>
                {
                    switch (whichAnswer)
                    {
                        case "Confirm":
                            this.RecycleTempTools(whoInCallback);
                            break;
                        case "Leave":
                            // do nothing
                            break;
                    }
                    return;
                },
                this.blacksmithNpc
            );
            this.recycleOffered = true;
        }

        private void SetupRentToolsOfferDialog(StardewValley.Farmer who)
        {
            who.currentLocation.createQuestionDialogue(
                Game1.content.LoadString(
                    "Strings\\JarvieK_RentedTools:Blacksmith_OfferTools_Menu",
                    GetRentedToolByTool(who.toolBeingUpgraded).DisplayName, who.toolBeingUpgraded.DisplayName
                ),
                new Response[2]
                {
                    new Response("Confirm", Game1.content.LoadString("Strings\\JarvieK_RentedTools:Blacksmith_OfferToolsMenu_Confirm")),
                    new Response("Leave", Game1.content.LoadString("Strings\\JarvieK_RentedTools:Blacksmith_OfferToolsMenu_Leave")),
                },
                (StardewValley.Farmer whoInCallback, String whichAnswer) =>
                {
                    switch (whichAnswer)
                    {
                        case "Confirm":
                            this.BuyTempTool(whoInCallback);
                            break;
                        case "Leave":
                            // do nothing
                            break;
                    }
                    return;
                },
                this.blacksmithNpc
            );
            rentedToolsOffered = true;
        }

        private void SetupFailedToRentDialog(StardewValley.Farmer who)
        {
            if (who.freeSpotsInInventory() <= 0)
            {
                Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\JarvieK_RentedTools:Blacksmith_NoInventorySpace"));
            }
            else
            {
                Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\JarvieK_RentedTools:Blacksmith_InsufficientFundsToRentTool"));
            }
            
        }

        private Tool GetRentedToolByTool(Item tool)
        {
            if (tool is Axe)
            {
                return new RentedAxe();
            }
            else if (tool is Pickaxe)
            {
                return new RentedPickaxe();
            }
            else if (tool is WateringCan)
            {
                return new RentedWateringCan();
            }
            else if (tool is Hoe)
            {
                return new RentedHoe();
            }
            else
            {
                Monitor.Log($"unsupported upgradable tool: {tool.ToString()}");
                return null;
            }
        }

        private void BuyTempTool(StardewValley.Farmer who)
        {
            // NOTE: there's no thread safe method for money transactions, so I suppose the game doesn't care about it as well?
            Item toolToBuy;
            int toolCost;

            // TODO: handle upgradeLevel so rented tool is not always the cheapest

            toolToBuy = this.GetRentedToolByTool(who.toolBeingUpgraded);

            if (toolToBuy == null)
            {
                return;
            }

            toolCost = this.GetToolCost(toolToBuy);

            if (who.money >= toolCost && who.freeSpotsInInventory() > 0)
            {
                ShopMenu.chargePlayer(who, 0, toolCost);
                who.addItemToInventory(toolToBuy);
            }
            else
            {
                this.shuoldCreateFailedToRentTools = true;
            }

        }

        private void RecycleTempTools(StardewValley.Farmer who)
        {
            // recycle all rented tools

            while (who.items.Any(item => item is IRentedTool))
            {
                foreach (Item item in who.items)
                {
                    if (item is IRentedTool)
                    {
                        who.removeItemFromInventory(item);
                        break;
                    }
                }

            }

            
        }

        private int GetToolCost(Item tool)
        {
            // TODO: this function is subject to change
            return 200;
        }

        private void BeforeSaveHandler(object sender, EventArgs e)
        {
            rentedToolRefs.Clear();

            // NOTE: Farmer.addItemToInventory()'s implementation uses raw mapping of item position and index in Farmer.items
            // i.e. items[10] means the 10th item in player's inventory, I suppose it's subject to change unless the game provides
            // and uses a method that would return index of item after inserting.

            // get rented tool references

            for (int i = 0; i < this.player.items.Count; i++)
            {
                if (this.player.items[i] is IRentedTool)
                {
                    this.rentedToolRefs.Add(new Tuple<List<Item>, int>(this.player.items, i), this.player.items[i]);
                    Monitor.Log($"rented tool found: {this.player.items[i]} in player's inventory");
                }
            }

            // loop through all chests
            foreach (GameLocation loc in Game1.locations)
            {
                foreach (var key in loc.objects.Keys)
                {
                    if (loc.objects[key] is StardewValley.Objects.Chest)
                    {
                        // loop through each chest
                        List<Item> currItems = ((StardewValley.Objects.Chest)loc.objects[key]).items;
                        for (int i = 0; i < currItems.Count(); i++)
                        {
                            if (currItems[i] is IRentedTool)
                            {
                                this.rentedToolRefs.Add(new Tuple<List<Item>, int>(currItems, i), currItems[i]);
                                Monitor.Log($"rented tool found: {currItems[i].ToString()} in {loc.objects[key]}");
                            }
                        }
                    }
                }
            }

            // remove rented tools from all inventories

            foreach(KeyValuePair<Tuple<List<Item>, int>, Item> pair in this.rentedToolRefs.Reverse())
            {
                Monitor.Log($"removing {pair.Value.ToString()} in {pair.Key.Item1.ToString()} at index {pair.Key.Item2}");
                pair.Key.Item1.RemoveAt(pair.Key.Item2);
            }
        }

        private void AfterSaveHandler(object sender, EventArgs e)
        {
            // load rented tools back to player's inventory

            foreach (KeyValuePair<Tuple<List<Item>, int>, Item> pair in this.rentedToolRefs)
            {
                Monitor.Log($"addint {pair.Value.ToString()} to {pair.Key.Item1.ToString()} at index {pair.Key.Item2}");
                pair.Key.Item1.Insert(pair.Key.Item2, pair.Value);
            }

            rentedToolRefs.Clear();
        }
    }
}