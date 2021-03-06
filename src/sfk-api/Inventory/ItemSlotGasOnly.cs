using System;
using Vintagestory.API.Common;

namespace SFK.API
{
  public class ItemSlotGasOnly : ItemSlot
  {
    public int CapacityLitres;

    public override int GetRemainingSlotSpace(ItemStack forItemstack)
    {
      // Use waterTightProps from forItemstack in future when gas have not 1:1 item-litres.
      return Math.Max(0, CapacityLitres - StackSize);
    }

    public ItemSlotGasOnly(InventoryBase inventory, int capacityLitres) : base(inventory)
    {
      this.CapacityLitres = capacityLitres;
    }

    public override bool CanHold(ItemSlot itemstackFromSourceSlot)
    {
      return itemstackFromSourceSlot.Itemstack.Collectible?.MatterState == EnumMatterState.Gas;
    }

    public override bool CanTake()
    {
      return true;
    }

    public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
    {
      if (inventory?.PutLocked == true) return false;

      ItemStack sourceStack = sourceSlot.Itemstack;
      if (sourceStack == null) return false;

      return sourceStack.Collectible?.MatterState == EnumMatterState.Gas
        && (itemstack == null || itemstack.Collectible.GetMergableQuantity(itemstack, sourceStack, priority) > 0)
        && GetRemainingSlotSpace(itemstack) > 0;
    }

    public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
    {
      if (Empty) return;
      if (sourceSlot.CanHold(this))
      {
        if (sourceSlot.Itemstack != null && sourceSlot.Itemstack != null && sourceSlot.Itemstack.Collectible.GetMergableQuantity(sourceSlot.Itemstack, itemstack, op.CurrentPriority) < itemstack.StackSize) return;

        op.RequestedQuantity = StackSize;

        TryPutInto(sourceSlot, ref op);

        if (op.MovedQuantity > 0)
        {
          OnItemSlotModified(itemstack);
        }
      }
    }
  }
}
