using System;
using System.Linq;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SFK.API
{
  public class BlockEntityGasFlow : BlockEntityContainer, IGasFLow
  {
    internal InventoryGeneric inventory;
    public override InventoryBase Inventory => inventory;
    public string inventoryClassName = "gasholder";
    public override string InventoryClassName => inventoryClassName;
    public string GasFlowObjectLangCode = "gasholder-contents";

    public BlockFacing[] GasPullFaces { get; set; } = new BlockFacing[0];
    public BlockFacing[] GasPushFaces { get; set; } = new BlockFacing[0];
    public BlockFacing[] AcceptGasFromFaces { get; set; } = new BlockFacing[0];

    public int CapacityLitres;
    protected float gasFlowRate = 1;
    public virtual float GasFlowRate => gasFlowRate;
    public BlockFacing LastReceivedFromDir;
    int gasCheckRateMs;
    float gasFlowAccum;

    public BlockEntityGasFlow() : base()
    {
    }

    public override void Initialize(ICoreAPI api)
    {
      InitInventory();

      base.Initialize(api);

      if (api is ICoreServerAPI)
      {
        // Randomize movement a bit
        RegisterDelayedCallback((dt) => RegisterGameTickListener(MoveGas, gasCheckRateMs), 10 + api.World.Rand.Next(200));
      }
    }

    private void InitInventory()
    {
      if (Block?.Attributes != null)
      {
        if (Block.Attributes["pullGasFaces"].Exists)
        {
          string[] faces = Block.Attributes["pullGasFaces"].AsArray<string>(null);
          GasPullFaces = new BlockFacing[faces.Length];
          for (int i = 0; i < faces.Length; i++)
          {
            GasPullFaces[i] = BlockFacing.FromCode(faces[i]);
          }
        }

        if (Block.Attributes["pushGasFaces"].Exists)
        {
          string[] faces = Block.Attributes["pushGasFaces"].AsArray<string>(null);
          GasPushFaces = new BlockFacing[faces.Length];
          for (int i = 0; i < faces.Length; i++)
          {
            GasPushFaces[i] = BlockFacing.FromCode(faces[i]);
          }
        }

        if (Block.Attributes["acceptGasFromFaces"].Exists)
        {
          string[] faces = Block.Attributes["acceptGasFromFaces"].AsArray<string>(null);
          AcceptGasFromFaces = new BlockFacing[faces.Length];
          for (int i = 0; i < faces.Length; i++)
          {
            AcceptGasFromFaces[i] = BlockFacing.FromCode(faces[i]);
          }
        }

        gasFlowRate = Block.Attributes["gas-flowrate"].AsFloat(gasFlowRate);
        gasCheckRateMs = Block.Attributes["gas-checkrateMs"].AsInt(200);
        inventoryClassName = Block.Attributes["inventoryClassName"].AsString(inventoryClassName);
        GasFlowObjectLangCode = Block.Attributes["gasFlowObjectLangCode"].AsString(GasFlowObjectLangCode);
        CapacityLitres = Block.Attributes["capacityLitres"].AsInt(CapacityLitres);
      }

      if (Inventory == null)
      {
        inventory = new InventoryGeneric(1, null, null, (id, self) => new ItemSlotGasOnly(self, CapacityLitres));

        inventory.SlotModified += OnSlotModified;
        inventory.OnGetAutoPushIntoSlot = GetAutoPushIntoSlot;
        inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;
      }
    }

    public ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
    {
      if (GasPushFaces.Contains(atBlockFace))
      {
        return Inventory.FirstOrDefault(slot => slot is ItemSlotGasOnly);
      }

      return null;
    }

    // Return the slot where a pipe may push items into. Return null if it shouldn't move items into this inventory.
    public ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
    {
      if (GasPullFaces.Contains(atBlockFace) || AcceptGasFromFaces.Contains(atBlockFace))
      {
        return Inventory.FirstOrDefault(slot => slot is ItemSlotGasOnly);
      }

      return null;
    }

    public void MoveGas(float dt)
    {
      gasFlowAccum = Math.Min(gasFlowAccum + GasFlowRate, Math.Max(1, GasFlowRate * 1));
      if (gasFlowAccum < 1) return;

      if (GasPushFaces != null && GasPushFaces.Length > 0 && Inventory.FirstOrDefault(slot => slot is ItemSlotGasOnly && !slot.Empty) != null)
      {
        ItemStack stack = Inventory.First(slot => slot is ItemSlotGasOnly && !slot.Empty).Itemstack;

        BlockFacing outputFace = GasPushFaces[Api.World.Rand.Next(GasPushFaces.Length)];
        int dir = stack.Attributes.GetInt("tubeDir", -1);
        BlockFacing desiredDir = dir >= 0 && GasPushFaces.Contains(BlockFacing.ALLFACES[dir]) ? BlockFacing.ALLFACES[dir] : null;

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

      if (GasPullFaces != null && GasPullFaces.Length > 0 && Inventory.Empty)
      {
        BlockFacing inputFace = GasPullFaces[Api.World.Rand.Next(GasPullFaces.Length)];

        TryPullFrom(inputFace);
      }
    }

    public virtual void TryPullFrom(BlockFacing inputFace)
    {
      BlockPos InputPosition = Pos.AddCopy(inputFace);

      if (Api.World.BlockAccessor.GetBlockEntity(InputPosition) is BlockEntityGasFlow beGs)
      {
        ItemSlot sourceSlot = beGs.GetAutoPullFromSlot(inputFace.Opposite);
        ItemSlot targetSlot = sourceSlot == null ? null : Inventory.FirstOrDefault(slot => slot is ItemSlotGasOnly);
        BlockEntityGasFlow beFlow = beGs as BlockEntityGasFlow;

        if (sourceSlot != null && targetSlot != null && (beFlow == null || targetSlot.Empty))
        {
          if (sourceSlot.StackSize >= targetSlot.StackSize) return;

          ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, (int)gasFlowAccum);

          int qmoved = sourceSlot.TryPutInto(targetSlot, ref op);
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
            beFlow?.MarkDirty();
          }

          if (qmoved > 0 && Api.World.Rand.NextDouble() < 0.2)
          {
            gasFlowAccum -= qmoved;
          }
        }
      }
    }


    private bool TryPushInto(BlockFacing outputFace)
    {
      BlockPos OutputPosition = Pos.AddCopy(outputFace);

      if (Api.World.BlockAccessor.GetBlockEntity(OutputPosition) is BlockEntityGasFlow beFlow)
      {
        ItemSlot sourceSlot = Inventory.FirstOrDefault(slot => slot is ItemSlotGasOnly && !slot.Empty);
        if ((sourceSlot?.Itemstack?.StackSize ?? 0) == 0) return false;  //seems FirstOrDefault() method can sometimes give a slot with stacksize == 0, weird

        int tubeDir = sourceSlot.Itemstack.Attributes.GetInt("tubeDir");
        sourceSlot.Itemstack.Attributes.RemoveAttribute("tubeDir");

        ItemSlot targetSlot = beFlow.GetAutoPushIntoSlot(outputFace.Opposite, sourceSlot);
        BEGasContainer beCont = beFlow as BEGasContainer;

        if (targetSlot != null && targetSlot is ItemSlotGasOnly)
        {
          if (beCont != null && !targetSlot.Empty && targetSlot.Itemstack.Item.Code != sourceSlot.Itemstack.Item.Code) return false;

          if (targetSlot.StackSize >= sourceSlot.StackSize) return false;

          int quantity = (int)gasFlowAccum;
          ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, quantity);

          int qmoved = sourceSlot.TryPutInto(targetSlot, ref op);

          if (qmoved > 0)
          {
            if (beCont != null)
            {
              targetSlot.Itemstack.Attributes.RemoveAttribute("tubeDir");
            }
            else
            {
              targetSlot.Itemstack.Attributes.SetInt("tubeDir", outputFace.Index);
            }

            sourceSlot.MarkDirty();
            targetSlot.MarkDirty();
            MarkDirty(false);
            beFlow?.MarkDirty(false);

            gasFlowAccum -= qmoved;

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
      if (!GasPushFaces.Contains(outputFace)) return false;

      if (Api.World.BlockAccessor.GetBlock(Pos.AddCopy(outputFace)).Replaceable >= 6000)
      {
        ItemSlot sourceSlot = Inventory.FirstOrDefault(slot => slot is ItemSlotGasOnly && !slot.Empty);

        ItemStack stack = sourceSlot.TakeOut((int)gasFlowAccum);
        gasFlowAccum -= stack.StackSize;

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

    public override void OnBlockBroken(IPlayer byPlayer)
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

      base.OnBlockBroken(byPlayer);
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
          dsc.AppendLine($"{slot.Itemstack.StackSize} litres of {slot.Itemstack.GetName()} / Max: {(slot as ItemSlotGasOnly).CapacityLitres}");
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
