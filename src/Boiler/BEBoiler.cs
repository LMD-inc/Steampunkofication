using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Steampunkofication.src.Boiler
{
  public class BEBoiler : BlockEntityContainer
  {
    public int CapacityLitresWater { get; set; } = 50;
    public int CapacityLitresSteam { get; set; } = 100;
    GuiDialogBoiler invDialog;
    internal BoilerInventory inventory;
    BlockBoiler ownBlock;

    #region Config

    public override string InventoryClassName => "boiler";
    public virtual string DialogTitle => Lang.Get("Boiler");
    public override InventoryBase Inventory => inventory;
    private long tickListenerId;

    #endregion

    public BEBoiler()
    {
      inventory = new BoilerInventory(null, null);
      inventory.SlotModified += OnSlotModified;
    }
    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);
      inventory.LateInitialize("boiler-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);

      ownBlock = Block as BlockBoiler;

      if (ownBlock?.Attributes?["capacityLitresWater"].Exists == true)
      {
        CapacityLitresWater = ownBlock.Attributes["capacityLitresWater"].AsInt(50);
        (inventory[1] as ItemSlotLiquidOnly).CapacityLitres = CapacityLitresWater;
      }

      if (ownBlock?.Attributes?["capacityLitresSteam"].Exists == true)
      {
        CapacityLitresSteam = ownBlock.Attributes["capacityLitresSteam"].AsInt(100);
        (inventory[2] as ItemSlotLiquidOnly).CapacityLitres = CapacityLitresSteam;
      }

      if (api.Side == EnumAppSide.Server)
      {
        tickListenerId = RegisterGameTickListener(ProduceSteam, 1000);
      }
    }

    private void OnSlotModified(int slotId)
    {
      if (Api is ICoreClientAPI)
      {
        if (slotId == 0 || slotId == 1 || slotId == 2)
        {
          invDialog?.Update();
        }
      }
    }

    #region Events

    public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
    {
      if (packetid <= 1000)
      {
        inventory.InvNetworkUtil.HandleClientPacket(fromPlayer, packetid, data);
      }

      if (packetid == (int)EnumBlockEntityPacketId.Close)
      {
        if (fromPlayer.InventoryManager != null)
        {
          fromPlayer.InventoryManager.CloseInventory(Inventory);
        }
      }
    }

    public override void OnReceivedServerPacket(int packetid, byte[] data)
    {
      base.OnReceivedServerPacket(packetid, data);

      if (packetid == (int)EnumBlockEntityPacketId.Close)
      {
        (Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
        invDialog?.TryClose();
        invDialog?.Dispose();
        invDialog = null;
      }
    }

    public void OnBlockInteract(IPlayer byPlayer)
    {
      if (Api.Side == EnumAppSide.Client)
      {
        if (invDialog == null)
        {
          invDialog = new GuiDialogBoiler(DialogTitle, Inventory, Pos, Api as ICoreClientAPI);
          invDialog.OnClosed += () =>
          {
            invDialog = null;
            (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)EnumBlockEntityPacketId.Close, null);
            byPlayer.InventoryManager.CloseInventory(inventory);
          };
        }

        invDialog.TryOpen();

        (Api as ICoreClientAPI).Network.SendPacketClient(inventory.Open(byPlayer));
      }
      else
      {
        byPlayer.InventoryManager.OpenInventory(inventory);
      }
    }

    #endregion

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
      base.FromTreeAttributes(tree, worldForResolving);

      if (Api?.Side == EnumAppSide.Client)
      {
        invDialog?.Update();
      }
    }

    #region Helper getters

    public ItemSlot fuelSlot => inventory[0];
    public ItemSlot waterSlot => inventory[1];
    public ItemSlot steamSlot => inventory[2];
    public ItemStack fuelStack
    {
      get { return inventory[0].Itemstack; }
      set { inventory[0].Itemstack = value; inventory[0].MarkDirty(); }
    }

    public ItemStack waterStack
    {
      get { return inventory[1].Itemstack; }
      set { inventory[1].Itemstack = value; inventory[1].MarkDirty(); }
    }

    public ItemStack steamStack
    {
      get { return inventory[2].Itemstack; }
      set { inventory[2].Itemstack = value; inventory[2].MarkDirty(); }
    }

    #endregion

    private void ProduceSteam(float dt)
    {
      if (Api?.Side == EnumAppSide.Server)
      {
        if (!waterSlot.Empty)
        {
          // TODO: fing how to proper check for emptiness
          if (/*is burning && */ waterStack.StackSize > 0)
          {
            if (steamStack?.StackSize >= CapacityLitresSteam)
            {
              // Boom?
              return;
            }

            waterSlot.TakeOut(1);

            if (steamSlot.Empty)
            {
              steamSlot.Itemstack = new ItemStack(Api.World.GetItem(new AssetLocation("steampunkofication:steamportion")), 1);
            }
            else
            {
              steamStack.StackSize += 1;
            }

            MarkDirty(true);
            Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
          }
        }
      }

      invDialog?.Update();

      return;
    }

    public override void OnBlockRemoved()
    {
      base.OnBlockRemoved();

      invDialog?.TryClose();
      invDialog?.Dispose();
      invDialog = null;

      UnregisterGameTickListener(tickListenerId);
    }
  }
}
