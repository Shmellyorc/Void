// ============================================================================
//  GLStateCache.cs
// ============================================================================
//  Tracks OpenGL bindings owned by the built-in backend to avoid redundant state changes.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Silk.NET.OpenGL;

namespace Void.Engine.Graphics.Rendering.OpenGL;

internal sealed class GLStateCache
{
    private readonly GL _gl;
    private readonly uint[] _texture2DBindings;
    private readonly bool[] _hasTexture2DBinding;

    private bool _hasProgram;
    private uint _program;

    private bool _hasVertexArray;
    private uint _vertexArray;

    private bool _hasElementArrayBuffer;
    private uint _elementArrayBuffer;

    private bool _hasCopyWriteBuffer;
    private uint _copyWriteBuffer;

    private bool _hasActiveTextureUnit;
    private int _activeTextureUnit;

    internal int MaxTextureUnits => _texture2DBindings.Length;

    internal GLStateCache(GL gl, int maxTextureUnits)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        if (maxTextureUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTextureUnits));

        _texture2DBindings = new uint[maxTextureUnits];
        _hasTexture2DBinding = new bool[maxTextureUnits];
    }

    internal void UseProgram(uint handle)
    {
        if (_hasProgram && _program == handle)
            return;

        _gl.UseProgram(handle);
        _program = handle;
        _hasProgram = true;
    }

    internal void BindVertexArray(uint handle)
    {
        if (_hasVertexArray && _vertexArray == handle)
            return;

        _gl.BindVertexArray(handle);
        _vertexArray = handle;
        _hasVertexArray = true;

        // GL_ELEMENT_ARRAY_BUFFER is VAO state. Changing VAOs means the cached
        // element-buffer binding is no longer authoritative until it is rebound.
        _hasElementArrayBuffer = false;
    }

    internal void BindElementArrayBuffer(uint handle)
    {
        if (_hasElementArrayBuffer && _elementArrayBuffer == handle)
            return;

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, handle);
        _elementArrayBuffer = handle;
        _hasElementArrayBuffer = true;
    }

    internal void BindCopyWriteBuffer(uint handle)
    {
        if (_hasCopyWriteBuffer && _copyWriteBuffer == handle)
            return;

        _gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, handle);
        _copyWriteBuffer = handle;
        _hasCopyWriteBuffer = true;
    }

    internal void BindTexture2D(int textureUnit, uint handle)
    {
        if ((uint)textureUnit >= (uint)MaxTextureUnits)
            throw new ArgumentOutOfRangeException(nameof(textureUnit));

        if (!_hasActiveTextureUnit || _activeTextureUnit != textureUnit)
        {
            _gl.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + textureUnit));
            _activeTextureUnit = textureUnit;
            _hasActiveTextureUnit = true;
        }

        if (_hasTexture2DBinding[textureUnit] && _texture2DBindings[textureUnit] == handle)
            return;

        _gl.BindTexture(TextureTarget.Texture2D, handle);
        _texture2DBindings[textureUnit] = handle;
        _hasTexture2DBinding[textureUnit] = true;
    }

    internal void ForgetProgram(uint handle)
    {
        if (_hasProgram && _program == handle)
            _hasProgram = false;
    }

    internal void ForgetVertexArray(uint handle)
    {
        if (_hasVertexArray && _vertexArray == handle)
        {
            _hasVertexArray = false;
            _hasElementArrayBuffer = false;
        }
    }

    internal void ForgetBuffer(uint handle)
    {
        if (_hasElementArrayBuffer && _elementArrayBuffer == handle)
            _hasElementArrayBuffer = false;

        if (_hasCopyWriteBuffer && _copyWriteBuffer == handle)
            _hasCopyWriteBuffer = false;
    }

    internal void ForgetTexture(uint handle)
    {
        for (int i = 0; i < _texture2DBindings.Length; i++)
        {
            if (_hasTexture2DBinding[i] && _texture2DBindings[i] == handle)
                _hasTexture2DBinding[i] = false;
        }
    }
}
