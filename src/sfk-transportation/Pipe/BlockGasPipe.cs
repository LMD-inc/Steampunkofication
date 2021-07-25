using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

using SFK.API;

namespace SFK.Transportation.Pipe
{
  public class BlockGasPipe : BlockPipe
  {
    public override bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
    {
      BlockPos pos = ownPos.AddCopy(side);
      Block block = world.BlockAccessor.GetBlock(pos);


      bool attrExists = block.Attributes?["gasPipeConnect"][side.Opposite.Code].Exists == true;

      if (attrExists)
      {
        return block.Attributes["gasPipeConnect"][side.Opposite.Code].AsBool(true);
      }

      if (world.BlockAccessor.GetBlockEntity(pos) is IGasFLow beFlow)
      {
        return block is BlockGasPipe
          || beFlow.GasPullFaces.Contains(side)
          || beFlow.GasPushFaces.Contains(side)
          || beFlow.AcceptGasFromFaces.Contains(side);
      };

      return false;
    }
  }
}
