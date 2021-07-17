using SFK.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SFK.Transportation.GasPipe
{
  public class BlockEntityGasPipe : BlockEntityGasFlow
  {
    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);

      InitGasFlowFromType();
    }

    private void InitGasFlowFromType()
    {
      string type = Block.Variant["type"];
      BlockFacing[] faces = new BlockFacing[type.Length];

      for (int i = 0; i < type.Length; i++)
      {
        faces[i] = BlockFacing.FromFirstLetter(type[i]);
      }

      GasPullFaces = GasPushFaces = AcceptGasFromFaces = faces;
    }
  }
}