// @since 22
using System;
using Microsoft.Xna.Framework;
using MonsterMaze.Mazes;

namespace MonsterMaze.Gameplay;

/// <summary>
/// Keeps score. Players earn points for exploring, for surviving while Rex is on their trail,
/// for narrow escapes and, above all, for getting out. Everything is multiplied by the
/// difficulty level's multiplier.
/// </summary>
public sealed class ScoreKeeper
{
    public const int StepPoints = 2;
    public const int NewCellPoints = 10;
    public const int SurvivalPointsPerSecond = 5;
    public const int NearMissPoints = 250;
    public const int EscapePoints = 2000;
    public const int MaxTimeBonus = 3000;
    public const int TimeBonusLostPerSecond = 10;

    private readonly Maze _maze;
    private readonly bool[] _visited;
    private float _survivalPoints;
    private bool _nearMissArmed;

    public ScoreKeeper(Maze maze, int multiplier)
    {
        _maze = maze;
        _visited = new bool[maze.Width * maze.Height];
        Multiplier = multiplier;
        _visited[maze.Start.Y * maze.Width + maze.Start.X] = true;
        CellsExplored = 1;
    }

    public int Score { get; private set; }
    public int Multiplier { get; }
    public int Steps { get; private set; }
    public int CellsExplored { get; private set; }
    public int NearMisses { get; private set; }

    /// <summary>Raised for special awards so the HUD can celebrate them.</summary>
    public event Action<int, string> Awarded;

    public void OnStep(Point cell)
    {
        Steps++;
        Add(StepPoints);

        if (!_maze.InBounds(cell))
            return;

        int index = cell.Y * _maze.Width + cell.X;
        if (!_visited[index])
        {
            _visited[index] = true;
            CellsExplored++;
            Add(NewCellPoints);
        }
    }

    /// <param name="underPressure">True while Rex is stalking or chasing the player.</param>
    /// <param name="stepsFromRex">The walking distance between Rex and the player.</param>
    public void Update(float deltaSeconds, bool underPressure, int stepsFromRex)
    {
        if (underPressure)
        {
            _survivalPoints += deltaSeconds * SurvivalPointsPerSecond;
            int whole = (int)_survivalPoints;
            if (whole > 0)
            {
                _survivalPoints -= whole;
                Add(whole);
            }
        }

        // A near miss: Rex gets within a step of the player, and the player gets away.
        if (stepsFromRex <= 1)
        {
            _nearMissArmed = true;
        }
        else if (_nearMissArmed && stepsFromRex >= 4)
        {
            _nearMissArmed = false;
            NearMisses++;
            Add(NearMissPoints, "NEAR MISS!");
        }
    }

    /// <summary>Adds the escape bonus, plus a bonus for escaping quickly. Returns both amounts.</summary>
    public (int EscapeBonus, int TimeBonus) AwardEscape(float seconds)
    {
        int escape = EscapePoints * Multiplier;
        int time = Math.Max(0, MaxTimeBonus - (int)(seconds * TimeBonusLostPerSecond)) * Multiplier;
        Score += escape + time;
        return (escape, time);
    }

    private void Add(int points, string reason = null)
    {
        int total = points * Multiplier;
        Score += total;
        if (reason != null)
            Awarded?.Invoke(total, reason);
    }
}
