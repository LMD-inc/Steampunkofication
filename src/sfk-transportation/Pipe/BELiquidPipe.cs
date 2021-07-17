using SFK.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SFK.Transportation.Pipe
{
  public class BlockEntityLiquidPipe : BlockEntityLiquidFlow
  {
    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);

      InitLiquidFlowFromType();
    }

    private void InitLiquidFlowFromType()
    {
      string type = Block.Variant["type"];
      BlockFacing[] faces = new BlockFacing[type.Length];

      for (int i = 0; i < type.Length; i++)
      {
        faces[i] = BlockFacing.FromFirstLetter(type[i]);
      }

      LiquidPullFaces = LiquidPushFaces = AcceptLiquidFromFaces = faces;
    }
  }
}
