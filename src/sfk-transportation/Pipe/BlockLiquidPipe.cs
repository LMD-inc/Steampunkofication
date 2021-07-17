using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SFK.Transportation.Pipe
{
  public class BlockLiquidPipe : BlockPipe
  {
    public override bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
    {
      Block block = world.BlockAccessor.GetBlock(ownPos.AddCopy(side));

      bool attrExists = block.Attributes?["liquidPipeConnect"][side.Code].Exists == true;

      if (attrExists)
      {
        return block.Attributes["liquidPipeConnect"][side.Code].AsBool(true);
      }

      return block is BlockLiquidPipe
        || block is BlockLiquidContainerBase
        || block.EntityClass == "LiquidFlow"
        || block.EntityClass == "MultiblockLiquidFlow";
    }
  }
}