using System;
using System.Linq;
using System.Text;
using SFK.Transportation.Pipe;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent.Mechanics;

namespace SFK.Transportation.Belt
{
    public class BEBehaviorBeltWithAxle : BEBehaviorMPBase
    {
        ICoreClientAPI capi;
        protected BlockFacing ownFacing;
        public BEBehaviorBeltWithAxle(BlockEntity blockentity) : base(blockentity)
        {
            Blockentity = blockentity;

            string orientation = blockentity.Block.Variant["orient"].ElementAt(0).ToString();
            ownFacing = BlockFacing.FromFirstLetter(orientation).GetCW();
            OutFacingForNetworkDiscovery = ownFacing.Opposite;
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            switch (Block.Variant["orient"])
            {
                case "n":
                case "s":
                case "ns":
                AxisSign = new int[] { -1, 0, 0 };
                break;

                case "e":
                case "w":
                case "ew":
                AxisSign = new int[] { 0, 0, -1 };
                break;
            }

            if (api.Side == EnumAppSide.Client)
            {
                capi = api as ICoreClientAPI;
            }
        }

        public override float GetResistance()
        {
            return 0.0005f;
        }
        
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);
            if (Api.World.EntityDebugMode)
            {
                string orientations = Block.Variant["orientation"];
                sb.AppendLine(string.Format(Lang.Get("Orientation: {0}", orientations)));
            }
        }
    }
}
