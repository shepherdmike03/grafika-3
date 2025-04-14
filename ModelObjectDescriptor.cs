using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GrafikaSzeminarium
{
    internal class ModelObjectDescriptor : IDisposable
    {
        private bool disposedValue;

        public uint Vao { get; private set; }
        public uint Vertices { get; private set; }
        public uint Colors { get; private set; }
        public uint Indices { get; private set; }
        public uint IndexArrayLength { get; private set; }

        private GL Gl;

        public unsafe static ModelObjectDescriptor CreateCube(GL gl)
        {
            uint vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

            // counter clockwise is front facing
            float[] vertexArray = new float[] {
                -0.5f, 1.0f, 0.5f, 0f, 0f, 1f,
                -0.5f, -1.0f, 0.5f, 0f, 0f, 1f,
                 0.5f, -1.0f, 0.5f, 0f, 0f, 1f,
                 0.5f, 1.0f, 0.5f, 0f, 0f, 1f
            };

            float[] colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
            };

            uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 2, 3,
                2, 1, 0,
                3, 2, 0
            };

            uint vertices = gl.GenBuffer();
            gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            // 0 is position; 2 is normals
            uint offsetPos = 0;
            uint offsetNormals = offsetPos + 3 * sizeof(float);
            uint vertexSize = offsetNormals + 3 * sizeof(float);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
            gl.EnableVertexAttribArray(2);
            gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint colors = gl.GenBuffer();
            gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            // 1 is color
            gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            gl.EnableVertexAttribArray(1);
            gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint indices = gl.GenBuffer();
            gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

            return new ModelObjectDescriptor() 
            { 
                Vao = vao, 
                Vertices = vertices, 
                Colors = colors, 
                Indices = indices, 
                IndexArrayLength = (uint)indexArray.Length, 
                Gl = gl 
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state if needed
                }

                Gl.DeleteBuffer(Vertices);
                Gl.DeleteBuffer(Colors);
                Gl.DeleteBuffer(Indices);
                Gl.DeleteVertexArray(Vao);

                disposedValue = true;
            }
        }

        ~ModelObjectDescriptor()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
