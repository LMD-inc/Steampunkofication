using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace SFK.Steamworks.RollingMachine
{
  public class RollingMachineRenderer : IRenderer
  {
    internal bool ShouldRender;
    internal bool IsRolling;
    private ICoreClientAPI api;
    private BlockPos pos;

    public Vec3f topRollerPos = new Vec3f(0, 7 / 16f, 0);
    public Vec3f downRollerPos = new Vec3f(0, 4.4f / 16f, 0);
    public Vec3f rollerOrig = new Vec3f(0.5f, 0.5f, 0.5f);
    public Vec3f handleOrig = new Vec3f(0.5f, 14.5f / 16f, 0.5f);

    TextureAtlasPosition rollertexpos;
    MeshRef toprollermeshref;
    MeshRef downrollermeshref;
    MeshRef handlemeshref;
    public Matrixf ModelMat = new Matrixf();
    public float AngleRad;
    float blockRotation;

    public RollingMachineRenderer(ICoreClientAPI coreClientAPI, BlockPos pos, MeshData handlemesh, float blockRot)
    {
      this.api = coreClientAPI;
      this.pos = pos;
      blockRotation = blockRot;
      handlemeshref = coreClientAPI.Render.UploadMesh(handlemesh);
    }

    public double RenderOrder
    {
      get { return 0.5; }
    }

    public int RenderRange => 24;

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
      if (!ShouldRender || toprollermeshref == null || downrollermeshref == null || handlemeshref == null || rollertexpos == null) return;

      IRenderAPI rpi = api.Render;
      Vec3d camPos = api.World.Player.Entity.CameraPos;
      Vec4f lightrgbs = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);

      rpi.GlDisableCullFace();
      rpi.GlToggleBlend(true);

      IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
      prog.RgbaLightIn = lightrgbs;

      // top roller
      rpi.BindTexture2d(rollertexpos.atlasTextureId);

      prog.ModelMatrix = ModelMat
        .Identity()
        .Translate(pos.X - camPos.X, pos.Y - camPos.Y + topRollerPos.Y, pos.Z - camPos.Z)
        .Translate(rollerOrig.X, rollerOrig.Y, rollerOrig.Z)
        .RotateYDeg(blockRotation)
        .RotateX(AngleRad)
        .Translate(-rollerOrig.X, -rollerOrig.Y, -rollerOrig.Z)
        .Values;

      prog.ViewMatrix = rpi.CameraMatrixOriginf;
      prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
      rpi.RenderMesh(toprollermeshref);

      // down roller
      prog.ModelMatrix = ModelMat
        .Identity()
        .Translate(pos.X - camPos.X, pos.Y - camPos.Y + downRollerPos.Y, pos.Z - camPos.Z)
        .Translate(rollerOrig.X, rollerOrig.Y, rollerOrig.Z)
        .RotateYDeg(blockRotation)
        .RotateX(-AngleRad)
        .Translate(-rollerOrig.X, -rollerOrig.Y, -rollerOrig.Z)
        .Values;

      rpi.RenderMesh(toprollermeshref);

      // handle
      prog.Tex2D = api.BlockTextureAtlas.AtlasTextureIds[0];

      prog.ModelMatrix = ModelMat
        .Identity()
        .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
        .Translate(handleOrig.X, handleOrig.Y, handleOrig.Z)
        .RotateYDeg(blockRotation)
        .RotateX(AngleRad)
        .Translate(-handleOrig.X, -handleOrig.Y, -handleOrig.Z)
        .Values;

      prog.ViewMatrix = rpi.CameraMatrixOriginf;
      prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
      rpi.RenderMesh(handlemeshref);

      prog.Stop();

      if (IsRolling)
      {
        AngleRad += deltaTime * 40 * GameMath.DEG2RAD;
      }
    }

    internal void UpdateRollersMeshes(MeshData rollerMesh, TextureAtlasPosition rollertexpos)
    {
      this.rollertexpos = rollertexpos;
      toprollermeshref?.Dispose();
      downrollermeshref?.Dispose();

      toprollermeshref = null;
      downrollermeshref = null;

      if (rollerMesh != null)
      {
        toprollermeshref = api.Render.UploadMesh(rollerMesh);
        downrollermeshref = api.Render.UploadMesh(rollerMesh);
      }
    }

    public void Dispose()
    {
      api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);

      toprollermeshref?.Dispose();
      downrollermeshref?.Dispose();
      handlemeshref?.Dispose();
    }
  }
}
