using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Steampunkofication.src.Boiler
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

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      BEBoiler beboiler = null;
      if (blockSel.Position != null)
      {
        beboiler = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBoiler;
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
      if (Attributes?["capacityLitresWater"].Exists == true)
      {
        /* This prop needed for world interactions e.g. put water from bucket into boiler.
         * BlockLiquidContainerBase uses capacityLitres by default, so overriding.
         */
        capacityLitresFromAttributes = Attributes["capacityLitresWater"].AsInt(50);
      }

      if (api.Side != EnumAppSide.Client) return;
    }
  }
}