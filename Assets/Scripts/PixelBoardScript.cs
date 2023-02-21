using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class PixelBoardScript : MonoBehaviour
{
    [SerializeField]
    private TextAsset _puzzles;

    private int _id = ++_idc;
    private static int _idc;

    private int _state;
    private string _puzzle;

    private PixelScript[] _pixels;
    private Color[] _baseState = Enumerable.Repeat(Color.black, 36).ToArray();
    private string _tf;
    private int _x, _y, _gx, _gy, _mz;
    private bool _isSolved;

    private static readonly List<List<bool[]>> _mazeWalls = new List<List<bool[]>>(9)
    {
        new List<bool[]>(2) { "  #  ## #  ## #  ##  # #  # ## # # #".Select(c => c == '#').ToArray(), "     ## #### # # # # # ########    #".Select(c => c == '#').ToArray() },
        new List<bool[]>(2) { "  #  # # # ## #  # # ###### ### #  #".Select(c => c == '#').ToArray(), "#    # # # ## #  # # # # ## ###    #".Select(c => c == '#').ToArray() },
        new List<bool[]>(2) { "  ## #### ## ## ######### ####   # #".Select(c => c == '#').ToArray(), " #   ##   ##    ## #   # #   #     #".Select(c => c == '#').ToArray() },
        new List<bool[]>(2) { " #   ###   ## # ###    #    ##  # ##".Select(c => c == '#').ToArray(), "     #  ##### ###### ####### #     #".Select(c => c == '#').ToArray() },
        new List<bool[]>(2) { "     #    ## # # ##  ####   ###    #".Select(c => c == '#').ToArray(), "#    ### # ######## # ## # ### #   #".Select(c => c == '#').ToArray() },
        new List<bool[]>(2) { "# #  #### ## ### # # ### ### #   # #".Select(c => c == '#').ToArray(), "   # #  # ##  # ###    # #  ##  #  #".Select(c => c == '#').ToArray() },
        new List<bool[]>(2) { "   # ## # ## # # # #  ####  ##     #".Select(c => c == '#').ToArray(), "  #  ## # ####  ## ##### # # #  #  #".Select(c => c == '#').ToArray() },
        new List<bool[]>(2) { "#  # #  # ###   ### #  ###   #     #".Select(c => c == '#').ToArray(), "     # # # #### ## ##### # ###   ###".Select(c => c == '#').ToArray() },
        new List<bool[]>(2) { "#    ### ###  # #### # #### ## # # #".Select(c => c == '#').ToArray(), "     #  #  ## #  ### # #  ## #    ##".Select(c => c == '#').ToArray() }
    };

    private void Start()
    {
        KMSelectable[] btns = GetComponent<KMSelectable>().Children;
        _pixels = btns.Select(s => s.GetComponent<PixelScript>()).ToArray();
        for(int i = 0; i < 36; ++i)
        {
            int j = i;
            btns[j].OnInteract += () => { Press(j); return false; };
        }
        _puzzle = _puzzles.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).PickRandom();

        _tf = new string[] { "N", "S", "E", "W" }.PickRandom() + new string[] { "N", "S", "E", "W" }.PickRandom();

        _y = int.Parse(_puzzle[10].ToString());
        _x = int.Parse(_puzzle[8].ToString());
        int m = int.Parse(_puzzle[12].ToString());

        int mx = m % 3;
        int my = m / 3;
        foreach(char d in _tf)
        {
            switch(d)
            {
                case 'N':
                    my += 2;
                    break;
                case 'S':
                    ++my;
                    break;
                case 'E':
                    ++mx;
                    break;
                case 'W':
                    mx += 2;
                    break;
            }
        }
        mx %= 3;
        my %= 3;
        _mz = 3 * my + mx;

        _gy = Enumerable.Range(0, 6).Where(y => Math.Abs(y - _y) > 1).PickRandom();
        _gx = Enumerable.Range(0, 6).Where(y => Math.Abs(y - _x) > 1).PickRandom();

        Log("First shown sequence and correct first press: " + _puzzle);
        Log("Second shown sequence and goal location: " + _tf + " " + _gx + "," + _gy);
    }

    private void Press(int ix)
    {
        switch(_state)
        {
            case 0:
                StartCoroutine(PlaySequence(1));
                break;
            case 1:
                int correct = int.Parse(_puzzle[10].ToString()) * 6 + int.Parse(_puzzle[8].ToString());
                if(ix == correct)
                {
                    Log("Button " + ix + " correctly pressed for stage 1.");
                    _baseState = Enumerable.Repeat(Color.black, 36).ToArray();
                    _baseState[ix] = Color.white;
                    _baseState[6 * _gy + _gx] = Color.red;
                    StartCoroutine(ShowTransform(true));
                }
                else
                {
                    Log("Button " + ix + " incorrectly pressed for stage 1.");
                    _baseState[ix] = Color.red;
                    StartCoroutine(Strike(1));
                }
                break;
            case 2:
                if((ix % 6 == _x - 1 || ix % 6 == _x + 1) && (ix / 6 == _y - 1 || ix / 6 == _y + 1))
                    goto badpos;

                if(ix % 6 == _x - 1)
                {
                    if(_mazeWalls[_mz][0][_y * 6 + _x - 1])
                        goto wall;
                    else
                    {
                        _pixels[6 * _y + _x].Color = Color.black;
                        --_x;
                        _pixels[6 * _y + _x].Color = Color.white;
                    }
                }
                else if(ix % 6 == _x + 1)
                {
                    if(_mazeWalls[_mz][0][_y * 6 + _x])
                        goto wall;
                    else
                    {
                        _pixels[6 * _y + _x].Color = Color.black;
                        ++_x;
                        _pixels[6 * _y + _x].Color = Color.white;
                    }
                }
                else if(ix / 6 == _y - 1)
                {
                    if(_mazeWalls[_mz][1][_y + 6 * _x - 1])
                        goto wall;
                    else
                    {
                        _pixels[6 * _y + _x].Color = Color.black;
                        --_y;
                        _pixels[6 * _y + _x].Color = Color.white;
                    }
                }
                else if(ix / 6 == _y + 1)
                {
                    if(_mazeWalls[_mz][1][_y + 6 * _x])
                        goto wall;
                    else
                    {
                        _pixels[6 * _y + _x].Color = Color.black;
                        ++_y;
                        _pixels[6 * _y + _x].Color = Color.white;
                    }
                }
                else if(ix != 6 * _y + _x)
                    goto badpos;

                Log("Button " + ix + " pressed for stage 2.");

                if(_y == _gy && _x == _gx)
                    StartCoroutine(Solve());
                break;
                badpos:
                Log("Button " + ix + " pressed for stage 2. That's not a possible move. Position reset.");
                StartCoroutine(Strike(2));
                break;
                wall:
                Log("Button " + ix + " pressed for stage 2. There's a wall there. Position reset.");
                StartCoroutine(Strike(2));
                break;
        }
    }

    private IEnumerator Solve()
    {
        _state = -1;

        Log("Module solved.");

        GetComponent<KMAudio>().PlaySoundAtTransform("Solve", transform);

        foreach(PixelScript p in _pixels)
            p.Color = Color.black;

        int[] shape = "432234321123210012210012321123432234".Select(c => int.Parse(c.ToString())).ToArray();

        for(int i = 0; i < 5; ++i)
        {
            for(int px = 0; px < 36; ++px)
            {
                if(shape[px] <= i)
                    _pixels[px].Color = Color.green;
            }

            yield return new WaitForSeconds(0.06f);
        }
        GetComponent<KMBombModule>().HandlePass();
        _isSolved = true;
    }

    private IEnumerator ShowTransform(bool first = false)
    {
        _state = -1;

        if(first)
        {
            GetComponent<KMAudio>().PlaySoundAtTransform("Correct", transform);
            yield return new WaitForSeconds(1.5f);
        }

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(Flash(_tf[0]));

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(Flash(_tf[1]));

        _y = int.Parse(_puzzle[10].ToString());
        _x = int.Parse(_puzzle[8].ToString());

        for(int i = 0; i < 36; ++i)
            _pixels[i].Color = _baseState[i];

        _state = 2;
    }

    private IEnumerator Strike(int stateToEnter)
    {
        _state = -1;

        GetComponent<KMBombModule>().HandleStrike();

        foreach(PixelScript p in _pixels)
            p.Color = Color.red;

        yield return new WaitForSeconds(1f);

        foreach(PixelScript p in _pixels)
            p.Color = Color.black;

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(PlaySequence(stateToEnter));

        if(stateToEnter != 2)
            yield break;

        foreach(PixelScript p in _pixels)
            p.Color = Color.black;

        _state = -1;
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(ShowTransform());
    }

    private IEnumerator PlaySequence(int stateToEnter)
    {
        _state = -1;

        for(int i = 0; i < 7; ++i)
        {
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(Flash(_puzzle[i]));
        }

        for(int i = 0; i < 36; ++i)
            _pixels[i].Color = _baseState[i];

        _state = stateToEnter;
    }

    private IEnumerator Flash(char v)
    {
        foreach(PixelScript p in _pixels)
            p.Color = Color.black;

        GetComponent<KMAudio>().PlaySoundAtTransform("Woosh", transform);

        Func<int, int> shapeFunc = i => i;

        switch(v)
        {
            case 'N':
                shapeFunc = i => 5 - (i / 6) + ((i % 6 == 1 || i % 6 == 4) ? 1 : (i % 6 == 0 || i % 6 == 5) ? 2 : 0);
                break;
            case 'S':
                shapeFunc = i => (i / 6) + ((i % 6 == 1 || i % 6 == 4) ? 1 : (i % 6 == 0 || i % 6 == 5) ? 2 : 0);
                break;
            case 'E':
                shapeFunc = i => (i % 6) + ((i / 6 == 1 || i / 6 == 4) ? 1 : (i / 6 == 0 || i / 6 == 5) ? 2 : 0);
                break;
            case 'W':
                shapeFunc = i => 5 - (i % 6) + ((i / 6 == 1 || i / 6 == 4) ? 1 : (i / 6 == 0 || i / 6 == 5) ? 2 : 0);
                break;
        }

        int[] shape = Enumerable.Range(0, 36).Select(i => shapeFunc(i)).ToArray();

        for(int i = 0; i < 8; ++i)
        {
            for(int px = 0; px < 36; ++px)
            {
                if(shape[px] == i)
                    _pixels[px].Color = Color.white;
                else if(shape[px] == i - 1)
                    _pixels[px].Color = Color.gray;
                else
                    _pixels[px].Color = Color.black;
            }

            yield return new WaitForSeconds(0.06f);
        }

        foreach(PixelScript p in _pixels)
            p.Color = Color.black;
    }

    private void Log(string msg)
    {
        Debug.Log("[Pixel Board #" + _id + "] " + msg);
    }

#pragma warning disable 414
    private const string TwitchHelpMessage = @"!{0} A1 B1 C1...";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToLowerInvariant();
        Match m = Regex.Match(command, @"[a-f][1-6](?:\s+[a-f][1-6])*");

        if(m.Success)
        {
            yield return null;
            while((m = Regex.Match(command, @"^\s*([a-f])([1-6])\s*")).Success)
            {
                int ix = "abcdef".IndexOf(m.Groups[1].Value) + 6 * int.Parse(m.Groups[2].Value) - 6;
                _pixels[ix].GetComponent<KMSelectable>().OnInteract();
                command = command.Substring(m.Index + m.Length);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }


    private class Node
    {
        public int X, Y;
        public Node Prev;

        public Node(int x, int y, Node prev)
        {
            X = x;
            Y = y;
            Prev = prev;
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while(_state == -1)
            yield return true;
        if(_state == 0)
        {
            _pixels[0].GetComponent<KMSelectable>().OnInteract();
            while(_state == -1)
                yield return true;
        }
        if(_state == 1)
        {
            _pixels[int.Parse(_puzzle[10].ToString()) * 6 + int.Parse(_puzzle[8].ToString())].GetComponent<KMSelectable>().OnInteract();
            while(_state == -1)
                yield return true;
        }
        if(_state == 2)
        {
            List<Node> visited = new List<Node>();
            Queue<Node> toCheck = new Queue<Node>();
            toCheck.Enqueue(new Node(_x, _y, null));
            Debug.Log(_y + "" + _x);
            Node solution;
            while(true)
            {
                if(toCheck.Count == 0)
                    throw new Exception("No solution path found!");

                Node node = toCheck.Dequeue();
                visited.Add(node);
                if(node.X - 1 != -1 && !_mazeWalls[_mz][0][node.Y * 6 + node.X - 1] && !visited.Any(n => node.X - 1 == n.X && node.Y == n.Y))
                {
                    toCheck.Enqueue(new Node(node.X - 1, node.Y, node));
                    visited.Add(new Node(node.X - 1, node.Y, node));
                }
                if(node.X + 1 != 6 && !_mazeWalls[_mz][0][node.Y * 6 + node.X] && !visited.Any(n => node.X + 1 == n.X && node.Y == n.Y))
                {
                    toCheck.Enqueue(new Node(node.X + 1, node.Y, node));
                    visited.Add(new Node(node.X + 1, node.Y, node));
                }
                if(node.Y - 1 != -1 && !_mazeWalls[_mz][1][node.Y + 6 * node.X - 1] && !visited.Any(n => node.X == n.X && node.Y - 1 == n.Y))
                {
                    toCheck.Enqueue(new Node(node.X, node.Y - 1, node));
                    visited.Add(new Node(node.X, node.Y - 1, node));
                }
                if(node.Y + 1 != 6 && !_mazeWalls[_mz][1][node.Y + 6 * node.X] && !visited.Any(n => node.X == n.X && node.Y + 1 == n.Y))
                {
                    toCheck.Enqueue(new Node(node.X, node.Y + 1, node));
                    visited.Add(new Node(node.X, node.Y + 1, node));
                }

                solution = toCheck.FirstOrDefault(n => n.X == _gx && n.Y == _gy);
                if(solution != null)
                    break;
            }

            Stack<int> presses = new Stack<int>();

            while(solution != null)
            {
                presses.Push(solution.X + 6 * solution.Y);
                solution = solution.Prev;
            }

            while(presses.Count > 0)
            {
                _pixels[presses.Pop()].GetComponent<KMSelectable>().OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            while(!_isSolved)
                yield return true;
        }
        yield break;
    }
}
