using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
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

        float nwspeed = mpc.Network?.Speed ?? 0;
        nwspeed = Math.Abs(nwspeed * 3f) * mpc.GearedRatio;

        accumWater += dt * nwspeed;

        if (accumWater > 1)
        {
          accumWater = 0;

          if (slot.Empty)
          {
            slot.Itemstack = new ItemStack(Api.World.GetItem(new AssetLocation("game:waterportion")), 0);
          }
          
          float itemsPerLitre = BlockLiquidContainerBase.GetContainableProps(slot.Itemstack)?.ItemsPerLitre ?? 1f;

          if (slot.Itemstack?.StackSize >= CapacityLitres * itemsPerLitre) return;

          if (slot.Itemstack?.Item?.Code.ToString() == "game:waterportion")
          {
            slot.Itemstack.StackSize += (int)(1 * itemsPerLitre); // +1L
          }
        }

        Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
      }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
      dsc.Clear();

      base.GetBlockInfo(forPlayer, dsc);

      bool isPowered = mpc.Network?.Speed > 0.001f;
      hasWater = (Block as BlockWaterPump).CheckHasWater(Api.World, Pos, BlockFacing.FromCode(Block.Variant["side"]));

      if (hasWater && isPowered) dsc.AppendLine("Working");
      else
      {
        dsc.Append("Not working:");

        if (!hasWater)
        {
          dsc.Append(" have no water source in proper place;");
        }

        // TODO: detect if speed or torque not enough and write it?
        if (!isPowered)
        {
          dsc.Append(" need mechanical power;");
        }
      }

      
    }
  }
}
