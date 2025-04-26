using AtomEngine;
using EngineLib;
using Silk.NET.OpenGL;
using System.Numerics;

namespace OpenglLib
{
    public class SkyBoxRenderSystem : IRenderSystem
    {
        const int BINDING_POINT = 0;
        private const int CUBE_VERTEX_COUNT = 36;

        public IWorld World { get; set; }
        private QueryEntity querySkyBoxEntities;
        private QueryEntity queryCameraEntities;

        private GL _gl;

        private uint _cubeVAO;
        private uint _cubeVBO;

        private uint _cubemapTexture;
        private bool _isCubeInitialized = false;

        public SkyBoxRenderSystem(IWorld world)
        {
            World = world;

            querySkyBoxEntities = this.CreateEntityQuery()
                .With<SkyBoxComponent>()
                ;

            queryCameraEntities = this.CreateEntityQuery()
                .With<CameraComponent>()
                .With<TransformComponent>()
            ;
        }

        public void Initialize()
        {
        }

        private unsafe void InitializeCube()
        {
            if (_isCubeInitialized)
                return;

            float[] skyboxVertices = {
                -1.0f,  1.0f, -1.0f,
                -1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,

                -1.0f, -1.0f,  1.0f,
                -1.0f, -1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f,  1.0f,
                -1.0f, -1.0f,  1.0f,

                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,

                -1.0f, -1.0f,  1.0f,
                -1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f, -1.0f,  1.0f,
                -1.0f, -1.0f,  1.0f,

                -1.0f,  1.0f, -1.0f,
                 1.0f,  1.0f, -1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                -1.0f,  1.0f,  1.0f,
                -1.0f,  1.0f, -1.0f,

                -1.0f, -1.0f, -1.0f,
                -1.0f, -1.0f,  1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                -1.0f, -1.0f,  1.0f,
                 1.0f, -1.0f,  1.0f
            };

            _cubeVAO = _gl.GenVertexArray();
            _cubeVBO = _gl.GenBuffer();

            _gl.BindVertexArray(_cubeVAO);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _cubeVBO);

            fixed (float* pVertices = skyboxVertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(skyboxVertices.Length * sizeof(float)), pVertices, BufferUsageARB.StaticDraw);
            }

            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);
            _gl.EnableVertexAttribArray(0);

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindVertexArray(0);

            _isCubeInitialized = true;
        }

        private unsafe uint CreateCubemap(SkyBoxComponent skyBox)
        {
            uint textureID = _gl.GenTexture();
            _cubemapTexture = textureID;

            _gl.BindTexture(TextureTarget.TextureCubeMap, textureID);

            if (skyBox.SkyBoxSourceType == SkyBoxSourceType.SixTexture)
            {
                LoadCubemapFace(skyBox.PosXTexture, TextureTarget.TextureCubeMapPositiveX);
                LoadCubemapFace(skyBox.NegXTexture, TextureTarget.TextureCubeMapNegativeX);
                LoadCubemapFace(skyBox.PosYTexture, TextureTarget.TextureCubeMapPositiveY);
                LoadCubemapFace(skyBox.NegYTexture, TextureTarget.TextureCubeMapNegativeY);
                LoadCubemapFace(skyBox.PosZTexture, TextureTarget.TextureCubeMapPositiveZ);
                LoadCubemapFace(skyBox.NegZTexture, TextureTarget.TextureCubeMapNegativeZ);
            }

            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

            return textureID;
        }

        private unsafe void LoadCubemapFace(Texture texture, TextureTarget target)
        {
            if (texture == null || texture.Width <= 0 || texture.Height <= 0)
                return;

            _gl.BindTexture(TextureTarget.TextureCubeMap, _cubemapTexture);
            texture.Bind(TextureUnit.Texture0 + BINDING_POINT);

            int width, height;
            _gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out width);
            _gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out height);

            byte[] textureData = new byte[width * height * 4];

            fixed (byte* ptr = textureData)
            {
                _gl.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
                _gl.TexImage2D(
                    target,
                    0,
                    (int)InternalFormat.Rgba8,
                    (uint)width,
                    (uint)height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    ptr
                );
            }
        }

        public void Render(double deltaTime, object? context)
        {
            if (context == null) return;
            if (context is GL gL) _gl = gL;

            if (!_isCubeInitialized) InitializeCube();

            Entity[] skyBoxEntities = querySkyBoxEntities.Build();
            if (skyBoxEntities.Length == 0)
                return;

            Entity[] cameraEntities = queryCameraEntities.Build();
            if (cameraEntities.Length == 0)
                return;

            int activeCameraIndex = -1;
            for (int i = 0; i < cameraEntities.Length; i++)
            {
                ref var camera = ref this.GetComponent<CameraComponent>(cameraEntities[i]);
                if (camera.IsActive)
                {
                    activeCameraIndex = i;
                    break;
                }
            }

            if (activeCameraIndex == -1)
                return;

            ref var activeCamera = ref this.GetComponent<CameraComponent>(cameraEntities[activeCameraIndex]);
            ref var cameraTransform = ref this.GetComponent<TransformComponent>(cameraEntities[activeCameraIndex]);

            foreach (var entity in skyBoxEntities)
            {
                ref var skyBox = ref this.GetComponent<SkyBoxComponent>(entity);
                if (skyBox.Material == null || 
                    skyBox.Material.Shader == null ||
                    skyBox.PosXTexture == null ||
                    skyBox.PosYTexture == null ||
                    skyBox.PosZTexture == null ||
                    skyBox.NegXTexture == null ||
                    skyBox.NegYTexture == null ||
                    skyBox.NegZTexture == null
                    )
                    continue;

                if (_cubemapTexture == 0)
                {
                    _cubemapTexture = CreateCubemap(skyBox);
                }

                var depthFunc = _gl.GetInteger(GetPName.DepthFunc);
                bool cullFaceEnabled = _gl.IsEnabled(EnableCap.CullFace);

                _gl.DepthFunc(DepthFunction.Lequal);

                if (cullFaceEnabled)
                {
                    _gl.Disable(EnableCap.CullFace);
                }


                //var viewMatrix = Matrix4x4.CreateLookAt(cameraTransform.Position, cameraTransform.Position + activeCamera.CameraFront, activeCamera.CameraUp);
                Matrix4x4 viewMatrix = activeCamera.ViewMatrix;
                Matrix4x4 viewMatrixWithoutTranslation = new Matrix4x4(
                    viewMatrix.M11, viewMatrix.M12, viewMatrix.M13, 0f,
                    viewMatrix.M21, viewMatrix.M22, viewMatrix.M23, 0f,
                    viewMatrix.M31, viewMatrix.M32, viewMatrix.M33, 0f,
                    0f, 0f, 0f, 1f
                );

                skyBox.Material.Use();
                skyBox.Material.SetUniform("viewWithoutTranslation", viewMatrixWithoutTranslation);
                //skyBox.Material.SetUniform("skybox", BINDING_POINT);
                skyBox.Material.SetUniform("skybox", 0);

                //_gl.ActiveTexture(TextureUnit.Texture0 + BINDING_POINT);
                _gl.ActiveTexture(TextureUnit.Texture0);
                _gl.BindTexture(TextureTarget.TextureCubeMap, _cubemapTexture);

                _gl.BindVertexArray(_cubeVAO);
                _gl.DrawArrays(PrimitiveType.Triangles, 0, CUBE_VERTEX_COUNT);
                _gl.BindVertexArray(0);

                _gl.DepthFunc((DepthFunction)depthFunc);
                if (cullFaceEnabled)
                {
                    _gl.Enable(EnableCap.CullFace);
                }
            }
        }

        public void Resize(Vector2 size)
        {
        }
    }
}
