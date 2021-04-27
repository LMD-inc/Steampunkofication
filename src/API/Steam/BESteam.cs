using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Datastructures;

namespace Steampunkofication.API.Steam
{
  public class BESteam : BlockEntity, ISteam
  {
    protected int maxPressure = 100;//how many packets that can move at once
    protected int Capacitance => capacitance;//how many packets it can store
    protected int capacitance = 1;
    //protected int cachedCapacitance = 0;
    protected int Capacitor => capacitor;  //packets currently stored (the ints store the volts for each packet)
    protected int capacitor = 0;
    protected bool isOn = true;        //if it's not on it won't do any power processing
    protected List<ISteam> outputConnections; //what we are connected to output power
    protected List<ISteam> inputConnections; //what we are connected to receive power
    protected List<ISteam> usedconnections; //track if already traded with in a given turn (to prevent bouncing)
    protected List<BlockFacing> distributionFaces; //what faces are valid for distributing power
    protected List<BlockFacing> receptionFaces; //what faces are valid for receiving power
    bool distributiontick = false;
    public int MaxPressure { get { return maxPressure; } }
    public virtual bool IsOn { get { return isOn; } }
    public bool IsPowered { get { return IsOn && Capacitor > 0; } }
    protected bool notfirsttick = false;
    protected bool justswitched = false; //create a delay after the player switches power
    public BlockEntity EBlock
    {
      get { return this as BlockEntity; }
    }

    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);

      //TODO need to load list of valid faces from the JSON for this stuff
      SetupIOFaces();

      if (outputConnections == null) { outputConnections = new List<ISteam>(); }
      if (inputConnections == null) { inputConnections = new List<ISteam>(); }
      if (Block.Attributes == null) { api.World.Logger.Error("ERROR BES INITIALIZE HAS NO BLOCK"); return; }
      maxPressure = Block.Attributes["maxPressure"].AsInt(maxPressure);
      capacitance = Block.Attributes["capacitance"].AsInt(Capacitance);

      RegisterGameTickListener(OnTick, 75);
      notfirsttick = false;
    }
    //attempt to load power distribution and reception faces from attributes, and orient them to this blocks face if necessary
    public virtual void SetupIOFaces()
    {
      string[] cfaces = { };

      if (Block.Attributes == null)
      {
        distributionFaces = BlockFacing.ALLFACES.ToList<BlockFacing>();
        receptionFaces = BlockFacing.ALLFACES.ToList<BlockFacing>();
        return;
      }
      if (!Block.Attributes.KeyExists("receptionFaces")) { receptionFaces = BlockFacing.ALLFACES.ToList<BlockFacing>(); }
      else
      {
        cfaces = Block.Attributes["receptionFaces"].AsArray<string>(cfaces);
        receptionFaces = new List<BlockFacing>();
        foreach (string f in cfaces)
        {
          receptionFaces.Add(OrientFace(Block.Code.ToString(), BlockFacing.FromCode(f)));
        }
      }

      if (!Block.Attributes.KeyExists("distributionFaces")) { distributionFaces = BlockFacing.ALLFACES.ToList<BlockFacing>(); }
      else
      {
        cfaces = Block.Attributes["distributionFaces"].AsArray<string>(cfaces);
        distributionFaces = new List<BlockFacing>();
        foreach (string f in cfaces)
        {
          distributionFaces.Add(OrientFace(Block.Code.ToString(), BlockFacing.FromCode(f)));
        }
      }

    }
    public virtual void FindConnections()
    {
      FindInputConnections();
      FindOutputConnections();

    }
    protected virtual void FindInputConnections()
    {
      //BlockFacing probably has useful stuff to do this right

      foreach (BlockFacing bf in receptionFaces)
      {


        BlockPos bp = Pos.Copy().Offset(bf);

        BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
        var bee = checkblock as ISteam;
        if (bee == null) { continue; }
        if (bee.TryOutputConnection(this) && !inputConnections.Contains(bee)) { inputConnections.Add(bee); }

      }
    }

    protected virtual void FindOutputConnections()
    {
      //BlockFacing probably has useful stuff to do this right

      foreach (BlockFacing bf in distributionFaces)
      {
        BlockPos bp = Pos.Copy().Offset(bf);
        BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
        var bee = checkblock as ISteam;
        if (bee == null) { continue; }
        if (bee.TryInputConnection(this) && !outputConnections.Contains(bee)) { outputConnections.Add(bee); }

      }
    }
    //Allow devices to connection to each other

    //API
    public virtual bool TryInputConnection(ISteam connectto)
    {
      if (inputConnections == null) { inputConnections = new List<ISteam>(); }
      Vec3d vector = connectto.EBlock.Pos.ToVec3d() - Pos.ToVec3d();
      BlockFacing bf = BlockFacing.FromVector(vector.X, vector.Y, vector.Z);
      if (receptionFaces == null) { return false; }
      if (!receptionFaces.Contains(bf)) { return false; }
      if (!inputConnections.Contains(connectto)) { inputConnections.Add(connectto); MarkDirty(); }
      return true;
    }
    //API
    public virtual bool TryOutputConnection(ISteam connectto)
    {
      if (outputConnections == null) { outputConnections = new List<ISteam>(); }
      Vec3d vector = connectto.EBlock.Pos.ToVec3d() - Pos.ToVec3d();
      BlockFacing bf = BlockFacing.FromVector(vector.X, vector.Y, vector.Z);
      if (distributionFaces == null) { return false; }
      if (!distributionFaces.Contains(bf)) { return false; }
      if (!outputConnections.Contains(connectto)) { outputConnections.Add(connectto); MarkDirty(); }
      return true;
    }

    public virtual void OnTick(float par)
    {
      if (!notfirsttick)
      {
        // FindConnections();
        notfirsttick = true;
      }

      if (isOn && distributiontick)
      {
        // DistributePower();
        //FlushCapacitorCache();
        usedconnections = new List<ISteam>(); //clear record of connections for next tick
      }
      distributiontick = !distributiontick;
      justswitched = false;
    }
    //Tell a connection to remove itself
    //API
    public virtual void RemoveConnection(ISteam disconnect)
    {
      inputConnections.Remove(disconnect);
      outputConnections.Remove(disconnect);
    }
    public override void OnBlockBroken()
    {
      base.OnBlockBroken();

      foreach (ISteam bes in inputConnections) { bes.RemoveConnection(this); }
      foreach (ISteam bes in outputConnections) { bes.RemoveConnection(this); }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
      base.GetBlockInfo(forPlayer, dsc);
      dsc.AppendLine("   On:" + isOn.ToString());
      dsc.AppendLine("Pressure:" + maxPressure.ToString() + "Bar");
      dsc.AppendLine("Steam amount:" + Capacitor.ToString() + "/" + Capacitance.ToString());
      dsc.AppendLine("IN:" + inputConnections.Count.ToString() + " OUT:" + outputConnections.Count.ToString());
    }

    //Used for other power devices to offer this device some energy returns how much power was used
    //API
    public virtual int ReceivePacketOffer(ISteam from, int inPressuse, int inSteam)
    {
      if (usedconnections == null) { usedconnections = new List<ISteam>(); }
      if (!isOn) { return 0; }//Not even on
      // if (inPressuse != maxPressure) { DoOverload(); return 0; }//Incompatible power - bad!
      if (inSteam <= 0) { return 0; }
      if (Capacitor >= Capacitance) { return 0; }//already full
      inSteam = Math.Min(inSteam, maxPressure); //can only move a certain amount of amps - eg 2
      int usesteam = Math.Min(inSteam, Capacitance - Capacitor); //2
      usesteam = Math.Max(usesteam, 0);
      ChangeCapacitor(usesteam);

      usedconnections.Add(from);
      if (usesteam != 0) { MarkDirty(); }//not zero should be dirty
      return usesteam;//return 2
    }
    public virtual void DistributePower()
    {
      // bunch of checks to see if we can give power
      if (Capacitor == 0) { return; }
      if (usedconnections == null) { usedconnections = new List<ISteam>(); }
      if (!isOn) { return; } //can't generator power if off
      if (outputConnections == null) { return; } //nothing hooked up
      if (outputConnections.Count == 0) { return; }

      //figure out who needs power
      List<ISteam> tempconnections = new List<ISteam>();
      int powerreq = 0;
      foreach (ISteam ist in outputConnections)
      {
        int np = ist.NeedSteam();
        if (np == 0) { continue; }
        powerreq += np;
        tempconnections.Add(ist);
      }
      if (powerreq == 0) { return; } //Don't need to distribute any power
      bool gavepower = false;
      //cap the powerrequest to our max pressure, by the number of requests
      powerreq = Math.Min(powerreq, tempconnections.Count * maxPressure);
      //distribute what power we can
      //If we have more power than is requested, just go through and give power
      if (Capacitor >= powerreq)
      {
        foreach (ISteam ie in tempconnections)
        {
          int offer = ie.ReceivePacketOffer(this, MaxPressure, Math.Min(Capacitor, maxPressure));
          if (offer > 0) { ChangeCapacitor(-offer); gavepower = true; }
        }
        if (gavepower) { MarkDirty(true); }
        return;
      }

      //Not enough power to go around, have to divide it up
      int eachavail = Capacitor / tempconnections.Count;
      int leftover = Capacitor % tempconnections.Count;//remainder
      foreach (ISteam ie in tempconnections)
      {
        int offer = ie.ReceivePacketOffer(this, MaxPressure, eachavail);
        if (offer == 0) { continue; }
        gavepower = true;
        ChangeCapacitor(-offer);
        if (leftover > 0)
        {
          offer = ie.ReceivePacketOffer(this, MaxPressure, leftover);
          if (offer > 0)
          {
            leftover -= offer;
            ChangeCapacitor(-offer);
          }
        }
      }
      if (gavepower) { MarkDirty(true); }

    }
    public virtual void DoOverload()
    {
      ////BOOOOM!
      if (!IsOn) { return; }
      EnumBlastType blastType = EnumBlastType.OreBlast;
      var iswa = Api.World as IServerWorldAccessor;
      Api.World.BlockAccessor.SetBlock(0, Pos);
      if (iswa != null)
      {
        iswa.CreateExplosion(Pos, blastType, 4, 15);
        isOn = false;
      }

    }
    public virtual void TogglePower()
    {
      if (justswitched) { return; }
      isOn = !isOn;
      justswitched = true;
      Api.World.PlaySoundAt(new AssetLocation("sounds/electriczap"), Pos.X, Pos.Y, Pos.Z, null, false, 8, 1);
    }
    public virtual int NeedSteam()
    {
      int needs = 0;
      if (isOn)
      {
        needs = Capacitance - Capacitor;
        if (needs < 0) { needs = 0; }
        needs = Math.Min(needs, MaxPressure);
      }
      return needs;

    }
    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
      base.FromTreeAttributes(tree, worldAccessForResolve);


      //if (type == null) type = defaultType; // No idea why. Somewhere something has no type. Probably some worldgen ruins
      capacitor = tree.GetInt("capacitor");
      isOn = tree.GetBool("isOn");
    }
    public override void ToTreeAttributes(ITreeAttribute tree)
    {
      base.ToTreeAttributes(tree);

      tree.SetInt("capacitor", Capacitor);
      tree.SetBool("isOn", isOn);
    }
    public void ChangeCapacitor(int amount)
    {
      // if (distributiontick && amount > 0) { cachedCapacitance += amount; }
      // else
      //{
      capacitor += amount;

      //}
      capacitor = Math.Max(Capacitor, 0);
      capacitor = Math.Min(Capacitance, Capacitor);
    }
    //Take a block code (that ends in a cardinal direction) and a BlockFacing,
    //and rotate it, returning the appropriate blockfacing
    public static BlockFacing OrientFace(string checkBlockCode, BlockFacing toChange)
    {
      if (!toChange.IsHorizontal) { return toChange; }
      if (checkBlockCode.EndsWith("east"))
      {
        toChange = toChange.GetCW();
      }
      else if (checkBlockCode.EndsWith("south"))
      {
        toChange = toChange.GetCW().GetCW();
      }
      else if (checkBlockCode.EndsWith("west"))
      {
        toChange = toChange.GetCCW();
      }
      return toChange;
    }
  }
}