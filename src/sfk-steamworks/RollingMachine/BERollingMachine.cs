using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using System.Text;

namespace SFK.Steamworks.RollingMachine
{
  public enum EnumRollingMachineSection
  {
    Base,
    Handle
  }

  public class BERollingMachine : BlockEntityContainer, ITexPositionSource
  {
    InventoryRollingMachine inv;
    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => "rollingmachine";
    public ItemSlot rollersSlot => inv[0];
    public ItemSlot workItemSlot => inv[1];

    public bool HasRollers => !rollersSlot.Empty;

    private bool isRolling;
    public bool IsRolling
    {
      get
      {
        return isRolling;
      }
      set
      {
        isRolling = value;

        if (renderer != null)
        {
          renderer.IsRolling = isRolling;
          renderer.ShouldRender = isRolling;
          UpdateRollingState();
        }
      }
    }

    public bool CanRoll => HasRollers;

    #region Render

    public BlockFacing Facing { get; protected set; } = BlockFacing.NORTH;
    float RotateY = 0f;

    public string RollersMaterial
    {
      get
      {
        if (!HasRollers) return null;
        return rollersSlot.Itemstack.Collectible.Variant["metal"];
      }
    }
    MeshData RollingMachineStandMesh
    {
      get
      {
        object value;
        Api.ObjectCache.TryGetValue("rollingmachinestandmesh", out value);
        return (MeshData)value;
      }
      set
      {
        Api.ObjectCache["rollingmachinestandmesh"] = value;
      }
    }

    MeshData RollerMesh;

    MeshData HandleMesh;

    RollingMachineRenderer renderer;

    ITexPositionSource tmpTextureSource;

    public TextureAtlasPosition this[string textureCode]
    {
      get { return tmpTextureSource["roller-" + RollersMaterial]; }
    }

    public Size2i AtlasSize
    {
      get { return ((ICoreClientAPI)Api).BlockTextureAtlas.Size; }
    }

    TextureAtlasPosition rollertexpos;

    #endregion

    public BERollingMachine()
    {
      inv = new InventoryRollingMachine(this, 2);
    }

    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);

      inv.LateInitialize(InventoryClassName + "-" + Pos, api);

      Facing = BlockFacing.FromCode(Block.Variant["side"]);
      if (Facing == null) Facing = BlockFacing.NORTH;

      switch (Facing.Index)
      {
        case 1:
          RotateY = 270;
          break;
        case 2:
          RotateY = 180;
          break;
        case 3:
          RotateY = 90;
          break;
        default:
          break;
      }

      if (api is ICoreServerAPI sapi)
      {
        RegisterGameTickListener(OnServerTick, 200);
      }

      if (api is ICoreClientAPI capi)
      {
        renderer = new RollingMachineRenderer(capi, Pos, GenMesh("handle"), RotateY);

        capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "rollingmachine");

        if (RollingMachineStandMesh == null)
        {
          RollingMachineStandMesh = GenMesh("stand");
        }

        if (HandleMesh == null)
        {
          HandleMesh = GenMesh("handle");
        }

        if (HasRollers && RollerMesh == null)
        {
          RollerMesh = GenRollerMesh(capi);
          renderer.UpdateRollersMeshes(RollerMesh, rollertexpos);
        }
      }
    }

    #region Events

    private void OnServerTick(float dt)
    {
      if (!HasRollers) return;

      if (!workItemSlot.Empty)
      {

      }
    }

    internal bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumRollingMachineSection section)
    {
      if (section == EnumRollingMachineSection.Base && byPlayer.Entity.Controls.ShiftKey)
      {
        return TryPut(world, byPlayer, blockSel);
      }

      if (section == EnumRollingMachineSection.Handle && CanRoll)
      {
        IsRolling = true;
        return true;
      }

      return false;
    }

    internal bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      IsRolling = true;
      return CanRoll;
    }

    internal void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      IsRolling = false;
    }

    private void OnRetesselated()
    {
      if (renderer == null) return; // Maybe already disposed

      renderer.ShouldRender = HasRollers && IsRolling;
    }

    #endregion

    private bool TryPut(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      ItemSlot sourceSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
      if (sourceSlot.Itemstack == null) return false;
      ItemStack SourceStack = sourceSlot.Itemstack;

      // rollers
      if (!HasRollers)
      {
        if (SourceStack.Collectible.FirstCodePart() == "roller")
        {
          if (SourceStack.StackSize < 2)
          {
            (Api as ICoreClientAPI)?.TriggerIngameError(this, "require2rollers", Lang.Get("Please add 2 rollers at the same time!"));
            return true;
          }

          rollersSlot.Itemstack = sourceSlot.TakeOut(2);

          if (Api is ICoreClientAPI capi && RollerMesh == null)
          {
            RollerMesh = GenRollerMesh(capi);
            renderer.UpdateRollersMeshes(RollerMesh, rollertexpos);
          }

          sourceSlot.MarkDirty();
          MarkDirty(true);
        }

        return false;
      }

      // work item
      if (SourceStack.Collectible is IAnvilWorkable workableobj && workItemSlot.Empty)
      {
        int requiredTier = workableobj.GetRequiredAnvilTier(SourceStack);
        if (requiredTier > RollersTier)
        {
          if (world.Side == EnumAppSide.Client)
          {
            (Api as ICoreClientAPI)?.TriggerIngameError(this, "toolowtier", Lang.Get("Working this metal needs a tier {0} rollers", requiredTier));
          }

          return false;
        }

        if (workableobj.CanWork(SourceStack) && GetRecipe(SourceStack) != null)
        {
          // item ready to be worked and Found Recipe, put item
          int qm = sourceSlot.TryPutInto(world, workItemSlot, 1);

          if (qm > 0)
          {
            sourceSlot.TakeOut(qm);
            return true;
          }
        }
      }

      return false;
    }

    public bool CanWorkCurrent
    {
      get { return WorkItemStack != null && (WorkItemStack.Collectible as IAnvilWorkable).CanWork(WorkItemStack); }
    }

    public ItemStack WorkItemStack
    {
      get { return workItemSlot.Itemstack; }
    }

    public RollingRecipe GetRecipe(ItemStack stack)
    {
      if (stack == null) return null;

      return SFKApiAdditions.GetRollingRecipes(Api).Find(r => r.Ingredient.SatisfiesAsIngredient(stack));
    }

    public RollingRecipe GetCurrentRecipe()
    {
      if (WorkItemStack == null) return null;

      return GetRecipe(WorkItemStack);
    }

    private int RollersTier
    {
      get
      {
        if (!HasRollers) return 0;

        MetalPropertyVariant var;

        if (Api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(RollersMaterial, out var))
        {
          return var.Tier;
        }

        return 0;
      }
    }

    internal MeshData GenMesh(string type)
    {
      Block block = Api.World.BlockAccessor.GetBlock(Pos);
      if (block.BlockId == 0) return null;

      ICoreClientAPI capi = Api as ICoreClientAPI;
      ITesselatorAPI mesher = capi.Tesselator;

      return ObjectCacheUtil.GetOrCreate(capi, type, () =>
      {
        MeshData mesh;
        mesher.TesselateShape(block, Shape.TryGet(capi, $"sfksteamworks:shapes/block/machine/rollingmachine/{type}.json"), out mesh);
        return mesh;
      });
    }

    internal MeshData GenRollerMesh(ICoreClientAPI capi)
    {
      if (!HasRollers) return null;

      Block tmpblock = capi.World.BlockAccessor.GetBlock(Pos);
      Item rollerItem = rollersSlot.Itemstack.Item;
      tmpTextureSource = capi.Tesselator.GetTexSource(tmpblock);
      rollertexpos = capi.BlockTextureAtlas.GetPosition(tmpblock, "metal");

      return ObjectCacheUtil.GetOrCreate(capi, $"roller-{RollersMaterial}", () =>
      {
        MeshData rollerMesh;
        capi.Tesselator.TesselateItem(rollerItem, out rollerMesh, this);
        return rollerMesh;
      });
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
      if (Block == null) return false;

      mesher.AddMeshData(
        this.RollingMachineStandMesh.Clone()
        .Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, RotateY * GameMath.DEG2RAD, 0)
      );

      // when not in action - apply static mesh tesselation renderer
      if (!isRolling)
      {
        Vec3f Rot = GetRotation(renderer.AngleRad);

        mesher.AddMeshData(
          this.HandleMesh.Clone()
          .Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, RotateY * GameMath.DEG2RAD, 0)
          .Rotate(renderer.handleOrig, Rot.X, Rot.Y, -Rot.Z)
        );

        if (HasRollers)
        {
          mesher.AddMeshData(
              this.RollerMesh.Clone()
              .Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, RotateY * GameMath.DEG2RAD, 0)
              .Rotate(renderer.rollerOrig, Rot.X, Rot.Y, -Rot.Z)
              .Translate(renderer.topRollerPos)
          );

          mesher.AddMeshData(
              this.RollerMesh.Clone()
              .Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, RotateY * GameMath.DEG2RAD, 0)
              .Rotate(renderer.rollerOrig, Rot.X, Rot.Y, Rot.Z)
              .Translate(renderer.downRollerPos)
          );
        }
      }

      return true;
    }

    Vec3f GetRotation(float AngleRad)
    {
      switch (RotateY)
      {
        default:
        case 0:
          return new Vec3f(AngleRad, 0, 0);
        case 90:
          return new Vec3f(0, 0, AngleRad);
        case 180:
          return new Vec3f(-AngleRad, 0, 0);
        case 270:
          return new Vec3f(0, 0, -AngleRad);
      }
    }

    bool prevIsRolling;
    void UpdateRollingState()
    {
      if (Api?.World == null) return;

      if (prevIsRolling != isRolling)
      {
        Api.World.BlockAccessor.MarkBlockDirty(Pos, OnRetesselated);

        if (isRolling)
        {
          // ambientSound?.Start();
        }
        else
        {
          // ambientSound?.Stop();
        }

        if (Api.Side == EnumAppSide.Server)
        {
          MarkDirty();
        }
      }

      prevIsRolling = isRolling;
    }

    #region TreeAttributes

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
      base.FromTreeAttributes(tree, worldForResolving);
      Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));

      if (Api != null)
      {
        Inventory.AfterBlocksLoaded(Api.World);
      }

      if (worldForResolving.Side == EnumAppSide.Client)
      {
        UpdateRollingState();
      }
    }

    #endregion

    #region Blockinfo

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
    {
      sb.Clear();
      sb.Append(Block.GetPlacedBlockInfo(Api.World, Pos, forPlayer));
    }

    #endregion
  }
}
