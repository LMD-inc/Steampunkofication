using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

using SFK.API;

namespace SFK.Transportation.Pipe
{
  public class BlockLiquidPipe : BlockPipe
  {
    public override bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
    {
      BlockPos pos = ownPos.AddCopy(side);
      Block block = world.BlockAccessor.GetBlock(pos);

      JsonObject liquidPipeConnect = block.Attributes?["liquidPipeConnect"];

      if (liquidPipeConnect?.Exists == true)
      {
        if (liquidPipeConnect[side.Code].Exists == true)
        {
          return liquidPipeConnect[side.Code].AsBool(true);
        }

        return false;
      }

      ILiquidFLow beFlow = world.BlockAccessor.GetBlockEntity(pos) as ILiquidFLow;

      if (beFlow != null)
      {
        return block is BlockLiquidPipe
            || beFlow.LiquidPullFaces.Contains(side)
            || beFlow.LiquidPushFaces.Contains(side)
            || beFlow.AcceptLiquidFromFaces.Contains(side);
      }

      return false;
      // TODO: think do we connect to ALL liquid containers
      // return block is BlockLiquidContainerBase;
    }
  }

}
