using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlappyBirb;

using Void.Engine.Helpers;
using Void.Engine.Pathfinding;

public class PathfinderTest
{
    private const int MapWidth = 10;
    private const int MapHeight = 10;
    private AStar2D _astar;

    public void Run()
    {
        Console.WriteLine("=== AStar2D Pathfinding Test ===\n");

        TestBasicPath();
        TestWeightedPath();
        TestObstacles();
        TestDiagonalModes();
        TestFlowField();
        TestBFS();
        TestGreedyBestFirst();
    }

    private void SetupGrid()
    {
        _astar = new AStar2D(MapWidth * MapHeight);

        // Add grid points
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                int id = ToId(x, y);
                _astar.AddPoint(id, new Vect2(x, y));
            }
        }

        // Connect orthogonal neighbors
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                int id = ToId(x, y);

                if (x < MapWidth - 1)
                    _astar.ConnectPoints(id, ToId(x + 1, y));
                if (y < MapHeight - 1)
                    _astar.ConnectPoints(id, ToId(x, y + 1));
            }
        }

        // Connect diagonal neighbors (for 8-directional movement)
        for (int y = 0; y < MapHeight - 1; y++)
        {
            for (int x = 0; x < MapWidth - 1; x++)
            {
                int id = ToId(x, y);
                _astar.ConnectPoints(id, ToId(x + 1, y + 1));
                _astar.ConnectPoints(ToId(x + 1, y), ToId(x, y + 1));
            }
        }
    }

    private void TestBasicPath()
    {
        Console.WriteLine("--- Basic A* Path ---");
        SetupGrid();

        int start = ToId(0, 0);
        int end = ToId(9, 9);

        var path = _astar.GetIdPath(start, end);

        Console.WriteLine($"Path from (0,0) to (9,9):");
        PrintPath(path);
        Console.WriteLine();
    }

    private void TestWeightedPath()
    {
        Console.WriteLine("--- Weighted Path ---");
        SetupGrid();

        // Make a "swamp" in the middle - higher weight = more expensive
        for (int y = 3; y < 7; y++)
        {
            for (int x = 3; x < 7; x++)
            {
                _astar.SetPointWeightScale(ToId(x, y), 10f);
            }
        }

        int start = ToId(0, 5);
        int end = ToId(9, 5);

        var path = _astar.GetIdPath(start, end);

        Console.WriteLine($"Path from (0,5) to (9,5) avoiding swamp:");
        Console.WriteLine("Swamp area: x=3-6, y=3-6 (weight 10)");
        PrintPath(path);
        Console.WriteLine();
    }

    private void TestObstacles()
    {
        Console.WriteLine("--- Path Around Obstacles ---");
        SetupGrid();

        // Create a wall
        for (int y = 2; y < 8; y++)
        {
            _astar.SetPointDisabled(ToId(5, y));
        }

        int start = ToId(0, 5);
        int end = ToId(9, 5);

        var path = _astar.GetIdPath(start, end);

        Console.WriteLine($"Path from (0,5) to (9,5) around wall at x=5:");
        PrintPath(path);
        Console.WriteLine();
    }

    private void TestDiagonalModes()
    {
        Console.WriteLine("--- Diagonal Modes ---");

        // Always
        SetupGrid();
        _astar.DefaultDiagonalMode = DiagonalMode.Always;
        var path1 = _astar.GetIdPath(ToId(0, 0), ToId(5, 5));
        Console.WriteLine($"Always diagonal path length: {path1.Count} points");

        // Never
        SetupGrid();
        _astar.DefaultDiagonalMode = DiagonalMode.Never;
        var path2 = _astar.GetIdPath(ToId(0, 0), ToId(5, 5));
        Console.WriteLine($"Never diagonal path length: {path2.Count} points");

        // AtLeastOneWalkable - create diagonal wall
        SetupGrid();
        _astar.DefaultDiagonalMode = DiagonalMode.AtLeastOneWalkable;
        _astar.SetPointDisabled(ToId(3, 2));
        _astar.SetPointDisabled(ToId(2, 3));
        var path3 = _astar.GetIdPath(ToId(2, 2), ToId(3, 3));
        Console.WriteLine($"AtLeastOneWalkable path: {path3.Count} points");

        Console.WriteLine();
    }

    private void TestFlowField()
    {
        Console.WriteLine("--- Flow Field ---");
        SetupGrid();

        int target = ToId(5, 5);
        var flowField = _astar.ComputeFlowField(target);

        Console.WriteLine($"Flow field to target (5,5):");
        Console.WriteLine($"Total nodes in field: {flowField.Count}");

        // Show directions for a few sample points
        int[] samplePoints = { ToId(0, 0), ToId(0, 5), ToId(5, 0), ToId(9, 9) };
        foreach (var pointId in samplePoints)
        {
            var next = flowField.GetNextNode(pointId);
            var dir = flowField.GetDirection(pointId);
            var pos = _astar.GetPointPosition(pointId);
            Console.WriteLine($"  From ({pos.X},{pos.Y}) → next: {next}, direction: ({dir.X:F1},{dir.Y:F1})");
        }

        Console.WriteLine();
    }

    private void TestBFS()
    {
        Console.WriteLine("--- BFS Algorithm ---");
        SetupGrid();

        int start = ToId(0, 0);
        int end = ToId(9, 9);

        var path = _astar.GetIdPath(start, end, algorithm: PathAlgorithm.BFS);

        Console.WriteLine($"BFS path from (0,0) to (9,9):");
        PrintPath(path);
        Console.WriteLine($"Path length: {path.Count} points");
        Console.WriteLine();
    }

    private void TestGreedyBestFirst()
    {
        Console.WriteLine("--- Greedy Best-First Algorithm ---");
        SetupGrid();

        int start = ToId(0, 0);
        int end = ToId(9, 9);

        var path = _astar.GetIdPath(start, end, algorithm: PathAlgorithm.GreedyBestFirst);

        Console.WriteLine($"Greedy Best-First path from (0,0) to (9,9):");
        PrintPath(path);
        Console.WriteLine($"Path length: {path.Count} points");
        Console.WriteLine();
    }

    private int ToId(int x, int y) => y * MapWidth + x;

    private void PrintPath(List<int> path)
    {
        if (path.Count == 0)
        {
            Console.WriteLine("  No path found!");
            return;
        }

        // After SetupGrid, add:
        Console.WriteLine($"Point count: {_astar.PointCount}");
        Console.WriteLine($"Has point 0: {_astar.HasPoint(0)}");
        Console.WriteLine($"Has point 99: {_astar.HasPoint(99)}");
        Console.WriteLine($"Connections from 0: {_astar.GetPointConnections(0).Count}");
        Console.WriteLine($"Connections from 45: {_astar.GetPointConnections(45).Count}");

        Console.Write("  ");
        for (int i = 0; i < path.Count; i++)
        {
            var pos = _astar.GetPointPosition(path[i]);
            Console.Write($"({pos.X:F0},{pos.Y:F0})");
            if (i < path.Count - 1)
                Console.Write(" → ");
        }
        Console.WriteLine();
    }
}
