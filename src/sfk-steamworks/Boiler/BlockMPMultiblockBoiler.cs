using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;
using SFK.API;

namespace SFK.Steamworks.Boiler
{
  public class BlockMPMultiblockBoiler : Block
  {
    public override bool IsReplacableBy(Block block)
    {
      return base.IsReplacableBy(block);
    }

    public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
    {
      IWorldAccessor world = player?.Entity?.World;
      if (world == null) world = api.World;
      BEMPMultiblockGasFlow be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEMPMultiblockGasFlow;
      if (be == null || be.Principal == null) return 1f;  //never break
      Block principalBlock = world.BlockAccessor.GetBlock(be.Principal);
      BlockSelection bs = blockSel.Clone();
      bs.Position = be.Principal;
      return principalBlock.OnGettingBroken(player, bs, itemslot, remainingResistance, dt, counter);
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
    {
      base.OnBlockPlaced(world, blockPos, byItemStack);
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
    {
      BEMPMultiblockGasFlow be = world.BlockAccessor.GetBlockEntity(pos) as BEMPMultiblockGasFlow;
      if (be == null || be.Principal == null)
      {
        // being broken by other game code (including on breaking the pulverizer base block): standard block breaking treatment
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
      BEMPMultiblockGasFlow be = blockAccess.GetBlockEntity(pos) as BEMPMultiblockGasFlow;
      if (be == null || be.Principal == null)
      {
        return base.GetParticleBreakBox(blockAccess, pos, facing);
      }
      // being broken by player: break the main block instead
      Block principalBlock = blockAccess.GetBlock(be.Principal);
      return principalBlock.GetParticleBreakBox(blockAccess, be.Principal, facing);
    }

    public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing)
    {
      IBlockAccessor blockAccess = capi.World.BlockAccessor;
      BEMPMultiblockGasFlow be = blockAccess.GetBlockEntity(pos) as BEMPMultiblockGasFlow;
      if (be == null || be.Principal == null)
      {
        return 0;
      }
      Block principalBlock = blockAccess.GetBlock(be.Principal);
      return principalBlock.GetRandomColor(capi, be.Principal, facing);
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      BEMPMultiblockGasFlow bem = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEMPMultiblockGasFlow;
      if (bem != null)
      {
        BlockEntity be = world.BlockAccessor.GetBlockEntity(bem.Principal);
        if (be is BEBoiler beb)
          return beb.OnBlockInteract(byPlayer);
      }
      return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
  }
}
