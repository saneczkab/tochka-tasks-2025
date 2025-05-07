using System;
using System.Collections.Generic;
using System.Linq;

namespace tochka_tasks_2025;

public class Program
{
    // Константы для символов ключей и дверей
    static readonly char[] keys_char = Enumerable.Range('a', 26).Select(i => (char)i).ToArray();
    static readonly char[] doors_char = keys_char.Select(char.ToUpper).ToArray();

    private static readonly (int dx, int dy)[] Directions = { (-1,0), (1,0), (0,-1), (0,1) };
    
    // Метод для чтения входных данных
    static List<List<char>> GetInput()
    {
        var data = new List<List<char>>();
        string line;
        while ((line = Console.ReadLine()) != null && line != "")
        {
            data.Add(line.ToCharArray().ToList());
        }
        return data;
    }
    
    private static int Solve(List<List<char>> data)
    {
        var robots = new List<(int, int)>();
        var keys = new List<(int, int)>();
        var keyChars = new List<char>();
        var indexes = new Dictionary<char, int>();
        ProcessPositions(data, robots, keys, keyChars, indexes);
        
        var graph = BuildGraph(data, robots, keys, indexes);
        var keyBits = keyChars
            .Select(c => 1 << Array.IndexOf(keys_char, c))
            .ToArray();
        var targetMask = keyBits.Aggregate(0, (current, b) => current | b);
        var robotIdxs = Enumerable.Range(0, robots.Count).ToArray();
        
        return FindMinStepsDijkstra(graph, robotIdxs, keyBits, targetMask);
    }
    
    private static void ProcessPositions(List<List<char>> data, List<(int, int)> robots, 
        List<(int, int)> keys, List<char> keyChars, Dictionary<char, int> indexes)
    {
        for (var i = 0; i < data.Count; i++)
        {
            for (var j = 0; j < data[i].Count; j++)
            {
                var c = data[i][j];
                if (c == '@')
                {
                    robots.Add((j, i));
                    data[i][j] = '.';
                }
                else if (keys_char.Contains(c))
                {
                    indexes[c] = indexes.Count;
                    keys.Add((j, i));
                    keyChars.Add(c);
                }
            }
        }
    }
    
    private static Graph BuildGraph(List<List<char>> data, List<(int, int)> robots, 
        List<(int, int)> keys, Dictionary<char, int> indexes)
    {
        var nodeCount = robots.Count + keys.Count;
        var nodeCoords = new (int x, int y)[nodeCount];
        for (var i = 0; i < robots.Count; i++)
        {
            nodeCoords[i] = robots[i];
        }
        for (var i = 0; i < keys.Count; i++)
        {
            nodeCoords[i + robots.Count] = keys[i];
        }
        
        var graph = new Graph();
        for (var i = 0; i < nodeCount; i++)
        {
            var keysData = GetKeysData(data, nodeCoords[i]);
            foreach (var keyData in keysData)
            {
                var dest = indexes[keyData.Key] + robots.Count;
                var (stepCount, keysNeededMask) = keyData.Value;
                graph.AddEdge(i, dest, stepCount, keysNeededMask);
            }
        }

        return graph;
    }

    private static Dictionary<char, (int stepCount, int keysNeededMask)> GetKeysData(
        List<List<char>> data, (int x, int y) startCoord)
    {
        var queue = new Queue<(int x, int y, int stepCount, int keysNeededMask)>();
        var visited = new HashSet<(int x, int y, int keysNeededMask)>();
        queue.Enqueue((startCoord.x, startCoord.y, 0, 0));
        return RunBFS(data, queue, visited);
    }

    private static Dictionary<char, (int stepCount, int keysNeededMask)> RunBFS(
        List<List<char>> data, Queue<(int x, int y, int stepCount, int keysNeededMask)> queue,
        HashSet<(int x, int y, int keysNeededMask)> visited)
    {
        var result = new Dictionary<char, (int stepCount, int keysNeededMask)>();

        while (queue.Count > 0)
        {
            var (x, y, steps, mask) = queue.Dequeue();
            if (!visited.Add((x, y, mask)))
            {
                continue;
            }

            var c = data[y][x];
            if (keys_char.Contains(c) && !result.ContainsKey(c))
            {
                result[c] = (steps, mask);
            }
            
            ProcessNeighbors(data, x, y, steps, mask, queue);
        }

        return result;
    }

    private static void ProcessNeighbors(List<List<char>> data, int x, int y, int steps, int mask,
        Queue<(int x, int y, int stepCount, int keysNeededMask)> queue)
    {
        foreach (var (dx, dy) in Directions)
        {
            var newX = x + dx;
            var newY = y + dy;
            if (newY < 0 || newY >= data.Count || newX < 0 || newX >= data[newY].Count)
            {
                // в открытых тестах лабиринт ограничен стенами, и выход за границы не должен происходить
                // но на всякий случай пусть будет проверка
                continue;
            }

            var newPoint = data[newY][newX];
            if (newPoint == '#')
            {
                continue;
            }

            var newMask = mask;
            if (doors_char.Contains(newPoint))
            {
                // значительно ускорило решение
                newMask |= 1 << Array.IndexOf(doors_char, newPoint);
            }

            queue.Enqueue((newX, newY, steps + 1, newMask));
        }
    }
    
    private static int FindMinStepsDijkstra(Graph graph, int[] robotsIdxs, int[] keyBits, int targetMask)
    {
        var startState = new State(
            robotsIdxs[0], robotsIdxs[1], robotsIdxs[2], robotsIdxs[3], 0);
        var queue = new PriorityQueue<State, int>();
        var minSteps = new Dictionary<State, int>();
        queue.Enqueue(startState, 0);
        minSteps[startState] = 0;

        while (queue.TryDequeue(out var state, out var steps))
        {
            if (steps > minSteps[state])
            {
                continue;
            }
            if (state.Keys == targetMask)
            {
                return steps;
            }
            ProcessState(graph, state, steps, queue, minSteps, robotsIdxs.Length, keyBits);
        }

        return -1;
    }
    
    private static void ProcessState(Graph graph, State state, int steps, PriorityQueue<State, int> queue, 
        Dictionary<State, int> minSteps, int robotCount, int[] keyBits)
    {
        for (var i = 0; i < robotCount; i++)
        {
            foreach (var edge in graph.GetEdges(state[i]))
            {
                var keyIdx = edge.To - robotCount;
                var keyBit = keyBits[keyIdx];
                if ((state.Keys & keyBit) != 0 || (edge.RequiredKeys & ~state.Keys) != 0)
                {
                    continue;
                }
                
                var newState = i switch
                {
                    0 => state with { Pos1 = edge.To, Keys = state.Keys | keyBit },
                    1 => state with { Pos2 = edge.To, Keys = state.Keys | keyBit },
                    2 => state with { Pos3 = edge.To, Keys = state.Keys | keyBit },
                    _ => state with { Pos4 = edge.To, Keys = state.Keys | keyBit }
                };
                
                var newSteps = steps + edge.Weight;
                if (minSteps.TryGetValue(newState, out var b) && newSteps >= b)
                {
                    continue;
                }
                
                minSteps[newState] = newSteps;
                queue.Enqueue(newState, newSteps);
            }
        }
    }

    public static void Main()
    {
        var data = GetInput();
        int result = Solve(data);
        
        if (result == -1)
        {
            Console.WriteLine("No solution found");
        }
        else
        {
            Console.WriteLine(result);
        }
    }
}

public class Graph
{
    private readonly Dictionary<int, List<Edge>> _adjacency = new();

    public void AddEdge(int from, int to, int distance, int requiredKeys)
    {
        if (!_adjacency.ContainsKey(from))
            _adjacency[from] = new List<Edge>();
        
        _adjacency[from].Add(new Edge(to, distance, requiredKeys));
    }

    public List<Edge> GetEdges(int from) => _adjacency.TryGetValue(from, out var list) ? list : new List<Edge>();
}

public class Edge
{
    public int To { get; }
    public int Weight { get; }
    public int RequiredKeys { get; }
    
    public Edge(int to, int weight, int requiredKeys)
    {
        To = to;
        Weight = weight;
        RequiredKeys = requiredKeys;
    }
}

public readonly record struct State
{
    public int Pos1 { get; init; }
    public int Pos2 { get; init; }
    public int Pos3 { get; init; }
    public int Pos4 { get; init; }
    public int Keys { get; init; }
    
    public State(int Pos1, int Pos2, int Pos3, int Pos4, int Keys)
    {
        this.Pos1 = Pos1;
        this.Pos2 = Pos2;
        this.Pos3 = Pos3;
        this.Pos4 = Pos4;
        this.Keys = Keys;
    }
    
    public int[] ToArray() => new[] { Pos1, Pos2, Pos3, Pos4, Keys };
    
    public int this[int index] =>
        index switch
        {
            0 => Pos1,
            1 => Pos2,
            2 => Pos3,
            3 => Pos4,
            _ => throw new IndexOutOfRangeException()
        };
}