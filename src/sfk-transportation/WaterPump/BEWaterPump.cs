using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent.Mechanics;
using SFK.API;
using System.Text;

namespace SFK.Transportation.WaterPump
{
  public class BEWaterPump : BlockEntityLiquidFlow
  {
    private BEBehaviorWaterPump mpc;
    private float accumWater;
    bool automated;

    #region Config

    public override string InventoryClassName => "waterpump";
    private bool hasWater = false;
    public bool HasWater => hasWater;

    #endregion

    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);

      if (api.Side == EnumAppSide.Server)
      {
        RegisterGameTickListener(GenerateWater, 200);
      }
    }

    public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
    {
      base.CreateBehaviors(block, worldForResolve);

      mpc = GetBehavior<BEBehaviorWaterPump>();
    }

    private void GenerateWater(float dt)
    {
      if (Api?.Side == EnumAppSide.Server)
      {
        // MB make more rare checks;
        hasWater = (Block as BlockWaterPump).CheckHasWater(Api.World, Pos, BlockFacing.FromCode(Block.Variant["side"]));

        if (!hasWater) return;

        ItemSlot slot = Inventory[0];

        if (slot.Itemstack?.StackSize >= CapacityLitresPerSlot[0]) return;

        float nwspeed = mpc.Network?.Speed ?? 0;
        nwspeed = Math.Abs(nwspeed * 3f) * mpc.GearedRatio;

        accumWater += dt * nwspeed;

        if (accumWater > 1)
        {
          accumWater = 0;

          if (slot.Empty)
          {
            slot.Itemstack = new ItemStack(Api.World.GetItem(new AssetLocation("game:waterportion")), 1);
          }
          else
          {
            if (slot.Itemstack?.Item?.Code.ToString() == "game:waterportion")
            {
              slot.Itemstack.StackSize += 1;
            }
          }
        }

        Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
      }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
      base.GetBlockInfo(forPlayer, dsc);

      // if (!hasWater)
      // {
      //   dsc.Append("not working. Has no water source in proper place.");
      // }
    }
  }
}
