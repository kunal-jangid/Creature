using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Creature.Core.Physics;
using Creature.Core.StateMachine;
using Creature.Graphics;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Image = System.Windows.Controls.Image;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace Creature.Entities;

public abstract class DesktopPet
{
    public string Id { get; }
    public string Name { get; set; }
    public double SimpnessFactor { get; set; } = 0.5;
    public double VisualOffsetY { get; set; } = 0.0;
    public Transform2D Transform { get; } = new();
    public PhysicsBody Physics { get; }
    public Rect MonitorWorkingArea { get; set; }
    public SpriteManager SpriteManager { get; }
    public StateMachine<DesktopPet> StateMachine { get; }
    public Image VisualElement { get; }
    public FrameworkElement NameLabel { get; }
    private readonly TextBlock _nameTextBlock;

    private AnimationClip? _currentClip;
    private int _currentFrameIndex;
    private double _frameTimer;

    public string CurrentStateName
    {
        get
        {
            if (StateMachine.CurrentState == null) return "None";
            var name = StateMachine.CurrentState.GetType().Name;
            if (name.EndsWith("State")) name = name[..^5];
            var species = GetType().Name.Replace("Entity", "");
            if (name.StartsWith(species)) name = name[species.Length..];
            return name;
        }
    }

    protected DesktopPet(string id, SpriteManager spriteManager, Rect monitorWorkingArea, string name = "Pet", double baseWidth = 32, double baseHeight = 32)
    {
        Id = id;
        Name = name;
        SpriteManager = spriteManager;
        MonitorWorkingArea = monitorWorkingArea;
        Transform.BaseWidth = baseWidth;
        Transform.BaseHeight = baseHeight;
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

        _nameTextBlock = new TextBlock
        {
            Text = Name,
            Foreground = Brushes.White,
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(4, 1, 4, 1)
        };

        NameLabel = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
            CornerRadius = new CornerRadius(4),
            Child = _nameTextBlock,
            IsHitTestVisible = false
        };

        UpdateFloor();
        Transform.Position = new Vector2D(monitorWorkingArea.Left + (monitorWorkingArea.Width - Transform.ScaledWidth) / 2, Physics.FloorY);
        SyncVisualTransform();
    }

    public void SetName(string name)
    {
        Name = name;
        _nameTextBlock.Text = name;
    }

    public void SetShowName(bool show)
    {
        NameLabel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    public void UpdateFloor()
    {
        Physics.FloorY = MonitorWorkingArea.Bottom - Transform.ScaledHeight;
    }

    public void PlayAnimation(string clipName, bool loop = true)
    {
        if (clipName == "BunnyRun")
        {
            VisualOffsetY = 6.0;
        }
        else
        {
            VisualOffsetY = 0.0;
        }

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

    public virtual void HandleCursorInteraction(Point cursorVirtualPos, double dt)
    {
        // Don't interrupt special hover reactions (Jump/Special/Flinch) while active
        if (CurrentStateName == "Jump" || CurrentStateName == "Special" || CurrentStateName == "Flinch")
            return;

        var petCenterX = Transform.Position.X + Transform.ScaledWidth / 2.0;
        var petCenterY = Transform.Position.Y + Transform.ScaledHeight / 2.0;
        var dx = cursorVirtualPos.X - petCenterX;
        var dy = cursorVirtualPos.Y - petCenterY;

        var isNear = Math.Abs(dx) <= 350.0 && Math.Abs(dy) <= 250.0;
        if (isNear)
        {
            // Always face cursor when near
            Transform.IsFacingLeft = dx < 0;

            // Follow cursor if simpness factor is high
            if (SimpnessFactor >= 0.7)
            {
                if (Math.Abs(dx) > 35.0)
                {
                    var moveRight = dx > 0;
                    var speed = 50.0;
                    var nextX = Transform.Position.X + (moveRight ? speed : -speed) * dt;

                    if (nextX >= MonitorWorkingArea.Left && nextX + Transform.ScaledWidth <= MonitorWorkingArea.Right)
                    {
                        Physics.Velocity = new Vector2D(moveRight ? speed : -speed, Physics.Velocity.Y);
                        PlayAnimation(GetRunAnimationName(), loop: true);
                    }
                    else
                    {
                        Physics.Velocity = new Vector2D(0, Physics.Velocity.Y);
                        PlayAnimation(GetIdleAnimationName(), loop: true);
                    }
                }
                else
                {
                    Physics.Velocity = new Vector2D(0, Physics.Velocity.Y);
                    PlayAnimation(GetIdleAnimationName(), loop: true);
                }
            }
        }
    }

    protected virtual string GetIdleAnimationName() => "BunnyLieDown";
    protected virtual string GetRunAnimationName() => "BunnyRun";

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
        Canvas.SetTop(VisualElement, Transform.Position.Y + VisualOffsetY * Transform.Scale);
        VisualElement.Width = Transform.ScaledWidth;
        VisualElement.Height = Transform.ScaledHeight;

        var scaleX = Transform.IsFacingLeft ? -1.0 : 1.0;
        VisualElement.RenderTransform = new ScaleTransform(scaleX, 1.0);

        NameLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelWidth = NameLabel.DesiredSize.Width > 0 ? NameLabel.DesiredSize.Width : 40.0;
        var labelHeight = NameLabel.DesiredSize.Height > 0 ? NameLabel.DesiredSize.Height : 16.0;
        var labelX = Transform.Position.X + (Transform.ScaledWidth - labelWidth) / 2.0;
        var labelY = Transform.Position.Y - labelHeight - 2.0;
        Canvas.SetLeft(NameLabel, labelX);
        Canvas.SetTop(NameLabel, labelY);
    }

    public bool HitTest(Point point)
    {
        return Transform.BoundingBox.Contains(point);
    }

    public abstract void OnCursorHover();
}
