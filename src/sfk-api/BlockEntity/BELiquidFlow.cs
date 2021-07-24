using System;
using System.Linq;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SFK.API
{
  public class BlockEntityLiquidFlow : BlockEntityContainer, ILiquidFLow
  {
    internal InventoryGeneric inventory;
    public override InventoryBase Inventory => inventory;
    public string inventoryClassName = "liquidholder";
    public override string InventoryClassName => inventoryClassName;
    public string LiquidFlowObjectLangCode = "liquidholder-contents";

    public BlockFacing[] LiquidPullFaces { get; set; } = new BlockFacing[0];
    public BlockFacing[] LiquidPushFaces { get; set; } = new BlockFacing[0];
    public BlockFacing[] AcceptLiquidFromFaces { get; set; } = new BlockFacing[0];

    public int CapacityLitres;
    protected float liquidFlowRate = 1;
    public virtual float LiquidFlowRate => liquidFlowRate;
    public BlockFacing LastReceivedFromDir;
    int liquidCheckRateMs;
    float liquidFlowAccum;

    public BlockEntityLiquidFlow() : base()
    {
    }

    public override void Initialize(ICoreAPI api)
    {
      InitInventory();

      base.Initialize(api);

      if (api is ICoreServerAPI)
      {
        // Randomize movement a bit
        RegisterDelayedCallback((dt) => RegisterGameTickListener(MoveLiquid, liquidCheckRateMs), 10 + api.World.Rand.Next(200));
      }
    }

    private void InitInventory()
    {
      if (Block?.Attributes != null)
      {
        if (Block.Attributes["pullLiquidFaces"].Exists)
        {
          string[] faces = Block.Attributes["pullLiquidFaces"].AsArray<string>(new string[0]);
          LiquidPullFaces = new BlockFacing[faces.Length];
          for (int i = 0; i < faces.Length; i++)
          {
            LiquidPullFaces[i] = BlockFacing.FromCode(faces[i]);
          }
        }

        if (Block.Attributes["pushLiquidFaces"].Exists)
        {
          string[] faces = Block.Attributes["pushLiquidFaces"].AsArray<string>(new string[0]);
          LiquidPushFaces = new BlockFacing[faces.Length];
          for (int i = 0; i < faces.Length; i++)
          {
            LiquidPushFaces[i] = BlockFacing.FromCode(faces[i]);
          }
        }

        if (Block.Attributes["acceptLiquidFromFaces"].Exists)
        {
          string[] faces = Block.Attributes["acceptLiquidFromFaces"].AsArray<string>(new string[0]);
          AcceptLiquidFromFaces = new BlockFacing[faces.Length];
          for (int i = 0; i < faces.Length; i++)
          {
            AcceptLiquidFromFaces[i] = BlockFacing.FromCode(faces[i]);
          }
        }

        liquidFlowRate = Block.Attributes["liquid-flowrate"].AsFloat(liquidFlowRate);
        liquidCheckRateMs = Block.Attributes["liquid-checkrateMs"].AsInt(200);
        inventoryClassName = Block.Attributes["inventoryClassName"].AsString(inventoryClassName);
        LiquidFlowObjectLangCode = Block.Attributes["liquidFlowObjectLangCode"].AsString(LiquidFlowObjectLangCode);
        CapacityLitres = Block.Attributes["capacityLitres"].AsInt(10);
      }

      if (Inventory == null)
      {
        inventory = new InventoryGeneric(1, null, null, (id, self) => new ItemSlotLiquidOnly(self, CapacityLitres));

        inventory.SlotModified += OnSlotModified;
        inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;
        inventory.OnGetAutoPushIntoSlot = GetAutoPushIntoSlot;
      }
    }

    public ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
    {
      if (LiquidPushFaces.Contains(atBlockFace))
      {
        return Inventory.FirstOrDefault(slot => slot is ItemSlotLiquidOnly);
      }

      return null;
    }

    // Return the slot where a pipe may push items into. Return null if it shouldn't move items into this inventory.
    public ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
    {
      if (LiquidPullFaces.Contains(atBlockFace) || AcceptLiquidFromFaces.Contains(atBlockFace))
      {
        return Inventory.FirstOrDefault(slot => slot is ItemSlotLiquidOnly);
      }

      return null;
    }

    public void MoveLiquid(float dt)
    {
      liquidFlowAccum = Math.Min(liquidFlowAccum + LiquidFlowRate, Math.Max(1, LiquidFlowRate * 1));
      if (liquidFlowAccum < 1) return;

      if (LiquidPushFaces != null && LiquidPushFaces.Length > 0 && Inventory.FirstOrDefault(slot => slot is ItemSlotLiquidOnly && !slot.Empty) != null)
      {
        ItemStack stack = Inventory.First(slot => slot is ItemSlotLiquidOnly && !slot.Empty).Itemstack;

        BlockFacing outputFace = LiquidPushFaces[Api.World.Rand.Next(LiquidPushFaces.Length)];
        int dir = stack.Attributes.GetInt("tubeDir", -1);
        BlockFacing desiredDir = dir >= 0 && LiquidPushFaces.Contains(BlockFacing.ALLFACES[dir]) ? BlockFacing.ALLFACES[dir] : null;

        // If we have a desired dir, try to go there
        if (desiredDir != null)
        {
          // Try spit it out first
          if (!TrySpitOut(desiredDir))
          {
            // Then try push it in there,
            if (!TryPushInto(desiredDir) && outputFace != desiredDir.Opposite)
            {
              // Otherwise try spit it out in a random face, but only if its not back where it came frome
              if (!TrySpitOut(outputFace))
              {
                TryPushInto(outputFace);
              }
            }
          }
        }
        else
        {
          // Without a desired dir, try to spit it out anywhere first
          if (!TrySpitOut(outputFace))
          {
            // Then try push it anywhere next
            TryPushInto(outputFace);
          }
        }

      }

      if (LiquidPullFaces != null && LiquidPullFaces.Length > 0 && Inventory.Empty)
      {
        BlockFacing inputFace = LiquidPullFaces[Api.World.Rand.Next(LiquidPullFaces.Length)];

        TryPullFrom(inputFace);
      }
    }

    public virtual void TryPullFrom(BlockFacing inputFace)
    {
      BlockPos InputPosition = Pos.AddCopy(inputFace);

      BlockEntity beInput = Api.World.BlockAccessor.GetBlockEntity(InputPosition);

      if (beInput is BlockEntityContainer beContainer)
      {
        ItemSlot sourceSlot = beContainer.Inventory.GetAutoPullFromSlot(inputFace.Opposite);

        if (sourceSlot is ItemSlotLiquidOnly sourceSlotLiq)
        {
          ItemSlot targetSlot = sourceSlot == null ? null : Inventory.FirstOrDefault(slot => slot is ItemSlotLiquidOnly);
          BlockEntityLiquidFlow beFlow = beContainer as BlockEntityLiquidFlow;

          if (sourceSlot != null && targetSlot != null)
          {
            // Temporary stub, sine ItemSlotLiquidOnly.TryPutInto works wrong.
            if (sourceSlotLiq.StackSize >= sourceSlotLiq.CapacityLitres) return;
            // Tubes must balance themselves until push at their max.
            if (beFlow != null && sourceSlot.StackSize >= targetSlot.StackSize) return;

            ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, (int)liquidFlowAccum);

            int qmoved = (sourceSlot as ItemSlotLiquidOnly).TryPutInto(targetSlot, ref op);

            if (qmoved > 0)
            {
              if (beFlow != null)
              {
                targetSlot.Itemstack.Attributes.SetInt("tubeDir", inputFace.Opposite.Index);
              }
              else
              {
                targetSlot.Itemstack.Attributes.RemoveAttribute("tubeDir");
              }

              sourceSlot.MarkDirty();
              targetSlot.MarkDirty();
              MarkDirty(false);
              beContainer?.MarkDirty();
            }

            if (qmoved > 0 && Api.World.Rand.NextDouble() < 0.2)
            {
              liquidFlowAccum -= qmoved;
            }
          }
        }
      }
    }

    private bool TryPushInto(BlockFacing outputFace)
    {
      BlockPos OutputPosition = Pos.AddCopy(outputFace);
      BlockEntity beOutput = Api.World.BlockAccessor.GetBlockEntity(OutputPosition);
      Block blockOutput = Api.World.BlockAccessor.GetBlock(OutputPosition);

      if (beOutput is BlockEntityContainer beContainer)
      {
        ItemSlot sourceSlot = Inventory.FirstOrDefault(slot => slot is ItemSlotLiquidOnly && !slot.Empty);
        if ((sourceSlot?.Itemstack?.StackSize ?? 0) == 0) return false;  //seems FirstOrDefault() method can sometimes give a slot with stacksize == 0, weird

        int tubeDir = sourceSlot.Itemstack.Attributes.GetInt("tubeDir");
        sourceSlot.Itemstack.Attributes.RemoveAttribute("tubeDir");

        ItemSlot targetSlot = beContainer.Inventory.GetAutoPushIntoSlot(outputFace.Opposite, sourceSlot);
        BlockEntityLiquidFlow beFlow = beOutput as BlockEntityLiquidFlow;
        BlockLiquidContainerBase blockLiqCont = blockOutput as BlockLiquidContainerBase;

        if (targetSlot != null && targetSlot is ItemSlotLiquidOnly targetSlotLiq)
        {
          // Temporary stub, sine ItemSlotLiquidOnly.TryPutInto works wrong.
          if (targetSlotLiq.Itemstack?.StackSize >= targetSlotLiq.CapacityLitres) return false;
          // Tubes must balance themselves until push at their max.
          if (beFlow != null && blockLiqCont == null && sourceSlot.StackSize <= targetSlotLiq.StackSize) return false;

          int quantity = (int)liquidFlowAccum;
          ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, quantity);

          int qmoved = 0;
          if (blockLiqCont != null)
          {
            qmoved = blockLiqCont.TryPutContent(Api.World, OutputPosition, sourceSlot.Itemstack, quantity);
            sourceSlot.TakeOut(qmoved);
          }
          else
          {
            qmoved = sourceSlot.TryPutInto(targetSlotLiq, ref op);
          }

          if (qmoved > 0)
          {
            if (beFlow != null && blockLiqCont == null)
            {
              targetSlotLiq.Itemstack.Attributes.SetInt("tubeDir", outputFace.Index);
            }
            else
            {
              targetSlotLiq.Itemstack.Attributes.RemoveAttribute("tubeDir");
            }

            sourceSlot.MarkDirty();
            targetSlotLiq.MarkDirty();
            MarkDirty(false);
            beFlow?.MarkDirty(false);

            liquidFlowAccum -= qmoved;

            return true;
          }
          else
          {
            //If the push failed, re-apply original tubeDir so that the itemStack still has it for next push attempt
            sourceSlot.Itemstack.Attributes.SetInt("tubeDir", tubeDir);
          }
        }
      }

      return false;
    }

    private bool TrySpitOut(BlockFacing outputFace)
    {
      if (!LiquidPushFaces.Contains(outputFace)) return false;

      if (Api.World.BlockAccessor.GetBlock(Pos.AddCopy(outputFace)).Replaceable >= 6000)
      {
        ItemSlot sourceSlot = Inventory.FirstOrDefault(slot => slot is ItemSlotLiquidOnly && !slot.Empty);

        ItemStack stack = sourceSlot.TakeOut((int)liquidFlowAccum);
        liquidFlowAccum -= stack.StackSize;

        stack.Attributes.RemoveAttribute("tubeDir");

        float velox = outputFace.Normalf.X / 10f + ((float)Api.World.Rand.NextDouble() / 20f - 1 / 20f) * Math.Sign(outputFace.Normalf.X);
        float veloy = outputFace.Normalf.Y / 10f + ((float)Api.World.Rand.NextDouble() / 20f - 1 / 20f) * Math.Sign(outputFace.Normalf.Y);
        float veloz = outputFace.Normalf.Z / 10f + ((float)Api.World.Rand.NextDouble() / 20f - 1 / 20f) * Math.Sign(outputFace.Normalf.Z);

        Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5 + outputFace.Normalf.X / 2, 0.5 + outputFace.Normalf.Y / 2, 0.5 + outputFace.Normalf.Z / 2), new Vec3d(velox, veloy, veloz));

        sourceSlot.MarkDirty();
        MarkDirty(false);
        return true;
      }

      return false;
    }

    private void OnSlotModified(int slot)
    {
      Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
    }

    public override void OnBlockBroken()
    {
      if (Api.World is IServerWorldAccessor)
      {
        Vec3d epos = Pos.ToVec3d().Add(0.5, 0.5, 0.5);
        foreach (var slot in inventory)
        {
          if (slot.Itemstack == null) continue;

          slot.Itemstack.Attributes.RemoveAttribute("tubeQHTravelled");
          slot.Itemstack.Attributes.RemoveAttribute("tubeDir");

          Api.World.SpawnItemEntity(slot.Itemstack, epos);
          slot.Itemstack = null;
          slot.MarkDirty();
        }
      }

      base.OnBlockBroken();
    }

    public override void OnReceivedServerPacket(int packetid, byte[] data)
    {
      base.OnReceivedServerPacket(packetid, data);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
      InitInventory();

      int index = tree.GetInt("lastReceivedFromDir");

      if (index < 0) LastReceivedFromDir = null;
      else LastReceivedFromDir = BlockFacing.ALLFACES[index];

      base.FromTreeAttributes(tree, worldForResolving);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
      base.ToTreeAttributes(tree);

      tree.SetInt("lastReceivedFromDir", LastReceivedFromDir?.Index ?? -1);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
      dsc.Clear();

#if DEBUG
      if (!inventory.Empty)
      {
        dsc.AppendLine("Contents:");

        foreach (ItemSlot slot in inventory)
        {
          if (slot.Empty) continue;

          // TODO: localize and pluralize
          dsc.AppendLine($"{slot.Itemstack.StackSize} litres of {slot.Itemstack.GetName()} / Max: {(slot as ItemSlotLiquidOnly).CapacityLitres}");
        }
      }
      else
      {
        dsc.AppendLine("Empty");
      }
#endif
    }
  }
}