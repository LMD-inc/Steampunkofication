using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent.Mechanics;
using SFK.API;

namespace SFK.Transportation.WaterPump
{
  public class BEWaterPump : BlockEntityLiquidFlow
  {
    private BEBehaviorMPConsumer mpcBh;
    private float accumWater;
    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);

      if (api.Side == EnumAppSide.Server)
      {
        RegisterGameTickListener(GenerateWater, 200);
      }

      mpcBh = GetBehavior<BEBehaviorMPConsumer>();
    }
    private void GenerateWater(float dt)
    {
      if (Api?.Side == EnumAppSide.Server)
      {
        ItemSlot slot = Inventory[0];

        if (slot.Itemstack?.StackSize >= CapacityLitresPerSlot[0]) return;

        float nwspeed = mpcBh.Network?.Speed ?? 0;
        nwspeed = Math.Abs(nwspeed * 3f) * mpcBh.GearedRatio;

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
  }
}
