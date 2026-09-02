// ============================================================
//  MyGameGame.cs - Your Main Game Class
// ============================================================
//  This is where your game logic lives.
//  Override the methods below to add your own behavior.
//
//  See the Getting Started guide:
//  https://github.com/Shmellyorc/Void/wiki/Getting-Started
//
//  Full documentation:
//  https://github.com/Shmellyorc/Void/wiki
// ============================================================

using Void.Engine;
using Void.Engine.Systems;

namespace MyGame;

public sealed class MyGameGame(GameSettings setting) : Game(setting)
{
    // ============================================================
    //  OnEnter() - Called once when the game starts
    //  Use this for loading assets, setting up initial state, etc.
    // ============================================================
    protected override void OnEnter()
    {
    }

    // ============================================================
    //  OnUpdate() - Called every frame
    //  Use this for game logic: input, movement, AI, collision, etc.
    //  frameTime.DeltaTime = time since last frame (in seconds)
    // ============================================================
    protected override void OnUpdate(FrameTime frameTime)
    {
    }

    // ============================================================
    //  OnDraw() - Called every frame after OnUpdate
    //  Use this for rendering: sprites, primitives, text, etc.
    // ============================================================
    protected override void OnDraw(FrameTime frameTime)
    {
    }

    // ============================================================
    //  OnExit() - Called once when the game exits
    //  Use this for cleanup: saving data, releasing resources, etc.
    // ============================================================
    protected override void OnExit()
    {
    }
}