using System;
using System.Text;

using SFK.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SFK.Steamworks.Boiler
{
  public class BEBoiler : BlockEntityContainer, IHeatSource, ILiquidFLow
  {
    ILoadedSound ambientSound;
    public int CapacityLitresInput { get; set; } = 50;
    public int CapacityLitresOutput { get; set; } = 100;
    GuiDialogBoiler invDialog;
    internal BoilerInventory inventory;
    BlockBoiler ownBlock;

    public float prevFurnaceTemperature = 20;
    public float furnaceTemperature = 20;
    public int maxTemperature;
    public float fuelBurnTime;
    public float maxFuelBurnTime;
    public float smokeLevel;
    public bool canIgniteFuel;
    public double extinguishedTotalHours;
    public int steamProductionCoefitient => 1; // To use float coefitient need to think on portions logic and balance;
    bool clientSidePrevBurning;
    bool shouldRedraw;

    #region Liquid faces

    public BlockFacing[] LiquidPullFaces { get; set; } = new BlockFacing[1] { BlockFacing.EAST }; // Default just in case
    public BlockFacing[] LiquidPushFaces { get; set; } = new BlockFacing[0];
    public BlockFacing[] AcceptLiquidFromFaces { get; set; } = new BlockFacing[1] { BlockFacing.EAST }; // Default just in case

    #endregion

    #region Config

    public override string InventoryClassName => "boiler";
    public virtual string DialogTitle => Lang.Get("Boiler");
    public override InventoryBase Inventory => inventory;

    public virtual bool BurnsAllFuell => true;
    public virtual float HeatModifier => 1f;
    public virtual float BurnDurationModifier => 1f;
    public virtual int enviromentTemperature() => 20;
    public virtual float SoundLevel => 0.66f;

    #endregion

    public BEBoiler()
    {
      inventory = new BoilerInventory(null, null);
      inventory.SlotModified += OnSlotModified;

      inventory.OnGetAutoPushIntoSlot = GetAutoPushIntoSlot;
      inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;
    }

    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);
      inventory.LateInitialize("boiler-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);

      ownBlock = Block as BlockBoiler;

      if (ownBlock?.Attributes?["capacityLitresInput"].Exists == true)
      {
        CapacityLitresInput = ownBlock.Attributes["capacityLitresInput"].AsInt(50);
        (inventory[1] as ItemSlotLiquidOnly).CapacityLitres = CapacityLitresInput;
      }

      if (ownBlock?.Attributes?["capacityLitresOutput"].Exists == true)
      {
        CapacityLitresOutput = ownBlock.Attributes["capacityLitresOutput"].AsInt(100);
        (inventory[2] as ItemSlotGasOnly).CapacityLitres = CapacityLitresOutput;
      }

      InitSides();

      if (api.Side == EnumAppSide.Server)
      {
        RegisterGameTickListener(OnBurnTick, 100);
        RegisterGameTickListener(On500msTick, 500);
        RegisterGameTickListener(ProduceTick, 1000);
      }
    }

    private void InitSides()
    {
      AcceptLiquidFromFaces[0] = LiquidPullFaces[0] = BlockFacing.FromCode(Block.Variant["side"]);
    }

    public void ToggleAmbientSounds(bool on)
    {
      if (Api.Side != EnumAppSide.Client) return;

      if (on)
      {
        if (ambientSound == null || !ambientSound.IsPlaying)
        {
          ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams()
          {
            Location = new AssetLocation("sounds/environment/fireplace.ogg"),
            ShouldLoop = true,
            Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
            DisposeOnFinish = false,
            Volume = SoundLevel
          });

          ambientSound.Start();
        }
      }
      else
      {
        ambientSound?.Stop();
        ambientSound?.Dispose();
        ambientSound = null;
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

      MarkDirty();
      Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
    }

    #region Burning

    public bool IsBurning => this.fuelBurnTime > 0;
    public int getInventoryStackLimit() => 64;

    private void OnBurnTick(float dt)
    {
      if (Api is ICoreClientAPI)
      {
        return;
      }

      // Use up fuel
      if (fuelBurnTime > 0)
      {
        bool lowFuelConsumption = Math.Abs(furnaceTemperature - maxTemperature) < 50 && inputSlot.Empty;

        fuelBurnTime -= dt / (lowFuelConsumption ? 3 : 1);

        if (fuelBurnTime <= 0)
        {
          fuelBurnTime = 0;
          maxFuelBurnTime = 0;
          if (!canSmelt()) // This check avoids light flicker when a piece of fuel is consumed and more is available
          {
            setBlockState("extinct");
            extinguishedTotalHours = Api.World.Calendar.TotalHours;
          }
        }
      }

      if (!IsBurning && Block.Variant["burnstate"] == "extinct" && Api.World.Calendar.TotalHours - extinguishedTotalHours > 2)
      {
        canIgniteFuel = false;
        setBlockState("cold");
      }

      // Furnace is burning: Heat furnace
      if (IsBurning)
      {
        furnaceTemperature = changeTemperature(furnaceTemperature, maxTemperature, dt);
      }

      // Furnace is not burning and can burn: Ignite the fuel
      if (!IsBurning && canIgniteFuel && canSmelt())
      {
        igniteFuel();
      }


      if (canHeatInput())
      {
        heatInput(dt);
      }

      invDialog?.Update();
    }

    private void On500msTick(float dt)
    {
      if (Api is ICoreServerAPI && (IsBurning || prevFurnaceTemperature != furnaceTemperature))
      {
        MarkDirty();
      }

      prevFurnaceTemperature = furnaceTemperature;
    }
    public float changeTemperature(float fromTemp, float toTemp, float dt)
    {
      float diff = Math.Abs(fromTemp - toTemp);

      dt = dt + dt * (diff / 28);


      if (diff < dt)
      {
        return toTemp;
      }

      if (fromTemp > toTemp)
      {
        dt = -dt;
      }

      if (Math.Abs(fromTemp - toTemp) < 1)
      {
        return toTemp;
      }

      return fromTemp + dt;
    }

    private bool canSmelt()
    {
      CombustibleProperties fuelCopts = fuelCombustibleOpts;
      if (fuelCopts == null) return false;

      return BurnsAllFuell && fuelCopts.BurnTemperature * HeatModifier > 0;
    }

    private bool canHeatInput() => !inputSlot.Empty;

    public void heatInput(float dt)
    {
      float oldTemp = InputStackTemp;
      float nowTemp = oldTemp;
      float meltingPoint = 100; // Water boiling temperature. Patch and get from Collectible.GetTemperature if not only water would be used.

      // Only Heat. Cooling happens already in the itemstack
      if (oldTemp < furnaceTemperature)
      {
        float f = (1 + GameMath.Clamp((furnaceTemperature - oldTemp) / 30, 0, 1.6f)) * dt;
        if (nowTemp >= meltingPoint) f /= 11;

        float newTemp = changeTemperature(oldTemp, furnaceTemperature, f);
        // TODO: think about temperature functions.
        // int maxTemp = 400?
        // if (maxTemp > 0)
        // {
        //   newTemp = Math.Min(maxTemp, newTemp);
        // }

        if (oldTemp != newTemp)
        {
          InputStackTemp = newTemp;
          nowTemp = newTemp;
        }
      }
    }

    public float InputStackTemp
    {
      get
      {
        return GetTemp(inputStack);
      }
      set
      {
        SetTemp(inputStack, value);
      }
    }

    float GetTemp(ItemStack stack)
    {
      if (stack == null) return enviromentTemperature();

      return stack.Collectible.GetTemperature(Api.World, stack);
    }

    void SetTemp(ItemStack stack, float value)
    {
      if (stack == null) return;

      stack.Collectible.SetTemperature(Api.World, stack, value);
    }

    public void igniteFuel()
    {
      igniteWithFuel(fuelStack);

      fuelStack.StackSize -= 1;

      if (fuelStack.StackSize <= 0)
      {
        fuelStack = null;
      }
    }

    public void igniteWithFuel(IItemStack stack)
    {
      CombustibleProperties fuelCopts = stack.Collectible.CombustibleProps;

      maxFuelBurnTime = fuelBurnTime = fuelCopts.BurnDuration * BurnDurationModifier;
      maxTemperature = (int)(fuelCopts.BurnTemperature * HeatModifier);
      smokeLevel = fuelCopts.SmokeLevel;
      setBlockState("lit");
    }

    public void setBlockState(string state)
    {
      AssetLocation loc = Block.CodeWithVariant("burnstate", state);
      Block block = Api.World.GetBlock(loc);
      if (block == null) return;

      Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
      this.Block = block;
    }

    #endregion

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

    public bool OnBlockInteract(IPlayer byPlayer)
    {
      if (Api.Side == EnumAppSide.Client)
      {
        if (invDialog == null)
        {
          SyncedTreeAttribute dtree = new SyncedTreeAttribute();

          invDialog = new GuiDialogBoiler(DialogTitle, Inventory, Pos, dtree, Api as ICoreClientAPI);
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

      return true;
    }

    #endregion

    void SetDialogValues(ITreeAttribute dialogTree)
    {
      dialogTree.SetFloat("furnaceTemperature", furnaceTemperature);

      dialogTree.SetInt("maxTemperature", maxTemperature);
      dialogTree.SetFloat("maxFuelBurnTime", maxFuelBurnTime);
      dialogTree.SetFloat("fuelBurnTime", fuelBurnTime);

      if (inputStack != null)
      {
        dialogTree.SetFloat("inputTemperature", InputStackTemp);
      }
      else
      {
        dialogTree.RemoveAttribute("inputTemperature");
      }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
      base.ToTreeAttributes(tree);
      ITreeAttribute invtree = new TreeAttribute();
      Inventory.ToTreeAttributes(invtree);
      tree["inventory"] = invtree;

      tree.SetFloat("furnaceTemperature", furnaceTemperature);
      tree.SetInt("maxTemperature", maxTemperature);
      tree.SetFloat("fuelBurnTime", fuelBurnTime);
      tree.SetFloat("maxFuelBurnTime", maxFuelBurnTime);
      tree.SetDouble("extinguishedTotalHours", extinguishedTotalHours);
      tree.SetBool("canIgniteFuel", canIgniteFuel);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
      InitSides();

      base.FromTreeAttributes(tree, worldForResolving);
      Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));

      if (Api != null)
      {
        Inventory.AfterBlocksLoaded(Api.World);
      }

      furnaceTemperature = tree.GetFloat("furnaceTemperature");
      maxTemperature = tree.GetInt("maxTemperature");
      fuelBurnTime = tree.GetFloat("fuelBurnTime");
      maxFuelBurnTime = tree.GetFloat("maxFuelBurnTime");
      extinguishedTotalHours = tree.GetDouble("extinguishedTotalHours");
      canIgniteFuel = tree.GetBool("canIgniteFuel", true);

      if (Api?.Side == EnumAppSide.Client)
      {
        invDialog?.Update();

        if (invDialog != null) SetDialogValues(invDialog.Attributes);

        if (Api?.Side == EnumAppSide.Client && (clientSidePrevBurning != IsBurning || shouldRedraw))
        {
          ToggleAmbientSounds(IsBurning);
          clientSidePrevBurning = IsBurning;
          MarkDirty(true);
          shouldRedraw = false;
        }
      }
    }

    #region Helper getters

    public ItemSlot fuelSlot => inventory[0];
    public ItemSlot inputSlot => inventory[1];
    public ItemSlot outputSlot => inventory[2];
    public ItemStack fuelStack
    {
      get { return inventory[0].Itemstack; }
      set { inventory[0].Itemstack = value; inventory[0].MarkDirty(); }
    }

    public ItemStack inputStack
    {
      get { return inventory[1].Itemstack; }
      set { inventory[1].Itemstack = value; inventory[1].MarkDirty(); }
    }

    public ItemStack outputStack
    {
      get { return inventory[2].Itemstack; }
      set { inventory[2].Itemstack = value; inventory[2].MarkDirty(); }
    }

    public CombustibleProperties fuelCombustibleOpts => getCombustibleOpts(0);

    public CombustibleProperties getCombustibleOpts(int slotid)
    {
      ItemSlot slot = inventory[slotid];
      if (slot.Itemstack == null) return null;
      return slot.Itemstack.Collectible.CombustibleProps;
    }

    #endregion

    public ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
    {
      if (fromSlot.Itemstack?.Collectible.IsLiquid() == true)
      {
        // Water input face
        if (atBlockFace == BlockFacing.FromCode(Block.Variant["side"]).Opposite)
        {
          return inputSlot;
        }
      }
      else
      {
        if (atBlockFace == BlockFacing.UP)
        {
          return fuelSlot;
        }
      }

      return null;
    }

    public ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
    {
      return null;
    }

    private void ProduceTick(float dt)
    {
      if (Api?.Side == EnumAppSide.Server)
      {
        if (!inputSlot.Empty)
        {
          // If Boiler would be able to process not only water better to use BoilerRecipe here
          if (inputStack?.Item.Code.ToString() == "game:waterportion"
            // just in case.
            && (outputSlot.Empty || outputStack?.Item?.Code.ToString() == "sfk-steamworks:steamportion")
            && inputStack.StackSize > 0
            && InputStackTemp > 100)
          {
            if (outputStack?.StackSize >= CapacityLitresOutput)
            {
              // Boom?
              return;
            }

            int consumed = ((int)Math.Round(1 + InputStackTemp / 500));
            int produced = consumed * steamProductionCoefitient; // Need for future to use coefficient


            if (outputSlot.Empty)
            {
              outputStack = new ItemStack(Api.World.GetItem(new AssetLocation("sfk-steamworks:steamportion")), produced);
            }
            else
            {
              if (outputStack?.Item?.Code.ToString() != "sfk-steamworks:steamportion") return;
              outputStack.StackSize += produced;
            }

            inputSlot.TakeOut(consumed);

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

      if (ambientSound != null)
      {
        ambientSound.Stop();
        ambientSound.Dispose();
      }

      invDialog?.TryClose();
      invDialog?.Dispose();
      invDialog = null;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
    {
      sb.Clear();
      sb.Append(Block.GetPlacedBlockInfo(Api.World, Pos, forPlayer));
    }

    ~BEBoiler()
    {
      if (ambientSound != null)
      {
        ambientSound.Dispose();
      }
    }

    public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
    {
      return IsBurning ? 7 : 0;
    }
  }
}
