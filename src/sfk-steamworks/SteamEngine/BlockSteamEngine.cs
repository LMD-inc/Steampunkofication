using System.Collections.Generic;
using SFK.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace SFK.Steamworks.SteamEngine
{
  public class BlockSteamEngine : BlockMPBase
  {

    #region Multiblock
    BlockFacing orientation;

    public BlockFacing Orientation => orientation;

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    {

      if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
      {
        return false;
      }

      orientation = BlockFacing.FromCode(api.World.BlockAccessor.GetBlock(blockSel.Position).LastCodePart());

      foreach (BlockFacing face in BlockFacing.HORIZONTALS)
      {
        BlockPos pos = blockSel.Position.AddCopy(face);
        IMechanicalPowerBlock block = world.BlockAccessor.GetBlock(pos) as IMechanicalPowerBlock;
        if (block != null)
        {
          if (block.HasMechPowerConnectorAt(world, pos, face.Opposite))
          {
            //Prevent rotor back-to-back placement
            // if (block is IMPPowered) return false;
            // IMPPowered is internal class, investigate how to make same check.

            Block toPlaceBlock = world.GetBlock(new AssetLocation(FirstCodePart() + "-" + face.Opposite.Code));
            world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, blockSel.Position);

            block.DidConnectAt(world, pos, face.Opposite);
            WasPlaced(world, blockSel.Position, face);

            return true;
          }
        }
      }

      bool handled = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);

      if (handled)
      {
        PlaceFakeBlock(world, blockSel.Position);
        return true;
      }

      return false;
    }

    private void PlaceFakeBlock(IWorldAccessor world, BlockPos pos)
    {
      orientation = BlockFacing.FromCode(api.World.BlockAccessor.GetBlock(pos).LastCodePart());
      Block toPlaceBlock = world.GetBlock(new AssetLocation($"sfk-steamworks:steamengine-mp-{orientation}"));

      world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, pos.AddCopy(orientation));
    }

    #endregion

    #region Mech power

    public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {

    }
    public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {
      return face == BlockFacing.FromCode(LastCodePart()).GetCCW();
    }

    #endregion

    #region Events

    public override void OnLoaded(ICoreAPI api)
    {
      base.OnLoaded(api);
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
      BlockFacing baseBlockFacing = BlockFacing.FromCode(api.World.BlockAccessor.GetBlock(pos).LastCodePart());
      Block mpBlock = api.World.BlockAccessor.GetBlock(pos.AddCopy(baseBlockFacing));

      if (mpBlock.Code.Path == $"steamengine-mp-{baseBlockFacing}")
      {
        world.BlockAccessor.SetBlock(0, pos.AddCopy(baseBlockFacing));
      }

      base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
    }

    #endregion
  }
}
