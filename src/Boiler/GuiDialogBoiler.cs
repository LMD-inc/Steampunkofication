using System;

using Cairo;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Steampunkofication.src.Boiler
{
  public class GuiDialogBoiler : GuiDialogBlockEntity
  {
    long lastRedrawMs;

    public GuiDialogBoiler(string dialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition,
                           SyncedTreeAttribute tree, ICoreClientAPI capi)
        : base(dialogTitle, Inventory, BlockEntityPosition, capi)
    {
      if (IsDuplicate) return;

      tree.OnModified.Add(new TreeModifiedListener() { listener = OnAttributesModified });
      Attributes = tree;

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
      ElementBounds boilerBounds = ElementBounds.Fixed(0, 0, 240, 200);

      ElementBounds fuelSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 140, 1, 1);
      ElementBounds waterFullnessMeterBounds = ElementBounds.Fixed(120, 30, 40, 200);
      ElementBounds steamFullnessMeterBounds = ElementBounds.Fixed(180, 30, 40, 200);

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
              .AddDynamicCustomDraw(boilerBounds, OnBgDraw, "symbolDrawer")
              .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 0 }, fuelSlotBounds, "fuelSlot")
              .AddDynamicText("", CairoFont.WhiteDetailText(), EnumTextOrientation.Left, fuelSlotBounds.RightCopy(5, 16).WithFixedSize(60, 30), "fueltemp")

              .AddInset(waterFullnessMeterBounds.ForkBoundingParent(2, 2, 2, 2), 2)
              .AddDynamicCustomDraw(waterFullnessMeterBounds, createFullnessMeterDraw(1, 50), "waterBar")
              .AddDynamicText("", CairoFont.WhiteDetailText(), EnumTextOrientation.Center, waterFullnessMeterBounds.BelowCopy(-10, 5).WithFixedSize(60, 30), "watertemp")

              .AddInset(steamFullnessMeterBounds.ForkBoundingParent(2, 2, 2, 2), 2)
              .AddDynamicCustomDraw(steamFullnessMeterBounds, createFullnessMeterDraw(2, 100), "steamBar")
          .EndChildElements()
          .Compose()
      ;

      lastRedrawMs = capi.ElapsedMilliseconds;
    }

    public void Update()
    {
      SingleComposer.GetCustomDraw("waterBar").Redraw();
      SingleComposer.GetCustomDraw("steamBar").Redraw();
    }

    #region Drawings

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

    private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
    {
      // 1. Fire
      ctx.Save();
      Matrix m = ctx.Matrix;
      m.Translate(GuiElement.scaled(5), GuiElement.scaled(83));
      m.Scale(GuiElement.scaled(0.25), GuiElement.scaled(0.25));
      ctx.Matrix = m;
      capi.Gui.Icons.DrawFlame(ctx);

      double dy = 210 - 210 * (Attributes.GetFloat("fuelBurnTime", 0) / Attributes.GetFloat("maxFuelBurnTime", 1));
      ctx.Rectangle(0, dy, 200, 210 - dy);
      ctx.Clip();
      LinearGradient gradient = new LinearGradient(0, GuiElement.scaled(250), 0, 0);
      gradient.AddColorStop(0, new Color(1, 1, 0, 1));
      gradient.AddColorStop(1, new Color(1, 0, 0, 1));
      ctx.SetSource(gradient);
      capi.Gui.Icons.DrawFlame(ctx, 0, false, false);
      gradient.Dispose();
      ctx.Restore();
    }

    #endregion

    private void OnAttributesModified()
    {
      if (!IsOpened()) return;

      float ftemp = Attributes.GetFloat("furnaceTemperature");
      float wtemp = Attributes.GetFloat("waterTemperature");

      string fuelTemp = ftemp.ToString("#");
      string waterTemp = wtemp.ToString("#");

      fuelTemp += fuelTemp.Length > 0 ? "°C" : "";
      waterTemp += waterTemp.Length > 0 ? "°C" : "";

      if (ftemp > 0 && ftemp <= 20) fuelTemp = Lang.Get("Cold");
      if (wtemp > 0 && wtemp <= 20) waterTemp = Lang.Get("Cold");

      SingleComposer.GetDynamicText("fueltemp").SetNewText(fuelTemp);
      SingleComposer.GetDynamicText("watertemp").SetNewText(waterTemp);

      if (capi.ElapsedMilliseconds - lastRedrawMs > 500)
      {
        if (SingleComposer != null) SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
        lastRedrawMs = capi.ElapsedMilliseconds;
      }
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

      // SingleComposer.GetSlotGrid("fuelslot").OnGuiClosed(capi);

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