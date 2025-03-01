using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using SFK.API;
using Vintagestory.GameContent;

namespace SFK.Steamworks.Boiler
{
  public class BlockMultiblockBoiler : Block, IIgnitable
  {
    public override bool IsReplacableBy(Block block)
    {
      return base.IsReplacableBy(block);
    }

    public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
    {
      IWorldAccessor world = player?.Entity?.World;
      world ??= api.World;

      if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEMultiblockGasFlow be || be.Principal == null) return 1f;  //never break

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

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
      return null;
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
    {
      if (world.BlockAccessor.GetBlockEntity(pos) is not BEMultiblockGasFlow be || be.Principal == null)
      {
        // being broken by other game code (including on breaking the boiler base block): standard block breaking treatment
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
      if (blockAccess.GetBlockEntity(pos) is not BEMultiblockGasFlow be || be.Principal == null)
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

      if (blockAccess.GetBlockEntity(pos) is not BEMultiblockGasFlow be || be.Principal == null)
      {
        return 0;
      }

      Block principalBlock = blockAccess.GetBlock(be.Principal);

      return principalBlock.GetRandomColor(capi, be.Principal, facing);
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMultiblockGasFlow bem)
      {
        BlockEntity be = world.BlockAccessor.GetBlockEntity(bem.Principal);

        if (be is BEBoiler beb)
        {
          return beb.OnBlockInteract(byPlayer);
        }
      }

      return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    #region Ignitable
    public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
    {
      IWorldAccessor world = byEntity?.World;
      world ??= api.World;

      if (world.BlockAccessor.GetBlockEntity(pos) is not BEMultiblockGasFlow be || be.Principal == null) return EnumIgniteState.NotIgnitable;

      if (world.BlockAccessor.GetBlock(be.Principal) is BlockBoiler principalBlock)
      {
        return principalBlock.OnTryIgniteBlock(byEntity, be.Principal, secondsIgniting);
      }

      return EnumIgniteState.NotIgnitable;
    }

    public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
    {
      IWorldAccessor world = byEntity?.World;
      world ??= api.World;

      if (world.BlockAccessor.GetBlockEntity(pos) is not BEMultiblockGasFlow be || be.Principal == null) return;

      if (world.BlockAccessor.GetBlock(be.Principal) is BlockBoiler principalBlock)
      {
        principalBlock.OnTryIgniteBlockOver(byEntity, be.Principal, secondsIgniting, ref handling);
      }
    }

    public EnumIgniteState OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
    {
      IWorldAccessor world = byEntity?.World;
      world ??= api.World;

      if (world.BlockAccessor.GetBlockEntity(pos) is not BEMultiblockGasFlow be || be.Principal == null) return EnumIgniteState.NotIgnitable;

      if (world.BlockAccessor.GetBlock(be.Principal) is BlockBoiler principalBlock)
      {
        return principalBlock.OnTryIgniteBlock(byEntity, be.Principal, secondsIgniting);
      }

      return EnumIgniteState.NotIgnitable;
    }

    #endregion

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
      if (world.BlockAccessor.GetBlockEntity(pos) is not BEMultiblockGasFlow be || be.Principal == null) return null;
      return world.BlockAccessor.GetBlock(be.Principal).OnPickBlock(world, be.Principal);
    }
  }
}
