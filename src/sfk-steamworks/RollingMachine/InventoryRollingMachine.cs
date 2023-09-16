using Vintagestory.API.Common;

namespace SFK.Steamworks.RollingMachine
{
  public class InventoryRollingMachine : InventoryDisplayed
  {
    public InventoryRollingMachine(BlockEntity be, int size) : base(be, size, "rollingmachine-0", null)
    {
      // slot 0: rollers
      // slot 1: work item
      slots = GenEmptySlots(size);
      for (int i = 0; i < size; i++) slots[i].MaxSlotStackSize = 1;
    }
  }
}
