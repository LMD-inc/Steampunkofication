using System;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace SFK.Transportation.WaterPump
{
  public class BlockWaterPump : BlockMPBase
  {
    public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {

    }

    public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {
      BlockFacing orient = BlockFacing.FromCode(Variant["side"]);
      return face == orient || face == orient.Opposite;
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    {
      if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
      {
        return false;
      }

      // Checking player's look at side first
      BlockFacing orient = SuggestedHVOrientation(byPlayer, blockSel)[0];
      BlockFacing[] faces = BlockFacing.HORIZONTALS;
      Array.Sort(faces, (a, b) => a == orient ? 1 : -1);

      foreach (BlockFacing face in faces)
      {
        if (CheckHasWater(world, blockSel.Position, face))
        {
          AssetLocation loc = new AssetLocation($"sfktransportation:{FirstCodePart()}-{face.Code}");
          Block toPlaceBlock = world.GetBlock(loc);

          if (toPlaceBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack))
          {
            WasPlaced(world, blockSel.Position, null);
            return true;
          }
          return false;
        }

      }

      failureCode = "requirefullwater";
      return false;
    }

    public bool CheckHasWater(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {
      BlockPos posToCheck = pos.AddCopy(face.GetCCW()).AddCopy(BlockFacing.DOWN);
      Block block = world.BlockAccessor.GetBlock(posToCheck) as Block;

      if (block == null) return false;
      if (block.IsLiquid() && block.LiquidLevel == 7 && block.LiquidCode.Contains("water")) return true;
      return false;
    }
  }
}
