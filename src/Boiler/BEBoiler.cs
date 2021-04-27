using System.IO;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

using Steampunkofication.API.Steam;

namespace Steampunkofication.src.Boiler
{
  class BEBoiler : BESteam
  {
    // how many power packets we can generate - will see if every more than one
    bool usesFuel = false;              // Whether item uses fuel
    protected List<string> fuelCodes;   // Valid item & block codes usable as fuel

    protected int fuelTicks = 0;        // Рow many OnTicks a piece of fuel will last for
    int genSteam = 100;                 // how many steam is generated per OnTick
    bool requiresWater = false;         // will check for water to produce power
    int waterUsage = 0;                 // how much water to use up
    int waterCapacity = 10000;          // how much water to use up
    int waterStored = 0;
    int waterReceived = 0;
    int heat = 0;
    int fuelCounter = 0;                // Сounts down to use fuel
    BlockFacing fuelHopperFace;         // which face fuel is loaded from
    bool fueled = false;                // whether device is currently fueld
    bool usesFuelWhileOn = false;       // always use fuel, even if no load (unless turned off)
    float waterUsePeriod = 2;           // how often to use water
    bool overloadIfNoWater = false;     // will it explode if it can't find water (assuming it uses water)
    double lastwaterused = 0;
    bool generating = false;
    bool animInit = false;
    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);
      if (Block.Attributes != null)
      {
        genSteam = Block.Attributes["genSteam"].AsInt(genSteam);
        fuelHopperFace = BlockFacing.FromCode(Block.Attributes["fuelHopperFace"].AsString("up"));
        fuelHopperFace = OrientFace(Block.Code.ToString(), fuelHopperFace);
        string[] fc = Block.Attributes["fuelCodes"].AsArray<string>();
        if (fc != null) { fuelCodes = fc.ToList<string>(); }
        fuelTicks = Block.Attributes["fuelTicks"].AsInt(1);
        usesFuel = Block.Attributes["usesFuel"].AsBool(false);
        usesFuelWhileOn = Block.Attributes["usesFuelWhileOn"].AsBool(false);
        waterUsage = Block.Attributes["waterUsage"].AsInt(waterUsage);
        waterUsePeriod = Block.Attributes["waterUsePeriod"].AsFloat(waterUsePeriod);
        overloadIfNoWater = Block.Attributes["overloadIfNoWater"].AsBool(overloadIfNoWater);
        fuelCounter = 0;
        if (lastwaterused == 0) { lastwaterused = Api.World.Calendar.TotalHours; }
      }
      if (api.World.Side == EnumAppSide.Client && animUtil != null)
      {
        float rotY = Block.Shape.rotateY;
        animUtil.InitializeAnimator("run", new Vec3f(0, rotY, 0));
        animUtil.StartAnimation(new AnimationMetaData() { Animation = "run", Code = "run", AnimationSpeed = 1, EaseInSpeed = 1, EaseOutSpeed = 1, Weight = 1, BlendMode = EnumAnimationBlendMode.Average });
        animInit = true;
      }
    }

    public override void OnTick(float par)
    {

      base.OnTick(par);
      if (isOn)
      {
        GenerateSteam(); //Create power packets if possible, base.Ontick will handle distribution attempts
        if (overloadIfNoWater && heat > 100 && waterStored == 0 && waterReceived > 0) { DoOverload(); }
      }

    }

    public virtual void GenerateSteam()
    {
      bool trysteam = DoGenerateSteam();
      generating = trysteam;

      if (Api.World.Side == EnumAppSide.Client && animUtil != null && animInit)
      {

        if (trysteam)
        {

          if (animUtil.activeAnimationsByAnimCode.Count == 0)
          {
            animUtil.StartAnimation(new AnimationMetaData() { Animation = "run", Code = "run", AnimationSpeed = 1, EaseInSpeed = 1, EaseOutSpeed = 1, Weight = 1, BlendMode = EnumAnimationBlendMode.Average });
          }
        }
        else
        {
          animUtil.StopAnimation("run");
        }

      }

      if (trysteam) { ChangeCapacitor(MaxPressure); }

      return;
    }

    public virtual bool DoGenerateSteam()
    {
      if (!isOn) { return false; }

      //if (Capacitor == Capacitance && !usesFuelWhileOn) { return false; }//not necessary to generate power

      if (!usesFuel) { return true; } //if we don't use fuel, we can make power

      //should really move fuel use to its own function
      if (fueled && fuelCounter < fuelTicks) //on going burning of current fuel item
      {
        fuelCounter++;
        return true;
      }
      //Now we begin trying to fuel
      fueled = false; fuelCounter = 0;
      BlockPos bp = Pos.Copy().Offset(fuelHopperFace);
      BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
      var inputContainer = checkblock as BlockEntityContainer;
      if (inputContainer == null) { return false; } //no fuel container at all
      if (inputContainer.Inventory.Empty) { return false; } //the fuel container is empty
                                                            //check each inventory slot in the container
      for (int c = 0; c < inputContainer.Inventory.Count; c++)
      {
        ItemSlot checkslot = inputContainer.Inventory[c];
        if (checkslot == null) { continue; }
        if (checkslot.StackSize == 0) { continue; }

        bool match = false;
        if (checkslot.Itemstack.Item != null && fuelCodes.Contains(checkslot.Itemstack.Item.Code.ToString())) { match = true; }
        else if (checkslot.Itemstack.Block != null && fuelCodes.Contains(checkslot.Itemstack.Block.Code.ToString())) { match = true; }
        if (match && checkslot.StackSize > 0)
        {

          checkslot.TakeOut(1);
          checkslot.MarkDirty();
          fueled = true;
        }
      }
      return fueled;
    }

    //generators don't receive steam?
    public override int ReceivePacketOffer(ISteam from, int inPressuse, int inAmount)
    {
      return 0;
    }
    BlockEntityAnimationUtil animUtil
    {
      get
      {
        BEBehaviorAnimatable bea = GetBehavior<BEBehaviorAnimatable>();
        if (bea == null) { return null; }
        return GetBehavior<BEBehaviorAnimatable>().animUtil;
      }
    }
    //TODO need turn on and turn off functions
    public override void TogglePower()
    {

      if (justswitched) { return; }
      isOn = !isOn;
      justswitched = true;
      if (Api.World.Side == EnumAppSide.Client && animUtil != null)
      {
        if (!animInit)
        {
          float rotY = Block.Shape.rotateY;
          animUtil.InitializeAnimator("run", new Vec3f(0, rotY, 0));
          animInit = true;
        }
        if (isOn)
        {

          animUtil.StartAnimation(new AnimationMetaData() { Animation = "run", Code = "run", AnimationSpeed = 1, EaseInSpeed = 1, EaseOutSpeed = 1, Weight = 1, BlendMode = EnumAnimationBlendMode.Average });
        }
        else
        {
          animUtil.StopAnimation("run");
        }

      }
      Api.World.PlaySoundAt(new AssetLocation("sounds/electriczap"), Pos.X, Pos.Y, Pos.Z, null, false, 8, 1);
    }
    public override int NeedSteam()
    {
      return 0;
    }
  }
}