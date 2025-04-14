using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using Szeminarium;

namespace GrafikaSzeminarium
{
    internal class Program
    {
        private static IWindow graphicWindow;
        private static GL Gl;
        private static ImGuiController imGuiController;
        private static ModelObjectDescriptor glObject;
        private static CameraDescriptor camera = new CameraDescriptor();
        private static CubeArrangementModel cubeArrangementModel = new CubeArrangementModel();
        private static bool UsePerpendicularNormalVectors = true;

        // Shader uniform valtozok nevei
        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string UsePerpendicularNormals = "uUsePerpendicularNormals";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string ShinenessVariableName = "uShininess";

        private static float shininess = 50;
        private static uint program;

        static void Main(string[] args)
        {
            // Ablak-beallitasok es letrehozasa
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Lab3_1 dezsa";
            windowOptions.Size = new Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

private static void GraphicWindow_Load()
{
    // Initialize the OpenGL context.
    Gl = graphicWindow.CreateOpenGL();

    // Pull the starting camera position back by increasing the distance.
    for (int i = 0; i < 10; i++)
    {
        camera.IncreaseDistance();
    }

    // Set up input and keyboard handling.
    var inputContext = graphicWindow.CreateInput();
    foreach (var keyboard in inputContext.Keyboards)
    {
        keyboard.KeyDown += Keyboard_KeyDown;
    }

    // Update the viewport when the window is resized.
    graphicWindow.FramebufferResize += size =>
    {
        Gl.Viewport(size);
    };

    // Initialize ImGui for the UI.
    imGuiController = new ImGuiController(Gl, graphicWindow, inputContext);

    // Initialize the cube model object.
    glObject = ModelObjectDescriptor.CreateCube(Gl);

    Gl.ClearColor(System.Drawing.Color.White);
    Gl.Enable(EnableCap.CullFace);
    Gl.CullFace(TriangleFace.Back);

    Gl.Enable(EnableCap.DepthTest);
    Gl.DepthFunc(DepthFunction.Lequal);

    // Load, compile, and link the vertex and fragment shaders.
    uint vshader = Gl.CreateShader(ShaderType.VertexShader);
    uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

    Gl.ShaderSource(vshader, GetEmbeddedResourceAsString("Shaders.VertexShader.vert"));
    Gl.CompileShader(vshader);
    Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
    if (vStatus != (int)GLEnum.True)
        throw new Exception("Vertex shader compilation failed: " + Gl.GetShaderInfoLog(vshader));

    Gl.ShaderSource(fshader, GetEmbeddedResourceAsString("Shaders.FragmentShader.frag"));
    Gl.CompileShader(fshader);
    Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
    if (fStatus != (int)GLEnum.True)
        throw new Exception("Fragment shader compilation failed: " + Gl.GetShaderInfoLog(fshader));

    program = Gl.CreateProgram();
    Gl.AttachShader(program, vshader);
    Gl.AttachShader(program, fshader);
    Gl.LinkProgram(program);

    Gl.DetachShader(program, vshader);
    Gl.DetachShader(program, fshader);
    Gl.DeleteShader(vshader);
    Gl.DeleteShader(fshader);

    if ((ErrorCode)Gl.GetError() != ErrorCode.NoError)
    {
        // Optional error handling.
    }

    Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
    if (status == 0)
    {
        Console.WriteLine($"Shader program linking error: {Gl.GetProgramInfoLog(program)}");
    }
}

        private static string GetEmbeddedResourceAsString(string resourceRelativePath)
        {
            string resourceFullPath = Assembly.GetExecutingAssembly().GetName().Name + "." + resourceRelativePath;
            using (var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullPath))
            using (var resStreamReader = new StreamReader(resStream))
            {
                return resStreamReader.ReadToEnd();
            }
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            // Billentyu esemenyek a kamera mozgatasa es az animacio vezerelesere
            switch (key)
            {
                case Key.Left:
                    camera.DecreaseZYAngle();
                    break;
                case Key.Right:
                    camera.IncreaseZYAngle();
                    break;
                case Key.Down:
                    camera.IncreaseDistance();
                    break;
                case Key.Up:
                    camera.DecreaseDistance();
                    break;
                case Key.U:
                    camera.IncreaseZXAngle();
                    break;
                case Key.D:
                    camera.DecreaseZXAngle();
                    break;
                case Key.Space:
                    cubeArrangementModel.AnimationEnabled = !cubeArrangementModel.AnimationEnabled;
                    break;
            }
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // Animacio idobeli elorehaladasa (nem OpenGL hivas!)
            cubeArrangementModel.AdvanceTime(deltaTime);
            imGuiController.Update((float)deltaTime);
        }

        private static void GraphicWindow_Render(double deltaTime)
        {
            // Kepernyo torlese
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            // Vilagitasi parameterek beallitasa
            SetUniform3(LightColorVariableName, new Vector3(1f, 1f, 1f));
            SetUniform3(LightPositionVariableName, new Vector3(0f, 1.2f, 0f));
            SetUniform3(ViewPositionVariableName, new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
            SetUniform1(ShinenessVariableName, shininess);

            // Kamera nezeti es projekcios matrix beallitas
            var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Target, camera.UpVector);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>(
                (float)(Math.PI / 2), 1024f / 768f, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);

            // Az objektumok kirajzolasa
            Draw();

            // ImGui panel a vilagitas parametereinek modositasara
            ImGuiNET.ImGui.Begin("Lighting", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize | ImGuiNET.ImGuiWindowFlags.NoCollapse);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 5, 100);
            ImGuiNET.ImGui.End();

            imGuiController.Render();
        }

        private static void Draw()
        {
            // Parameter a kor alaku elrendezeshez
            float radius = 2.3f;
            float rotationAngle = MathF.PI / 9.0f;
            int numRectangles = 18;

            float shiftX = 0;
            float shiftY, shiftZ = radius;

            // Elso csoport: meroleges normalokkal
            UsePerpendicularNormalVectors = true;
            setUsePerpendicularNormals();
            shiftY = 2.0f;
            drawRectangles(radius, rotationAngle, numRectangles, shiftX, shiftY, shiftZ);

            // Masodik csoport: modositott (nem meroleges) normalokkal
            UsePerpendicularNormalVectors = false;
            setUsePerpendicularNormals();
            shiftY = -2.0f;
            drawRectangles(radius, rotationAngle, numRectangles, shiftX, shiftY, shiftZ);
        }

        private static void drawRectangles(float radius, float rotationAngle, int numRectangles, float shiftX, float shiftY, float shiftZ)
        {
            float x = 0.0f, z = 0.0f;

            // Az egyes objektumok kirajzolasa ciklikusan a kor menten
            for (int i = 0; i < numRectangles; i++)
            {
                float angle = i * rotationAngle;
                x = radius * (float)Math.Cos(angle) + shiftX;
                z = radius * (float)Math.Sin(angle) + shiftZ;

                // Modell transzformacio: skala, translacio, forgatas es eltolas a kozepontol
                var centerCubeScale = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
                var translationCube = Matrix4X4.CreateTranslation(x, shiftY, z);
                var rotationCube = Matrix4X4.CreateRotationY(angle);
                var shiftBackCenter = Matrix4X4.CreateTranslation(-radius, 0.0f, 0.0f);

                var modelMatrixForCube = centerCubeScale * translationCube * rotationCube * shiftBackCenter;
                SetModelMatrix(modelMatrixForCube);
                DrawModelObject(glObject);
            }
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            // Beallitjuk az objektum modell matrixat a shaderben
            SetMatrix(modelMatrix, ModelMatrixVariableName);

            // Normal matrix kiszamolasa: (M^-1)^T (a translacio nelkul)
            int location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform nem talalhato a shaderben.");
            }

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4)
            {
                M41 = 0,
                M42 = 0,
                M43 = 0,
                M44 = 1
            };

            Matrix4X4<float> modelInverse;
            if (!Matrix4X4.Invert(modelMatrixWithoutTranslation, out modelInverse))
            {
                throw new Exception("A modell matrix inverzet nem sikerult kiszamolni.");
            }
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInverse));

            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUniform1(string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform nem talalhato a shaderben.");
            }
            Gl.Uniform1(location, uniformValue);
            CheckError();
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform nem talalhato a shaderben.");
            }
            Gl.Uniform3(location, uniformValue);
            CheckError();
        }

        private static unsafe void setUsePerpendicularNormals()
        {
            // Allitsuk be a shader valtozat, hogy meroleges normalokat hasznaljon-e
            int location = Gl.GetUniformLocation(program, UsePerpendicularNormals);
            if (location == -1)
            {
                throw new Exception($"{UsePerpendicularNormals} uniform nem talalhato a shaderben.");
            }
            Gl.Uniform1(location, UsePerpendicularNormalVectors ? 1 : 0);
            CheckError();
        }

        private static unsafe void DrawModelObject(ModelObjectDescriptor modelObject)
        {
            // Az objektum kirajzolasa az index buffer es VAO alapu alapjan
            Gl.BindVertexArray(modelObject.Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, modelObject.Indices);
            Gl.DrawElements(PrimitiveType.Triangles, modelObject.IndexArrayLength, DrawElementsType.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetMatrix(Matrix4X4<float> matrix, string uniformName)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform nem talalhato a shaderben.");
            }
            Gl.UniformMatrix4(location, 1, false, (float*)&matrix);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
            {
                throw new Exception("GL.GetError() hibakod: " + error.ToString());
            }
        }

        private static void GraphicWindow_Closing()
        {
            // Eroforrasok felszabaditasa az ablak bezaraskor
            glObject.Dispose();
            Gl.DeleteProgram(program);
        }
    }
}
