
using SFK.API;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SFK.Steamworks.Boiler
{
  class BoilerInventory : InventoryBase, ISlotProvider
  {
    ItemSlot[] slots;
    public ItemSlot[] Slots => slots;
    public override int Count => slots.Length;

    public BoilerInventory(string inventoryID, ICoreAPI api) : base(inventoryID, api)
    {
      // Slot 0: Fuel slot
      // Slot 1: Water slot
      // Slot 2: Steam slot
      slots = GenEmptySlots(3);
    }

    public override ItemSlot this[int slotId]
    {
      get
      {
        if (slotId < 0 || slotId >= Count) return null;
        return slots[slotId];
      }
      set
      {
        if (slotId < 0 || slotId >= Count) throw new ArgumentOutOfRangeException(nameof(slotId));
        if (value == null) throw new ArgumentNullException(nameof(value));
        slots[slotId] = value;
      }
    }

    public override void FromTreeAttributes(ITreeAttribute tree)
    {
      slots = SlotsFromTreeAttributes(tree, slots);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
      SlotsToTreeAttributes(slots, tree);
    }

    protected override ItemSlot NewSlot(int i)
    {
      if (i == 0) return new ItemSlotSurvival(this); // Fuel
      if (i == 1) return new ItemSlotLiquidOnly(this, 50); // Water
      return new ItemSlotGasOnly(this, 100); // Steam
    }

    public GetAutoPushIntoSlotDelegate OnGetAutoPushIntoSlot;
    public GetAutoPullFromSlotDelegate OnGetAutoPullFromSlot;

    public override ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
    {
      if (OnGetAutoPullFromSlot != null)
      {
        return OnGetAutoPullFromSlot(atBlockFace);
      }

      return base.GetAutoPullFromSlot(atBlockFace);
    }

    public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
    {
      if (OnGetAutoPushIntoSlot != null)
      {
        return OnGetAutoPushIntoSlot(atBlockFace, fromSlot);
      }

      return base.GetAutoPushIntoSlot(atBlockFace, fromSlot);
    }
  }
}
