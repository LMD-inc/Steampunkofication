using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace SFK.Transportation.Belt
{
  public class BlockBelt : BlockMPBase
  {
    public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {

    }

    public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {
      BlockFacing[] beltFaces = Variant["orient"].ToList().ConvertAll(letter => BlockFacing.FromFirstLetter(letter)).ToArray();

      if (beltFaces.Any(f => f.Opposite == face))
      {
        return true;
      }

      return false;
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack)
    {
      foreach (BlockFacing face in BlockFacing.HORIZONTALS)
      {
        BlockPos npos = blockPos.AddCopy(face);
        world.BlockAccessor.GetBlock(npos).OnNeighbourBlockChange(world, blockPos, blockPos);
      }
     
      base.OnBlockPlaced(world, blockPos, byItemStack);
    }

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

    #region Static directional variants
    static string[] OneDir = new string[] { "n", "e", "s", "w" };
    static string[] TwoDir = new string[] { "ns", "ew" };

    public static Dictionary<string, KeyValuePair<string[], int>> AngleGroups = new Dictionary<string, KeyValuePair<string[], int>>();

    static BlockBelt()
    {
      AngleGroups["n"] = new KeyValuePair<string[], int>(OneDir, 0);
      AngleGroups["e"] = new KeyValuePair<string[], int>(OneDir, 1);
      AngleGroups["s"] = new KeyValuePair<string[], int>(OneDir, 2);
      AngleGroups["w"] = new KeyValuePair<string[], int>(OneDir, 3);

      AngleGroups["ns"] = new KeyValuePair<string[], int>(TwoDir, 0);
      AngleGroups["ew"] = new KeyValuePair<string[], int>(TwoDir, 1);
    }

    public override AssetLocation GetRotatedBlockCode(int angle)
    {
      string type = Variant["orient"];

      if (type == "empty" || type == "nesw") return Code;


      int angleIndex = angle / 90;

      var val = AngleGroups[type];

      string newFacing = val.Key[GameMath.Mod(val.Value + angleIndex, val.Key.Length)];

      return CodeWithVariant("type", newFacing);
    }

    public static int GetRotatedBlockAngleFromCode(string code)
    {
      if (code == "empty") return 0;

      var val = AngleGroups[code];

      return 90 * val.Value;
    }
    #endregion
  }
}
