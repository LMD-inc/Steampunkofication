using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace SFK.API
{
  public class BEGasContainer : BlockEntityGasFlow
  {
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