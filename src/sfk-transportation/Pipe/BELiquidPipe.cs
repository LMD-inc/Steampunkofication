using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

using SFK.API;

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

      if (type == null || type == "empty") return;

      BlockFacing[] faces = new BlockFacing[type.Length];

      for (int i = 0; i < type.Length; i++)
      {
        faces[i] = BlockFacing.FromFirstLetter(type[i]);
      }

      LiquidPullFaces = LiquidPushFaces = AcceptLiquidFromFaces = faces;
    }

    public override void TryPullFrom(BlockFacing inputFace)
    {
      BlockPos InputPosition = Pos.AddCopy(inputFace);

      if (Api.World.BlockAccessor.GetBlockEntity(InputPosition) is BlockEntityGasFlow beGs)
      {
        //do not both push and pull across the same pipe-pipe connection
        if (beGs.Block is BlockLiquidPipe liquidPipe)
        {
          string[] pushFaces;

          if (liquidPipe.Attributes["pushLiquidFaces"].Exists)
          {
            pushFaces = liquidPipe.Attributes["pushLiquidFaces"].AsArray<string>(null);
          }
          else
          {
            pushFaces = new string[LiquidPushFaces.Length];

            for (int i = 0; i < LiquidPushFaces.Length; i++)
            {
              pushFaces[i] = LiquidPushFaces[i].Code;
            }
          }

          if (pushFaces?.Contains(inputFace.Opposite.Code) == true) return;
        }
      }

      base.TryPullFrom(inputFace);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
      InitLiquidFlowFromType();

      base.FromTreeAttributes(tree, worldForResolving);
    }
  }
}
