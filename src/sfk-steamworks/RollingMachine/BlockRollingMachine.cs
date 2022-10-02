using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SFK.Steamworks.RollingMachine
{
  public class BlockRollingMachine : Block
  {
    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
    {
      return true;
    }

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
  }
}
