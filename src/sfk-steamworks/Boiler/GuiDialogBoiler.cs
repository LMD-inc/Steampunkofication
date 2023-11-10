using System;

using Cairo;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SFK.Steamworks.Boiler
{
  public class GuiDialogBoiler : GuiDialogBlockEntity
  {
    long lastRedrawMs;
    EnumPosFlag screenPos;

    protected override double FloatyDialogPosition => 0.6;
    protected override double FloatyDialogAlign => 0.8;

    public override double DrawOrder => 0.2;

    public GuiDialogBoiler(string dialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition,
                           SyncedTreeAttribute tree, ICoreClientAPI capi)
        : base(dialogTitle, Inventory, BlockEntityPosition, capi)
    {
      if (IsDuplicate) return;

      tree.OnModified.Add(new TreeModifiedListener() { listener = OnAttributesModified });
      Attributes = tree;
    }

    private void OnInventorySlotModified(int slotid)
    {
      // Direct call can cause InvalidOperationException
      capi.Event.EnqueueMainThreadTask(SetupDialog, "setupboilerdlg");
    }

    void SetupDialog()
    {
      // TODO: deisgn further boiler GUI
      ElementBounds boilerBounds = ElementBounds.Fixed(0, 0, 240, 200);

      ElementBounds fuelSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 140, 1, 1);
      ElementBounds inputFullnessMeterBounds = ElementBounds.Fixed(120, 30, 40, 200);
      ElementBounds outputFullnessMeterBounds = ElementBounds.Fixed(180, 30, 40, 200);

      // 2. Around all that is 10 pixel padding
      ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
      bgBounds.BothSizing = ElementSizing.FitToChildren;
      bgBounds.WithChildren(boilerBounds);

      // 3. Finally Dialog
      ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
        .WithAlignment(EnumDialogArea.RightMiddle)
        .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

      SingleComposer = capi.Gui
          .CreateCompo("blockentityboiler" + BlockEntityPosition, dialogBounds)
          .AddShadedDialogBG(bgBounds)
          .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
          .BeginChildElements(bgBounds)
              .AddDynamicCustomDraw(boilerBounds, OnBgDraw, "symbolDrawer")
              .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 0 }, fuelSlotBounds, "fuelSlot")
              .AddDynamicText("", CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Center), fuelSlotBounds.RightCopy(5, 16).WithFixedSize(60, 30), "fueltemp")

              .AddInset(inputFullnessMeterBounds.ForkBoundingParent(2, 2, 2, 2), 2)
              .AddDynamicCustomDraw(inputFullnessMeterBounds, CreateFullnessMeterDraw(1, 50), "inputBar")
              .AddDynamicText("", CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Center), inputFullnessMeterBounds.BelowCopy(-10, 5).WithFixedSize(60, 30), "inputtemp")

              .AddInset(outputFullnessMeterBounds.ForkBoundingParent(2, 2, 2, 2), 2)
              .AddDynamicCustomDraw(outputFullnessMeterBounds, CreateFullnessMeterDraw(2, 100), "outputBar")
          .EndChildElements()
          .Compose()
      ;

      lastRedrawMs = capi.ElapsedMilliseconds;
    }

    public void Update()
    {
      SingleComposer.GetCustomDraw("inputBar").Redraw();
      SingleComposer.GetCustomDraw("outputBar").Redraw();
    }

    #region Drawings

    private DrawDelegateWithBounds CreateFullnessMeterDraw(int slotId, float capacityLitres)
    {
      return (ctx, surface, currentBounds) =>
      {
        ItemSlot liquidSlot = Inventory[slotId];
        if (liquidSlot.Empty) return;

        float itemsPerLitre = 1f;

        WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(liquidSlot.Itemstack);
        if (props != null)
        {
          itemsPerLitre = props.ItemsPerLitre;
        }

        float fullnessRelative = liquidSlot.StackSize / itemsPerLitre / capacityLitres;

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
          GuiElement.fillWithPattern(capi, ctx, new AssetLocation(loc.Path), true, false);
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
      float wtemp = Attributes.GetFloat("inputTemperature");

      string fuelTemp = ftemp.ToString("#");
      string inputTemp = wtemp.ToString("#");

      fuelTemp += fuelTemp.Length > 0 ? "°C" : "";
      inputTemp += inputTemp.Length > 0 ? "°C" : "";

      if (ftemp > 0 && ftemp <= 20) fuelTemp = Lang.Get("Cold");
      if (wtemp > 0 && wtemp <= 20) inputTemp = Lang.Get("Cold");

      SingleComposer.GetDynamicText("fueltemp").SetNewText(fuelTemp);
      SingleComposer.GetDynamicText("inputtemp").SetNewText(inputTemp);

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

      screenPos = GetFreePos("smallblockgui");
      OccupyPos("smallblockgui", screenPos);
      SetupDialog();
    }

    public override void OnGuiClosed()
    {
      Inventory.SlotModified -= OnInventorySlotModified;

      SingleComposer.GetSlotGrid("fuelSlot").OnGuiClosed(capi);

      base.OnGuiClosed();

      FreePos("smallblockgui", screenPos);
    }

    public override bool OnEscapePressed()
    {
      base.OnEscapePressed();
      OnTitleBarClose();
      return TryClose();
    }
  }
}
