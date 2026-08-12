// ============================================================================
//  Usings.cs
// ============================================================================
//  Global using directives for the FlappyBirb example.
//
//  These let us use engine types without typing the full namespace
//  every time. For example, instead of writing:
//
//    Void.Engine.Systems.Vect2 position = new(10, 20);
//
//  We can just write:
//
//    Vect2 position = new(10, 20);
//
//  Copyright (c) 2025 Void Engine Examples
//  Licensed under the MIT License.
//  See LICENSE file in the project root for full license information.
// ============================================================================

// FlappyBirb game classes
global using FlappyBirb;

// Engine core
global using Void.Engine;
global using Void.Engine.Systems;

// Assets and loading
global using Void.Engine.Assets;
global using Void.Engine.Assets.Loaders;
global using Void.Engine.Assets.Loaders.Fonts;
global using Void.Engine.Assets.Loaders.Spritesheets;

// Rendering and helpers
global using Void.Engine.Graphics;
global using Void.Engine.Helpers;

// Input
global using Void.Engine.Inputs.Keyboards;
global using Void.Engine.Inputs.Mouses;