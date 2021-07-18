using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

using SFK.API;

namespace SFK.Transportation.Pipe
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

      if (type == null || type == "empty") return;

      BlockFacing[] faces = new BlockFacing[type.Length];

      for (int i = 0; i < type.Length; i++)
      {
        faces[i] = BlockFacing.FromFirstLetter(type[i]);
      }

      GasPullFaces = GasPushFaces = AcceptGasFromFaces = faces;
    }

    public override void TryPullFrom(BlockFacing inputFace)
    {
      BlockPos InputPosition = Pos.AddCopy(inputFace);

      if (Api.World.BlockAccessor.GetBlockEntity(InputPosition) is BlockEntityGasFlow beGs)
      {
        //do not both push and pull across the same pipe-pipe connection
        if (beGs.Block is BlockGasPipe gasPipe)
        {
          string[] pushFaces;

          if (gasPipe.Attributes["pushGasFaces"].Exists)
          {
            pushFaces = gasPipe.Attributes["pushGasFaces"].AsArray<string>(null);
          }
          else
          {
            pushFaces = new string[GasPushFaces.Length];

            for (int i = 0; i < GasPushFaces.Length; i++)
            {
              pushFaces[i] = GasPushFaces[i].Code;
            }
          }

          if (pushFaces?.Contains(inputFace.Opposite.Code) == true) return;
        }
      }

      base.TryPullFrom(inputFace);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
      InitGasFlowFromType();

      base.FromTreeAttributes(tree, worldForResolving);
    }
  }
}
