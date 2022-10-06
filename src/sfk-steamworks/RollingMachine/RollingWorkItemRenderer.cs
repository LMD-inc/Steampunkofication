using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SFK.Steamworks.RollingMachine
{
  public class RollingWorkItemRenderer : IRenderer, ITexPositionSource
  {
    private ICoreClientAPI capi;
    private BlockPos pos;

    MeshRef workItemMeshRef;
    MeshRef outputItemMeshRef;


    ItemStack workItemStack;
    internal float progressPercent;

    int textureId;

    string tmpMetal;
    ITexPositionSource tmpTextureSource;
    Matrixf ModelMat = new Matrixf();

    float blockRotation;

    public double RenderOrder => 0.5;

    public int RenderRange => 24;

    public Size2i AtlasSize
    {
      get { return capi.BlockTextureAtlas.Size; }
    }

    public Vec3f shapeOrig = new Vec3f(0.5f, 0.5f, 0.5f);

    public TextureAtlasPosition this[string textureCode]
    {
      get { return tmpTextureSource[tmpMetal]; }
    }

    public RollingWorkItemRenderer(ICoreClientAPI capi, BlockPos pos, float blockRot)
    {
      this.pos = pos;
      this.capi = capi;
      blockRotation = blockRot;
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
      if (workItemStack == null) return;

      IRenderAPI rpi = capi.Render;
      IClientWorldAccessor worldAccess = capi.World;
      Vec3d camPos = worldAccess.Player.Entity.CameraPos;

      rpi.GlDisableCullFace();
      IStandardShaderProgram prog = rpi.StandardShader;
      prog.Use();
      prog.RgbaAmbientIn = rpi.AmbientColor;
      prog.RgbaFogIn = rpi.FogColor;
      prog.FogMinIn = rpi.FogMin;
      prog.FogDensityIn = rpi.FogDensity;
      prog.RgbaTint = ColorUtil.WhiteArgbVec;
      prog.DontWarpVertices = 0;
      prog.AddRenderFlags = 0;
      prog.ExtraGodray = 0;
      prog.OverlayOpacity = 0;

      if (workItemStack != null && workItemMeshRef != null && outputItemMeshRef != null)
      {
        int temp = (int)workItemStack.Collectible.GetTemperature(capi.World, workItemStack);

        Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
        float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(temp);
        int extraGlow = GameMath.Clamp((temp - 550) / 2, 0, 255);

        prog.NormalShaded = 1;
        prog.RgbaLightIn = lightrgbs;
        prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], extraGlow / 255f);

        prog.ExtraGlow = extraGlow;
        prog.Tex2D = textureId;

        // input
        prog.ModelMatrix = ModelMat
          .Identity()
          .Translate(pos.X - camPos.X, pos.Y - camPos.Y + 12.5f / 16f, pos.Z - camPos.Z)
          // rotate relative to origin
          .Translate(shapeOrig.X, shapeOrig.Y, shapeOrig.Z)
          .RotateYDeg(blockRotation)
          .Translate(-shapeOrig.X, -shapeOrig.Y, -shapeOrig.Z)
          // static pos, maybe apply transformInRoller later
          .Translate(0, 0, 7 / 16f)
          // transforming during progress
          .Translate(0, 0, - (1 - progressPercent) * (3.5f / 16f))
          .Scale(1, 1, 1 - progressPercent)
          .Values;

        prog.ViewMatrix = rpi.CameraMatrixOriginf;
        prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

        rpi.RenderMesh(workItemMeshRef);

        // output
        prog.ModelMatrix = ModelMat
          .Identity()
          .Translate(pos.X - camPos.X, pos.Y - camPos.Y + 13f / 16f, pos.Z - camPos.Z)
          // rotate relative to origin
          .Translate(shapeOrig.X, shapeOrig.Y, shapeOrig.Z)
          .RotateYDeg(blockRotation)
          .RotateYDeg(180) // to set right squash direction
          .Translate(-shapeOrig.X, -shapeOrig.Y, -shapeOrig.Z)
          // static pos, maybe apply transformInRoller later
          .Translate(0, 0, 8 / 16f)
          // transforming during progress
          .Translate(0, 0, - progressPercent * (4 / 16f)) // coef to move = half of pos offset in this dir
          .Scale(1, 1, progressPercent)
          .Values;

        rpi.RenderMesh(outputItemMeshRef);

        prog.Stop();
      }
    }

    public void SetContents(ItemStack inputStack, ItemStack outputStack, bool regen)
    {
      this.workItemStack = inputStack;

      if (regen)
      {
        RegenMesh(inputStack, ref workItemMeshRef);
        RegenMesh(outputStack, ref outputItemMeshRef);
      }
    }

    // inherited from VS forge
    void RegenMesh(ItemStack stack, ref MeshRef meshRef)
    {
      meshRef?.Dispose();
      meshRef = null;

      if (stack == null) return;

      Shape shape;

      tmpMetal = stack.Collectible.LastCodePart();
      MeshData mesh = null;

      string firstCodePart = stack.Collectible.FirstCodePart();
      if (firstCodePart == "metalplate")
      {
        tmpTextureSource = capi.Tesselator.GetTexSource(capi.World.GetBlock(new AssetLocation("game:platepile")));
        shape = Shape.TryGet(capi, "game:shapes/block/stone/forge/platepile.json");
        textureId = tmpTextureSource[tmpMetal].atlasTextureId;
        capi.Tesselator.TesselateShape("block-fcr", shape, out mesh, this, null, 0, 0, 0, stack.StackSize);

      }
      else if (firstCodePart == "workitem")
      {
        MeshData workItemMesh = ItemWorkItem.GenMesh(capi, stack, ItemWorkItem.GetVoxels(stack), out textureId);
        if (workItemMesh != null)
        {
          workItemMesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.75f, 0.75f, 0.75f);
          workItemMesh.Translate(0, -9f / 16f, 0);
          meshRef = capi.Render.UploadMesh(workItemMesh);
        }
      }
      else if (firstCodePart == "ingot")
      {
        tmpTextureSource = capi.Tesselator.GetTexSource(capi.World.GetBlock(new AssetLocation("game:ingotpile")));
        shape = Shape.TryGet(capi, "game:shapes/block/stone/forge/ingotpile.json");
        textureId = tmpTextureSource[tmpMetal].atlasTextureId;
        capi.Tesselator.TesselateShape("block-fcr", shape, out mesh, this, null, 0, 0, 0, stack.StackSize);
      }
      else if (stack.Collectible.Attributes?.IsTrue("rollable") == true)
      {
        if (stack.Class == EnumItemClass.Block)
        {
          mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
          textureId = capi.BlockTextureAtlas.AtlasTextureIds[0];
        }
        else
        {
          capi.Tesselator.TesselateItem(stack.Item, out mesh);
          textureId = capi.ItemTextureAtlas.AtlasTextureIds[0];
        }

        ModelTransform tf = stack.Collectible.Attributes["inRollerTransform"].AsObject<ModelTransform>();
        if (tf != null)
        {
          tf.EnsureDefaultValues();
          mesh.ModelTransform(tf);
        }
      }

      if (mesh != null)
      {
        //mesh.Rgba2 = null;
        meshRef = capi.Render.UploadMesh(mesh);
      }
    }

    public void Dispose()
    {
      capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
      workItemMeshRef?.Dispose();
      outputItemMeshRef?.Dispose();
    }
  }
}
