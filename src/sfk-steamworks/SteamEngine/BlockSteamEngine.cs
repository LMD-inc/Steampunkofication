using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace SFK.Steamworks.SteamEngine
{
  public class BlockSteamEngine : BlockMPBase
  {

    #region Multiblock

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    {
      if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
      {
        return false;
      }

      // Can place second block
      BlockFacing[] horVer = SuggestedHVOrientation(byPlayer, blockSel);
      BlockPos secondPos = blockSel.Position.AddCopy(horVer[0]);
      BlockSelection secondBlockSel = new BlockSelection() { Position = secondPos, Face = BlockFacing.UP };

      if (!CanPlaceBlock(world, byPlayer, secondBlockSel, ref failureCode)) return false;

      string code = horVer[0].Opposite.Code;

      bool handled = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);

      if (handled)
      {
        PlaceFakeBlock(world, secondPos, horVer[0]);
        WasPlaced(world, blockSel.Position, null);
        return true;
      }

      return false;
    }

    private void PlaceFakeBlock(IWorldAccessor world, BlockPos pos, BlockFacing orientation)
    {
      Block toPlaceBlock = world.GetBlock(new AssetLocation($"sfksteamworks:steamengine-mp-{orientation}"));

      world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, pos);
    }

    #endregion

    #region Mech power

    public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {

    }
    public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {
      BlockFacing orient = BlockFacing.FromCode(LastCodePart()).GetCCW();

      return face == orient || face == orient.Opposite;
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

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
      StringBuilder sb = new StringBuilder(base.GetPlacedBlockInfo(world, pos, forPlayer));

      world.BlockAccessor.GetBlockEntity(pos)?.GetBlockInfo(forPlayer, sb);

      return sb.ToString();
    }

    #endregion
  }
}
