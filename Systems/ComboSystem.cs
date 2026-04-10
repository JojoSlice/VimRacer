using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace VimRacer;

public enum ComboResult { None, Correct, Wrong, Timeout }

public sealed class ComboSystem
{
    private static readonly Keys[] ComboKeys = [Keys.A, Keys.S, Keys.D, Keys.F];

    private static readonly int[] Lengths    = [3, 3, 4, 4, 5, 5, 6, 6, 7, 7];
    private static readonly float[] Limits   = [3.0f, 2.7f, 2.4f, 2.1f, 1.8f, 1.5f, 1.2f, 0.9f, 0.6f, 0.4f];

    private readonly Random _rng = new();

    private Keys[] _combo = [];
    private int _progress;
    private float _timeLeft;
    private int _streak;

    public Keys[] Combo     => _combo;
    public int    Progress  => _progress;
    public float  TimeLeft  => _timeLeft;
    public float  TimeLimit { get; private set; }
    public int    Streak    => _streak;
    public bool   HasBullet { get; private set; }

    public void UseBullet() => HasBullet = false;
    public void ResetStreak() => _streak = 0;

    public void GenerateCombo(int maxSpeedLevel)
    {
        int level = Math.Clamp(maxSpeedLevel, 1, 10) - 1;
        int len   = Lengths[level];
        TimeLimit = Limits[level];
        _timeLeft = TimeLimit;
        _progress = 0;

        _combo = new Keys[len];
        for (int i = 0; i < len; i++)
            _combo[i] = ComboKeys[_rng.Next(ComboKeys.Length)];
    }

    public ComboResult Update(GameTime gameTime, int maxSpeedLevel)
    {
        _timeLeft -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_timeLeft <= 0f)
        {
            _streak = 0;
            GenerateCombo(maxSpeedLevel);
            return ComboResult.Timeout;
        }

        Keys? pressed = PressedComboKey();
        if (pressed is null)
            return ComboResult.None;

        if (pressed == _combo[_progress])
        {
            _progress++;
            if (_progress < _combo.Length)
                return ComboResult.None;

            // Combo complete
            _streak++;
            if (_streak >= 5 && !HasBullet)
            {
                HasBullet = true;
                _streak = 0;
            }
            GenerateCombo(maxSpeedLevel + 1);
            return ComboResult.Correct;
        }
        else
        {
            _streak = 0;
            GenerateCombo(maxSpeedLevel);
            return ComboResult.Wrong;
        }
    }

    private static Keys? PressedComboKey()
    {
        foreach (var k in ComboKeys)
            if (InputSystem.WasPressed(k)) return k;
        return null;
    }
}
