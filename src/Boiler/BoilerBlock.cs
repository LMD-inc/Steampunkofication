using Vintagestory.API.Common;

using Steampunkofication.API.Steam;

namespace Steampunkofication.src.Boiler
{
  class BoilerBlock : Block
  {
    //Toggle power if player is holding a screwdriver or club
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {

      var bes = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BESteam;
      if (bes == null
        || byPlayer.Entity.RightHandItemSlot.Itemstack == null
        || byPlayer.Entity.RightHandItemSlot.Itemstack.Item == null)
      {
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
      }
      string fcp = byPlayer.Entity.RightHandItemSlot.Itemstack.Item.CodeWithoutParts(1);
      if ((fcp.Contains("wrench") && !fcp.Contains("head")) || fcp.Contains("woodenclub"))
      {
        bes.TogglePower();

        return true;
      }
      return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
  }
}