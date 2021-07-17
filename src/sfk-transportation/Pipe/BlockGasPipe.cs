using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SFK.Transportation.Pipe
{
  public class BlockGasPipe : BlockPipe
  {
    public override bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
    {
      Block block = world.BlockAccessor.GetBlock(ownPos.AddCopy(side));

      bool attrExists = block.Attributes?["gasPipeConnect"][side.Code].Exists == true;

      if (attrExists)
      {
        return block.Attributes["gasPipeConnect"][side.Code].AsBool(true);
      }

      return block is BlockGasPipe
        || block.EntityClass == "GasFlow"
        || block.EntityClass == "MultiblockGasFlow";
    }
  }
}