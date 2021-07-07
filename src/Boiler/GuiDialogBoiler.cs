using System;

using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Steampunkofication.src.Boiler
{
  public class GuiDialogBoiler : GuiDialogBlockEntity
  {
    public GuiDialogBoiler(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
    {
      if (IsDuplicate) return;

      capi.World.Player.InventoryManager.OpenInventory(Inventory);

      SetupDialog();
    }

    private void OnInventorySlotModified(int slotid)
    {
      SetupDialog();
    }

    void SetupDialog()
    {
      // TODO: deisgn further boiler GUI
      ElementBounds boilerBounds = ElementBounds.Fixed(0, 0, 240, 180);

      ElementBounds fuelSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 140, 1, 1);
      ElementBounds waterFullnessMeterBounds = ElementBounds.Fixed(100, 30, 40, 200);
      ElementBounds steamFullnessMeterBounds = ElementBounds.Fixed(160, 30, 40, 200);

      // 2. Around all that is 10 pixel padding
      ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
      bgBounds.BothSizing = ElementSizing.FitToChildren;
      bgBounds.WithChildren(boilerBounds);

      // 3. Finally Dialog
      ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle)
          .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

      SingleComposer = capi.Gui
          .CreateCompo("blockentityboiler" + BlockEntityPosition, dialogBounds)
          .AddShadedDialogBG(bgBounds)
          .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
          .BeginChildElements(bgBounds)
              .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 0 }, fuelSlotBounds, "inputSlot")

              .AddInset(waterFullnessMeterBounds.ForkBoundingParent(2, 2, 2, 2), 2)
              .AddDynamicCustomDraw(waterFullnessMeterBounds, createFullnessMeterDraw(1, 50), "waterBar")

              .AddInset(steamFullnessMeterBounds.ForkBoundingParent(2, 2, 2, 2), 2)
              .AddDynamicCustomDraw(steamFullnessMeterBounds, createFullnessMeterDraw(2, 100), "steamBar")
          .EndChildElements()
          .Compose()
      ;
    }

    public void Update()
    {
      SingleComposer.GetCustomDraw("waterBar").Redraw();
      SingleComposer.GetCustomDraw("steamBar").Redraw();
    }

    private DrawDelegateWithBounds createFullnessMeterDraw(int slotId, float capacity)
    {
      return (ctx, surface, currentBounds) =>
      {
        ItemSlot liquidSlot = Inventory[slotId];
        if (liquidSlot.Empty) return;

        BEBoiler beboiler = capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) as BEBoiler;
        float itemsPerLitre = 1f;

        WaterTightContainableProps props = BlockLiquidContainerBase.GetInContainerProps(liquidSlot.Itemstack);
        if (props != null)
        {
          itemsPerLitre = props.ItemsPerLitre;
          capacity = Math.Max(capacity, props.MaxStackSize);
        }

        float fullnessRelative = liquidSlot.StackSize / itemsPerLitre / capacity;

        double offY = (1 - fullnessRelative) * currentBounds.InnerHeight;

        ctx.Rectangle(0, offY, currentBounds.InnerWidth, currentBounds.InnerHeight - offY);

        CompositeTexture tex = liquidSlot.Itemstack.Collectible.Attributes?["waterTightContainerProps"]?["texture"]?.AsObject<CompositeTexture>(null, liquidSlot.Itemstack.Collectible.Code.Domain);
        if (tex != null)
        {
          ctx.Save();
          Matrix m = ctx.Matrix;
          m.Scale(GuiElement.scaled(3), GuiElement.scaled(3));
          ctx.Matrix = m;

          AssetLocation loc = tex.Base.Clone().WithPathAppendixOnce(".png");
          GuiElement.fillWithPattern(capi, ctx, loc.Path, true, false);
          ctx.Restore();
        }
      };
    }

    private void SendInvPacket(object p)
    {
      capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
    }

    private void OnTitleBarClose()
    {
      TryClose();
    }

    public override void OnGuiOpened()
    {
      base.OnGuiOpened();
      Inventory.SlotModified += OnInventorySlotModified;
    }

    public override void OnGuiClosed()
    {
      Inventory.SlotModified -= OnInventorySlotModified;

      SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(capi);

      base.OnGuiClosed();
    }

    public override bool OnEscapePressed()
    {
      base.OnEscapePressed();
      OnTitleBarClose();
      return TryClose();
    }
  }
}