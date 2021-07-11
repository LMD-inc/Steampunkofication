using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SFK.Steamworks.Boiler
{
  public class BlockBoiler : BlockLiquidContainerBase
  {
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

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
      return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
    }
  }
}
