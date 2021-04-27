using Vintagestory.API.Common;

namespace Steampunkofication.API.Steam
{

  /// <summary>
  /// https://discord.gg/VbYQc7yfnF
  /// 
  /// Class to Distribute electricity between block entities
  /// note this is barebones - what connects to what, and whether it needs power, and to use up power
  /// you would have to create your own tick (default 75) to go through and distribute available power if applicable
  /// (or it can passively accept power)
  /// 
  /// Suggested guidelines
  /// LV -  16 Volts
  /// MV -  64 Volts
  /// HV - 256 Volts
  /// 
  /// OnTick length to handle your electricity updates - 75
  /// </summary>

  // Class to Distribute electricity between block entities
  // note this is barebones - what connects to what, and whether it needs power, and to use up power
  // you would have to create your own tick (default 75) to go through and distribute available power if applicable
  // (or it can passively accept power)

  public interface ISteam
  {
    //the block entity this ISteam belongs to
    BlockEntity EBlock { get; }
    //MaxAmps - How much power can be transferred in one turn
    int MaxPressure { get; }
    //MaxVolts - the voltage class of this device (should match whatever its hooked up to)
    //if device is on
    bool IsOn { get; }
    //Device has power
    bool IsPowered { get; }
    //Receive an offer for a power packet, return how much power it uses
    int ReceivePacketOffer(ISteam from, int inPressuse, int inAmount);
    //return if this object needs any power
    int NeedSteam();
    //Try to accept an ISteam as a connected power source (return false to refuse connection)
    bool TryInputConnection(ISteam connectto);
    //Try to accept an ISteam as a connected power destination
    bool TryOutputConnection(ISteam connectto);
    //Delete any connection the the supplied ISteam (should be called on every connection when the block is deleted, removed etc)
    void RemoveConnection(ISteam disconnect);

  }

}