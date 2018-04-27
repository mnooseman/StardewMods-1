using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace RentedTools
{
    public interface IRentedTool
    {

    }

    public class RentedAxe : Axe, IRentedTool
    {
        public RentedAxe() : base()
        {
            this.name = "RentedAxe";
        }
    }

    public class RentedPickaxe : Pickaxe, IRentedTool
    {
        public RentedPickaxe() : base()
        {
            this.name = "RentedPickaxe";
        }
    }

    public class RentedHoe : Hoe, IRentedTool
    {
        public RentedHoe() : base()
        {
            this.name = "RentedHoe";
        }
    }

    public class RentedWateringCan : WateringCan, IRentedTool
    {
        public RentedWateringCan() : base()
        {
            // this.name = "RentedWateringCan";
        }
    }
}
