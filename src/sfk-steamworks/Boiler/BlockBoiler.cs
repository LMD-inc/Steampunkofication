using System.Text;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using SFK.API;

namespace SFK.Steamworks.Boiler
{
  public class BlockBoiler : BlockLiquidContainerBase
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
        return true;
      }

      return false;
    }

    private void PlaceFakeBlock(IWorldAccessor world, BlockPos pos, BlockFacing orientation)
    {
      Block toPlaceBlock = world.GetBlock(new AssetLocation($"sfksteamworks:boiler-mp-{orientation}"));

      world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, pos);
    }

    #endregion

    #region Liquid Container
    /*  Returning id of water slot, since this used only for player interaction,
     *  inheriting from BlockLiquidContainerBase.
     *
     * Cool if this method allows to put different types of liquids from bucket to proper slots.
     * Also, maybe it can be used in liquid transportation stuff (pipes)?
     */
    public override int GetContainerSlotId(IWorldAccessor world, BlockPos pos)
    {
      return 1;
    }

    public override int GetContainerSlotId(IWorldAccessor world, ItemStack containerStack)
    {
      return 1;
    }

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
      StringBuilder stb = new StringBuilder();

      BEBoiler beboiler = world.BlockAccessor.GetBlockEntity(pos) as BEBoiler;

      if (beboiler == null) return "n/a be";

      if (beboiler.inputSlot.Empty && beboiler.outputSlot.Empty) return "Empty";

      stb.AppendLine(Lang.Get("Contents:"));

      foreach (ItemSlot slot in beboiler.Inventory)
      {
        if (!slot.Empty && (slot is ItemSlotLiquidOnly || slot is ItemSlotGasOnly))
        {
          // TODO workaround GetCurrentLitres when use not 1:1 litres-item ratio liquids
          stb.AppendLine(Lang.Get("{0} litres of {1}", slot.Itemstack.StackSize, GetIncontentString(slot.Itemstack)));
        }
      }

      return stb.ToString();
    }

    private string GetIncontentString(ItemStack stack)
    {
      return Lang.Get(stack.Collectible.Code.Domain + ":incontainer-" + stack.Class.ToString().ToLowerInvariant() + "-" + stack.Collectible.Code.Path);
    }

    #endregion

    #region Ignitable
    public override EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
    {
      BEBoiler beb = api.World.BlockAccessor.GetBlockEntity(pos) as BEBoiler;
      if (beb != null && beb.fuelSlot.Empty) return EnumIgniteState.NotIgnitablePreventDefault;
      if (beb != null && beb.IsBurning) return EnumIgniteState.NotIgnitablePreventDefault;

      return secondsIgniting > 3 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
    }

    public override void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
    {
      BEBoiler beb = api.World.BlockAccessor.GetBlockEntity(pos) as BEBoiler;
      if (beb != null && !beb.canIgniteFuel)
      {
        beb.canIgniteFuel = true;
        beb.extinguishedTotalHours = api.World.Calendar.TotalHours;
      }

      handling = EnumHandling.PreventDefault;
    }

    #endregion

    #region Events

    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {

    }

    public override int TryPutContent(IWorldAccessor world, ItemStack containerStack, ItemStack contentStack, int desiredItems)
    {
      return 0;
    }

    public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
    {
      base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;

      BEBoiler beboiler = null;

      if (blockSel.Position != null)
      {
        beboiler = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBoiler;
      }

      // TODO: fix interaction with fuel not working. Seems like conflicts with BlockLiquidContainerBase
      if (beboiler != null && stack != null && byPlayer.Entity.Controls.Sneak)
      {
        if (stack.Collectible.CombustibleProps?.BurnTemperature > 0)
        {
          ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Right, 0, EnumMergePriority.DirectMerge, 1);
          byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(beboiler.fuelSlot, ref op);
          if (op.MovedQuantity > 0)
          {
            (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            return true;
          }
        }
      }

      if (beboiler != null && stack?.Block != null && stack.Block.HasBehavior<BlockBehaviorCanIgnite>())
      {
        return false;
      }

      bool handled = base.OnBlockInteractStart(world, byPlayer, blockSel);

      if (!handled && !byPlayer.WorldData.EntityControls.Sneak && blockSel.Position != null)
      {
        if (beboiler != null)
        {
          beboiler.OnBlockInteract(byPlayer);
        }

        return true;
      }

      return handled;
    }

    public override void OnLoaded(ICoreAPI api)
    {
      base.OnLoaded(api);

      if (Attributes?["capacityLitresInput"].Exists == true)
      {
        /* This prop needed for world interactions e.g. put water from bucket into boiler.
         * BlockLiquidContainerBase uses capacityLitres by default, so overriding.
         */
        capacityLitresFromAttributes = Attributes["capacityLitresInput"].AsInt(50);
      }

      // World interaction help
      if (api.Side != EnumAppSide.Client) return;

      interactions = ObjectCacheUtil.GetOrCreate(api, "boilerInteractions", () =>
              {
                List<ItemStack> canIgniteStacks = new List<ItemStack>();

                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                  string firstCodePart = obj.FirstCodePart();

                  if (obj is Block && (obj as Block).HasBehavior<BlockBehaviorCanIgnite>() || obj is ItemFirestarter)
                  {
                    List<ItemStack> stacks = obj.GetHandBookStacks(api as ICoreClientAPI);
                    if (stacks != null) canIgniteStacks.AddRange(stacks);
                  }
                }

                return new WorldInteraction[]
                {
                      new WorldInteraction()
                      {
                          ActionLangCode = "blockhelp-boiler-ignite",
                          MouseButton = EnumMouseButton.Right,
                          HotKeyCode = "sneak",
                          Itemstacks = canIgniteStacks.ToArray(),
                          GetMatchingStacks = (wi, bs, es) => {
                              BEBoiler beb = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BEBoiler;
                              if (beb?.fuelSlot != null && !beb.fuelSlot.Empty && !beb.IsBurning)
                              {
                                  return wi.Itemstacks;
                              }
                              return null;
                          }
                      },
                      new WorldInteraction()
                      {
                          ActionLangCode = "blockhelp-boiler-refuel",
                          MouseButton = EnumMouseButton.Right,
                          HotKeyCode = "sneak"
                      }
                    };
              });
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
      // Multiblock broke
      BlockFacing baseBlockFacing = BlockFacing.FromCode(api.World.BlockAccessor.GetBlock(pos).LastCodePart());
      Block mpBlock = api.World.BlockAccessor.GetBlock(pos.AddCopy(baseBlockFacing));

      if (mpBlock.Code.Path == $"boiler-mp-{baseBlockFacing}")
      {
        world.BlockAccessor.SetBlock(0, pos.AddCopy(baseBlockFacing));
      }

      // Override to drop the barrel empty and drop its contents instead
      if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
      {
        ItemStack[] drops = new ItemStack[] { new ItemStack(this) };

        for (int i = 0; i < drops.Length; i++)
        {
          world.SpawnItemEntity(drops[i], new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
        }

        world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, byPlayer);
      }

      if (EntityClass != null)
      {
        BlockEntity entity = world.BlockAccessor.GetBlockEntity(pos);
        if (entity != null)
        {
          entity.OnBlockBroken();
        }
      }

      world.BlockAccessor.SetBlock(0, pos);
    }

    #endregion

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
      return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
    }
  }
}
