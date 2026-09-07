using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Creature.Entities;
using Point = System.Windows.Point;

namespace Creature.Simulation;

public class SimulationEngine
{
    private readonly List<DesktopPet> _pets = new();
    private readonly Stopwatch _stopwatch = new();
    private double _lastTimestamp;
    private bool _isRunning;

    public bool IsPaused { get; set; }
    public IReadOnlyList<DesktopPet> Pets => _pets;

    public void AddPet(DesktopPet pet)
    {
        _pets.Add(pet);
    }

    public void RemovePet(DesktopPet pet)
    {
        _pets.Remove(pet);
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _stopwatch.Restart();
        _lastTimestamp = 0;
        CompositionTarget.Rendering += OnRendering;
    }

    public void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        CompositionTarget.Rendering -= OnRendering;
        _stopwatch.Stop();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var current = _stopwatch.Elapsed.TotalSeconds;
        var dt = current - _lastTimestamp;
        _lastTimestamp = current;

        // Clamp dt to avoid physics spiral on lag spike
        if (dt > 0.05) dt = 0.05;

        if (!IsPaused)
        {
            foreach (var pet in _pets)
            {
                pet.Update(dt);
            }
        }
    }

    public DesktopPet? HitTest(Point localPoint)
    {
        return _pets.FirstOrDefault(p => p.HitTest(localPoint));
    }

    public void ApplyGlobalScale(double scale)
    {
        foreach (var pet in _pets)
        {
            pet.Transform.Scale = scale;
            pet.UpdateFloor();
            pet.SyncVisualTransform();
        }
    }

    public void SetShowPetNames(bool show)
    {
        foreach (var pet in _pets)
        {
            pet.SetShowName(show);
        }
    }
}
