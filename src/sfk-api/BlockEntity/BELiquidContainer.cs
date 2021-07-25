using System.Text;
using Vintagestory.API.Common;

namespace SFK.API
{
  public class BELiquidContainer : BlockEntityLiquidFlow
  {
    public override string InventoryClassName => "liquidcontainer";

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
      dsc.Clear();

      if (!inventory.Empty)
      {
        dsc.AppendLine("Contents:");

        foreach (ItemSlot slot in inventory)
        {
          if (slot.Empty) continue;

          // TODO: localize and pluralize
          dsc.AppendLine($"{slot.Itemstack.StackSize} litres of {slot.Itemstack.GetName()}");
        }
      }
      else
      {
        dsc.AppendLine("Empty");
      }
    }
  }
}