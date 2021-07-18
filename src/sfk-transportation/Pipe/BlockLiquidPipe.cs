using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

using SFK.API;

namespace SFK.Transportation.Pipe
{
  public class BlockLiquidPipe : BlockPipe
  {
    public override bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
    {
      BlockPos pos = ownPos.AddCopy(side);
      Block block = world.BlockAccessor.GetBlock(pos);

      bool attrExists = block.Attributes?["liquidPipeConnect"][side.Code].Exists == true;

      if (attrExists)
      {
        return block.Attributes["liquidPipeConnect"][side.Code].AsBool(true);
      }

      if (world.BlockAccessor.GetBlockEntity(pos) is ILiquidFLow beFlow)
      {
        return block is BlockLiquidPipe
          || beFlow.LiquidPullFaces.Contains(side)
          || beFlow.LiquidPushFaces.Contains(side)
          || beFlow.AcceptLiquidFromFaces.Contains(side);
      };

      return false;
    }
  }
}
