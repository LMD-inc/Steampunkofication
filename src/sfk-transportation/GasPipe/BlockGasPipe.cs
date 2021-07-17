using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace SFK.Transportation.GasPipe
{
  public class BlockGasPipe : Block
  {
    #region Connecting variants

    public string GetOrientations(IWorldAccessor world, BlockPos pos)
    {
      string orientations =
          GetPipeCode(world, pos, BlockFacing.NORTH) +
          GetPipeCode(world, pos, BlockFacing.EAST) +
          GetPipeCode(world, pos, BlockFacing.SOUTH) +
          GetPipeCode(world, pos, BlockFacing.WEST) +
          GetPipeCode(world, pos, BlockFacing.UP) +
          GetPipeCode(world, pos, BlockFacing.DOWN)
      ;

      if (orientations.Length == 0) orientations = "empty";
      return orientations;
    }

    private string GetPipeCode(IWorldAccessor world, BlockPos pos, BlockFacing facing)
    {
      if (ShouldConnectAt(world, pos, facing)) return "" + facing.Code[0];
      return "";
    }

    public bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
    {
      Block block = world.BlockAccessor.GetBlock(ownPos.AddCopy(side));

      bool attrExists = block.Attributes?["pipeConnect"][side.Code].Exists == true;

      if (attrExists)
      {
        return block.Attributes["pipeConnect"][side.Code].AsBool(true);
      }

      return
          block is BlockGasPipe ||
          block.EntityClass == "GasFlow" ||
          block.EntityClass == "MultiblockGasFlow"
      ;
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

    #region Handbook and picking

    public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
    {
      return new BlockDropItemStack[] { new BlockDropItemStack(handbookStack) };
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
    {
      Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[] { "type" }, new string[] { "ew" }));
      return new ItemStack[] { new ItemStack(block) };
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
      Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[] { "type" }, new string[] { "ew" }));
      return new ItemStack(block);
    }

    #endregion

    #region Static directional variants

    static string[] OneDir = new string[] { "n", "e", "s", "w" };
    static string[] TwoDir = new string[] { "ns", "ew", "ud" };
    static string[] AngledDir = new string[] { "ne", "nw", "nu", "nd", "es", "eu", "ed", "sw", "su", "sd", "wu", "wd" };
    static string[] ThreeDir = new string[] { "nes", "new", "neu", "ned", "nsw", "nsu", "nsd", "nwu", "nwd", "nud", "esw", "esu", "esd", "ewu", "ewd", "eud", "swu", "swd", "sud", "wud", };

    public static Dictionary<string, KeyValuePair<string[], int>> AngleGroups = new Dictionary<string, KeyValuePair<string[], int>>();

    static BlockGasPipe()
    {
      AngleGroups["n"] = new KeyValuePair<string[], int>(OneDir, 0);
      AngleGroups["e"] = new KeyValuePair<string[], int>(OneDir, 1);
      AngleGroups["s"] = new KeyValuePair<string[], int>(OneDir, 2);
      AngleGroups["w"] = new KeyValuePair<string[], int>(OneDir, 3);

      AngleGroups["ns"] = new KeyValuePair<string[], int>(TwoDir, 0);
      AngleGroups["ew"] = new KeyValuePair<string[], int>(TwoDir, 1);
      AngleGroups["ud"] = new KeyValuePair<string[], int>(TwoDir, 2);

      AngleGroups["ne"] = new KeyValuePair<string[], int>(AngledDir, 0);
      AngleGroups["nw"] = new KeyValuePair<string[], int>(AngledDir, 1);
      AngleGroups["nu"] = new KeyValuePair<string[], int>(AngledDir, 2);
      AngleGroups["nd"] = new KeyValuePair<string[], int>(AngledDir, 3);
      AngleGroups["es"] = new KeyValuePair<string[], int>(AngledDir, 4);
      AngleGroups["eu"] = new KeyValuePair<string[], int>(AngledDir, 5);
      AngleGroups["ed"] = new KeyValuePair<string[], int>(AngledDir, 6);
      AngleGroups["sw"] = new KeyValuePair<string[], int>(AngledDir, 7);
      AngleGroups["su"] = new KeyValuePair<string[], int>(AngledDir, 8);
      AngleGroups["sd"] = new KeyValuePair<string[], int>(AngledDir, 9);
      AngleGroups["wu"] = new KeyValuePair<string[], int>(AngledDir, 10);
      AngleGroups["wd"] = new KeyValuePair<string[], int>(AngledDir, 11);

      AngleGroups["nes"] = new KeyValuePair<string[], int>(ThreeDir, 0);
      AngleGroups["new"] = new KeyValuePair<string[], int>(ThreeDir, 1);
      AngleGroups["nsw"] = new KeyValuePair<string[], int>(ThreeDir, 4);
      AngleGroups["esw"] = new KeyValuePair<string[], int>(ThreeDir, 10);
    }

    public override AssetLocation GetRotatedBlockCode(int angle)
    {
      string type = Variant["type"];

      if (type == "empty" || type == "nesw") return Code;


      int angleIndex = angle / 90;

      var val = AngleGroups[type];

      string newFacing = val.Key[GameMath.Mod(val.Value + angleIndex, val.Key.Length)];

      return CodeWithVariant("type", newFacing);
    }

    #endregion
  }
}
