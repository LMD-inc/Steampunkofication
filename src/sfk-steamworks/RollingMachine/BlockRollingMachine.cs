using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace SFK.Steamworks.RollingMachine
{
  public class BlockRollingMachine : Block
  {
    #region Config

    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
    {
      return true;
    }

    #endregion

    #region Events

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BERollingMachine berm)
      {
        berm.OnBlockInteractStart(world, byPlayer, blockSel, blockSel.SelectionBoxIndex == 1 ? EnumRollingMachineSection.Handle : EnumRollingMachineSection.Base);
        return true;
      }

      return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BERollingMachine berm && blockSel.SelectionBoxIndex == 1)
      {
        return berm.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
      }

      return false;
    }

    public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BERollingMachine berm)
      {
        berm.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
      }

    }

    public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
    {
      if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BERollingMachine berm)
      {
        berm.IsRolling = false;
      }

      return true;
    }

    #endregion

    #region Blockinfo

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
      StringBuilder stb = new StringBuilder();

      BERollingMachine berm = world.BlockAccessor.GetBlockEntity(pos) as BERollingMachine;

      if (berm == null) return "n/a be";

      if (berm.RollersSlot.Empty && berm.WorkItemSlot.Empty) return "Empty";

      if (!berm.RollersSlot.Empty)
      {
        stb.AppendLine(Lang.Get("Rollers material: {0}", berm.RollersMaterial));
      }

      if (!berm.WorkItemSlot.Empty)
      {
        stb.AppendLine(Lang.Get("Currently working: {0}", berm.WorkItemStack.GetName()));
        RollingOutputStack output = berm.GetCurrentRecipe().Output;
        stb.AppendLine(Lang.Get("Will produce: {0} of {1}", output.Quantity, output.ResolvedItemstack.GetName()));

        double progress = Math.Round(berm.CurrentRollingProgress / berm.MaxRollingProgress * 10);

        if (progress > 0)
        {
          stb.AppendLine(Lang.Get($"Progress: ") + $"[{new String('█', (int)progress)}{new String('░', 10 - (int)progress)}]");
        }
      }

      return stb.ToString();
    }

    #endregion
  }
}
