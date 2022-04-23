using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using SFK.API;

namespace SFK.Steamworks.SteamEngine
{
  public class BlockMultiblockSteamEngine : Block
  {
    public override bool IsReplacableBy(Block block)
    {
      return base.IsReplacableBy(block);
    }

    public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
    {
      IWorldAccessor world = player?.Entity?.World;
      if (world == null) world = api.World;
      BEMultiblockGasFlow be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEMultiblockGasFlow;
      if (be == null || be.Principal == null) return 1f;  //never break
      Block principalBlock = world.BlockAccessor.GetBlock(be.Principal);
      BlockSelection bs = blockSel.Clone();
      bs.Position = be.Principal;
      return principalBlock.OnGettingBroken(player, bs, itemslot, remainingResistance, dt, counter);
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
    {
      BlockPos npos = blockPos.AddCopy(BlockFacing.FromCode(Variant["side"]));
      world.BlockAccessor.GetBlock(npos).OnNeighbourBlockChange(world, npos, blockPos);

      base.OnBlockPlaced(world, blockPos, byItemStack);
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
    {
      if (!(world.BlockAccessor.GetBlockEntity(pos) is BEMultiblockGasFlow be) || be == null || be.Principal == null)
      {
        // being broken by other game code (including on breaking the steam engine base block): standard block breaking treatment
        base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        return;
      }
      // being broken by player: break the main block instead
      BlockPos principalPos = be.Principal;
      Block principalBlock = world.BlockAccessor.GetBlock(principalPos);
      principalBlock.OnBlockBroken(world, principalPos, byPlayer, dropQuantityMultiplier);

      // Need to trigger neighbourchange on client side only (because it's normally in the player block breaking code)
      if (api.Side == EnumAppSide.Client)
      {
        foreach (BlockFacing facing in BlockFacing.HORIZONTALS)
        {
          BlockPos npos = principalPos.AddCopy(facing);
          world.BlockAccessor.GetBlock(npos).OnNeighbourBlockChange(world, npos, principalPos);
        }
      }

      base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
    }

    public override Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
    {
      if (!(blockAccess.GetBlockEntity(pos) is BEMultiblockGasFlow be) || be == null || be.Principal == null)
      {
        return base.GetParticleBreakBox(blockAccess, pos, facing);
      }
      // being broken by player: break the main block instead
      Block principalBlock = blockAccess.GetBlock(be.Principal);
      return principalBlock.GetParticleBreakBox(blockAccess, be.Principal, facing);
    }

    public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex)
    {
      IBlockAccessor blockAccess = capi.World.BlockAccessor;
      if (!(blockAccess.GetBlockEntity(pos) is BEMultiblockGasFlow be) || be == null || be.Principal == null)
      {
        return 0;
      }
      Block principalBlock = blockAccess.GetBlock(be.Principal);
      return principalBlock.GetRandomColor(capi, be.Principal, facing, rndIndex);
    }
  }
}
