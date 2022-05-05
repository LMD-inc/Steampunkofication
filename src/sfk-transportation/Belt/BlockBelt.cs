using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SFK.Transportation.Pipe
{
  public class BlockBelt : Block
  {
    #region Connecting variants

    public string GetOrientations(IWorldAccessor world, BlockPos pos)
    {
      string orientations =
          GetBeltCode(world, pos, BlockFacing.NORTH) +
          GetBeltCode(world, pos, BlockFacing.EAST) +
          GetBeltCode(world, pos, BlockFacing.SOUTH) +
          GetBeltCode(world, pos, BlockFacing.WEST)
      ;

      if (orientations.Length == 0) orientations = "empty";
      return orientations;
    }

    private string GetBeltCode(IWorldAccessor world, BlockPos pos, BlockFacing facing)
    {
      if (ShouldConnectAt(world, pos, facing)) return "" + facing.Code[0];
      return "";
    }

    public virtual bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
    {
      Block block = world.BlockAccessor.GetBlock(ownPos.AddCopy(side));

      bool attrExists = block.Attributes?["beltConnect"][side.Code].Exists == true;

      if (attrExists)
      {
        return block.Attributes["beltConnect"][side.Code].AsBool(true);
      }

      return block is BlockBelt;
    }

    #endregion

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    {
      string orientations = GetOrientations(world, blockSel.Position);
      Block block = world.BlockAccessor.GetBlock(CodeWithVariant("type", orientations));

      if (block == null) block = this;

      if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
      {
        world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
        return true;
      }

      return false;
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
    {
      string orientations = GetOrientations(world, pos);

      AssetLocation newBlockCode = CodeWithVariant("type", orientations);

      if (!Code.Equals(newBlockCode))
      {
        Block block = world.BlockAccessor.GetBlock(newBlockCode);
        if (block == null) return;

        world.BlockAccessor.SetBlock(block.BlockId, pos);
        world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
      }
      else
      {
        base.OnNeighbourBlockChange(world, pos, neibpos);
      }
    }
  }
}
