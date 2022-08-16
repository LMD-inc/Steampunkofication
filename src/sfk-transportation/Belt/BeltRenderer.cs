using SFK.Transportation.Belt;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace Vintagestory.GameContent
{
  public class BeltRenderer : IRenderer
  {
    BEBelt be;
    private ICoreClientAPI api;

    public BEBehaviorMPBase beMechPower;
    MeshRef meshref;
    public Matrixf ModelMat = new Matrixf();
    public float AngleRad;
    Matrixf shadowMvpMat = new Matrixf();

    public double RenderOrder
    {
      get { return 0.5; }
    }

    public int RenderRange => 24;

    public BeltRenderer(ICoreClientAPI coreClientAPI, BEBelt bbe, MeshData mesh, BEBehaviorMPBase bebMechPower)
    {
      api = coreClientAPI;
      be = bbe;
      beMechPower = bebMechPower;

      meshref = coreClientAPI.Render.UploadMesh(mesh);
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
      if (meshref == null) return;

      BlockPos pos = be.Pos;
      IRenderAPI rpi = api.Render;
      Vec3d camPos = api.World.Player.Entity.CameraPos;

      // IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);

      // prog.ModelMatrix = ModelMat
      //   .Identity()
      //   .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
      //   .Translate(0.5f, 11f / 16f, 0.5f)
      //   .RotateY(AngleRad)
      //   .Translate(-0.5f, 0, -0.5f)
      //   .Values;

      // be.
    }

    public void Dispose()
    {
      api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);

      meshref.Dispose();
    }
  }
}