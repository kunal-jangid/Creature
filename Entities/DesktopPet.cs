using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Creature.Core.Physics;
using Creature.Core.StateMachine;
using Creature.Graphics;
using Point = System.Windows.Point;
using Image = System.Windows.Controls.Image;

namespace Creature.Entities;

public abstract class DesktopPet
{
    public string Id { get; }
    public Transform2D Transform { get; } = new();
    public PhysicsBody Physics { get; }
    public Rect MonitorWorkingArea { get; set; }
    public SpriteManager SpriteManager { get; }
    public StateMachine<DesktopPet> StateMachine { get; }
    public Image VisualElement { get; }

    private AnimationClip? _currentClip;
    private int _currentFrameIndex;
    private double _frameTimer;

    public string CurrentStateName => StateMachine.CurrentState?.GetType().Name.Replace("Bunny", "").Replace("State", "") ?? "None";

    protected DesktopPet(string id, SpriteManager spriteManager, Rect monitorWorkingArea)
    {
        Id = id;
        SpriteManager = spriteManager;
        MonitorWorkingArea = monitorWorkingArea;
        Physics = new PhysicsBody(Transform);
        StateMachine = new StateMachine<DesktopPet>(this);

        VisualElement = new Image
        {
            Width = Transform.BaseWidth,
            Height = Transform.BaseHeight,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1.0, 1.0)
        };
        RenderOptions.SetBitmapScalingMode(VisualElement, BitmapScalingMode.NearestNeighbor);

        UpdateFloor();
        Transform.Position = new Vector2D(monitorWorkingArea.Left + (monitorWorkingArea.Width - Transform.ScaledWidth) / 2, Physics.FloorY);
        SyncVisualTransform();
    }

    public void UpdateFloor()
    {
        Physics.FloorY = MonitorWorkingArea.Bottom - Transform.ScaledHeight;
    }

    public void PlayAnimation(string clipName, bool loop = true)
    {
        var clip = SpriteManager.GetClip(clipName);
        if (clip == null || clip == _currentClip)
            return;

        _currentClip = clip;
        _currentFrameIndex = 0;
        _frameTimer = 0;
        if (_currentClip.Frames.Length > 0)
        {
            VisualElement.Source = _currentClip.Frames[0];
        }
    }

    public virtual void Update(double dt)
    {
        StateMachine.Update(dt);
        Physics.Update(dt);
        UpdateAnimation(dt);
        SyncVisualTransform();
    }

    private void UpdateAnimation(double dt)
    {
        if (_currentClip == null || _currentClip.Frames.Length == 0)
            return;

        _frameTimer += dt * 1000.0;
        if (_frameTimer >= _currentClip.FrameDurationMs)
        {
            _frameTimer -= _currentClip.FrameDurationMs;
            _currentFrameIndex++;
            if (_currentFrameIndex >= _currentClip.Frames.Length)
            {
                if (_currentClip.Loop)
                {
                    _currentFrameIndex = 0;
                }
                else
                {
                    _currentFrameIndex = _currentClip.Frames.Length - 1;
                }
            }
            VisualElement.Source = _currentClip.Frames[_currentFrameIndex];
        }
    }

    public void SyncVisualTransform()
    {
        Canvas.SetLeft(VisualElement, Transform.Position.X);
        Canvas.SetTop(VisualElement, Transform.Position.Y);
        VisualElement.Width = Transform.ScaledWidth;
        VisualElement.Height = Transform.ScaledHeight;

        var scaleX = Transform.IsFacingLeft ? -Transform.Scale : Transform.Scale;
        VisualElement.RenderTransform = new ScaleTransform(scaleX, Transform.Scale);
    }

    public bool HitTest(Point point)
    {
        return Transform.BoundingBox.Contains(point);
    }

    public abstract void OnCursorHover();
}
