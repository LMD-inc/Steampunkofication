using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent.Mechanics;

namespace SFK.Transportation.Belt
{
  public enum EnumPinPart
  {
    Start,
    End
  }

  // Convention:
  // Right click + Sneak = Attach
  // Right click = Detach
  public class ItemBelt : Item
  {
    // sneak = attach
    // non-sneak = detach
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
      handling = EnumHandHandling.PreventDefault;
      Block targetBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);

      IServerPlayer srvpl = api.World.PlayerByUid((byEntity as EntityPlayer)?.PlayerUID) as IServerPlayer;

      if(srvpl == null) return;

      // Detach
      if (!byEntity.Controls.Sneak)
      {
        BindStartPoint(slot, null);
        return;
      }

      if (!(targetBlock is BlockAxle)) 
      {
        srvpl.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("Belts can be placed only on axles"), EnumChatType.Notification);
        return;
      }

      BlockPos startPos = GetStartPoint(slot);
      BlockPos selPos = blockSel.Position;
      if (startPos != null) 
      {
        if (!startPos.Equals(selPos)) 
        {
          TryPlaceBelts(slot, startPos, selPos, srvpl);
          return;
        }
        else
        {
          srvpl.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("Belt can't be bound to itself"), EnumChatType.Notification);
          return;
        }
      }
      else
      {
        srvpl.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get($"Start position is set to: {selPos.X} {selPos.Y} {selPos.Z}"), EnumChatType.Notification);
        BindStartPoint(slot, blockSel.Position);
        return;
      }
    }

    #region Binding Points
    private void BindStartPoint(ItemSlot slot, BlockPos pos) {
      AssetLocation boundLoc = CodeWithVariant("binding", pos != null ? "bound" : "unbound");
      Item BoundVariant = api.World.GetItem(boundLoc);

      int q = slot.Itemstack.StackSize;
      slot.Itemstack = new ItemStack(BoundVariant, q);
      
      if(pos != null)
      {
        slot.Itemstack?.Attributes?.SetBlockPos(EnumPinPart.Start.ToString(), pos);
      }
      else
      {
        slot.Itemstack?.Attributes.RemoveAttribute(EnumPinPart.Start.ToString());
      }

      slot.MarkDirty();
    }

    private BlockPos GetStartPoint(ItemSlot slot) {
      return slot.Itemstack?.Attributes?.GetBlockPos(EnumPinPart.Start.ToString());
    }

    private bool IsValidPositions(BlockPos start, BlockPos end) {
      // must be on same height
      if(start.Y != end.Y) return false;

      string startAxleDir = api.World.BlockAccessor.GetBlock(start).LastCodePart();
      string endAxleDir = api.World.BlockAccessor.GetBlock(start).LastCodePart();
      // belt gotta go in ns direction
      if (start.X == end.X) {
        if (startAxleDir == "we" && endAxleDir == "we") return true;
        return false;
      }
      // belt gotta go in we direction
      if (start.Z == end.Z) {
        if (startAxleDir == "ns" && endAxleDir == "ns") return true;
        return false;
      }

      // points not on same line
      return false;
    }

    private string GetBeltOrientation(BlockPos start, BlockPos end, BlockPos current) {
      // gotta go ns direction
      if (start.X == end.X) {
        if (current.Equals(start) || current.Equals(end)) {
          if (current.Z < start.Z || current.Z < end.Z) return "s";
          return "n";
        }
        return "ns";
      }

      // gotta go ew direction
      if (start.Z == end.Z) {
        if (current.Equals(start) || current.Equals(end)) {
          if (current.X < start.X || current.X < end.X) return "e";
          return "w";
        }
        return "ew";
      }
      return "error";
    }
    #endregion

    private bool TryPlaceBelts(ItemSlot slot, BlockPos start, BlockPos end, IServerPlayer byPlayer) {
      bool allBlocksValid = true;

      if (!IsValidPositions(start, end)) return false;

      int amountToPlace = Math.Max(Math.Abs(start.X - end.X), Math.Abs(start.Z - end.Z)) + 1;
      if (amountToPlace > slot.StackSize)
      {
        byPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("Not enough belts"), EnumChatType.Notification);
        return false;
      }

      api.World.BulkBlockAccessor.SearchBlocks(start, end, (block, pos) => {
        // can place belts only of between start and end axle air or another Horizontal axles
        if (block.Id == 0 || block is BlockAxle) return true;
        
        return allBlocksValid = false;
      });

      if (!allBlocksValid)
      {
        byPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("Something is on the way"), EnumChatType.Notification);
        return false;
      }

      api.World.BlockAccessor.WalkBlocks(start, end, (block, pos) => {
        string orient = GetBeltOrientation(start, end, pos);
        string blockCode = "belt-normal";
        // replace with axled belt
        if (block is BlockAxle) {
          blockCode = "belt-withaxle";
        }

        Block blockToPlace = api.World.GetBlock(new AssetLocation($"sfktransportation:{blockCode}-{orient}"));
        api.World.BlockAccessor.SetBlock(blockToPlace.Id, pos);
      });

      slot.TakeOut(amountToPlace);

      if (slot.StackSize > 0)
      {
        BindStartPoint(slot, null);
      }

      return true;
    }
  }
}
