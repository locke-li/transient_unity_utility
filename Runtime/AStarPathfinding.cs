﻿//#define DebugPathfinding

using System;
using System.Text;
using System.Collections.Generic;
using Transient.Container;
using UnityEngine;

namespace Transient.Pathfinding {
    public interface IGraphData {
        int Size { get; }
        int InaccessibleMask { get; }
        Vector3 Position(int index_);
        float Cost(int index_);
        StringBuilder PrintNode(int index_, StringBuilder text_);
    }

    public interface IGraph {
        IGraphData data { get; }
        IEnumerable<(int, float)> EnumerateLink(int index_);
        float HeuristicCost(int a_, int b_);
    }
}

#region grid based

namespace Transient.Pathfinding {

    public struct Grid {
        //virtual position when origin of the grid field is in top-left corner
        public ushort x;
        public ushort y;
        public byte v;//path finding related cost value
    }

    public class GridData : IGraphData {
        public Grid[] field;
        public int Size => field.Length;
        //a size limit of 65534x65534 [1,65534] should be reasonable for grid based data
        public ushort width;
        public ushort height;
        //origin point coordinates in a system where the origin is in the top-left corner
        public ushort originX;
        public ushort originY;
        public Vector3 origin;
        public Vector3 gridX;
        public Vector3 gridY;
        public int InaccessibleMask { get; set; } = byte.MaxValue;

        public void Init(ushort w_, ushort h_, ushort originX_ = 0, ushort originY_ = 0) {
            field = new Grid[w_ * h_];
            width = w_;
            height = h_;
            originX = originX_;
            originY = originY_;
        }

        public void InitCoordSystem(Vector3 gridX_, Vector3 gridY_, Vector3 origin_) {
            gridX = gridX_;
            gridY = gridY_;
            origin = origin_;
        }

        public void Fill(byte[] v_) {
            for (ushort x = 0; x < width; ++x) {
                for (ushort y = 0; y < height; ++y) {
                    var i = x + y * width;
                    field[i] = new Grid() {
                        x = x,
                        y = y,
                        v = v_[i]
                    };
                }
            }
        }

        public void Fill(byte v_ = 0) {
            for (ushort x = 0; x < width; ++x) {
                for (ushort y = 0; y < height; ++y) {
                    var i = x + y * width;
                    field[i] = new Grid() {
                        x = x,
                        y = y,
                        v = v_
                    };
                }
            }
        }

        public void Change(short x_, short y_, byte v_) {
            field[Coord2Index(x_, y_)].v = v_;
        }

        public int Coord2Index(short x, short y) {
            return (x + originX) + (y + originY) * width;
        }

        public Vector3 Position(int index_) {
            ref var grid = ref field[index_];
            return origin + (grid.x - originX + 0.5f) * gridX + (grid.y - originY + 0.5f) * gridY;
        }

        public float Cost(int index_) => field[index_].v;

        public StringBuilder PrintNode(int index_, StringBuilder text_) {
            text_.Append(index_ % width);
            text_.Append(",");
            text_.Append(index_ / width);
            return text_;
        }
    }

    public class RectGrid4Dir : IGraph {
        private readonly GridData _data;
        public IGraphData data => _data;

        public RectGrid4Dir(GridData data_) {
            _data = data_;
        }

        public void FindPath(AStarPathfinding astar_, short startX_, short startY_, short goalX_, short goalY_) {
            astar_.FindPath(this, _data.Coord2Index(startX_, startY_), _data.Coord2Index(goalX_, goalY_));
        }

        public IEnumerable<(int, float)> EnumerateLink(int index_) {
            int x = _data.field[index_].x;
            int y = _data.field[index_].y;
            //4 directions
            if (x > 0) yield return (index_ - 1, 1);
            if (y > 0) yield return (index_ - _data.width, 1);
            if (x < _data.width - 1) yield return (index_ + 1, 1);
            if (y < _data.height - 1) yield return (index_ + _data.width, 1);
        }

        public float HeuristicCost(int a_, int b_) {
            var a = _data.field[a_];
            var b = _data.field[b_];
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }
    }

    public class RectGrid8Dir : IGraph {
        private readonly GridData _data;
        public IGraphData data => _data;

        public RectGrid8Dir(GridData data_) {
            _data = data_;
        }

        public void FindPath(AStarPathfinding astar_, short startX_, short startY_, short goalX_, short goalY_)
            => astar_.FindPath(this, _data.Coord2Index(startX_, startY_), _data.Coord2Index(goalX_, goalY_));

        public IEnumerable<(int, float)> EnumerateLink(int index_) {
            int x = _data.field[index_].x;
            int y = _data.field[index_].y;
            //8 directions
            if (x > 0) {
                yield return (index_ - 1, 1);//-x
                if (y > 0) yield return (index_ - 1 - _data.width, 1.4f);//-x-y
                if (y < _data.height - 1) yield return (index_ - 1 + _data.width, 1.4f);//-x+y
            }
            if (y > 0) yield return (index_ - _data.width, 1);//-y
            if (x < _data.width - 1) {
                yield return (index_ + 1, 1);//+x
                if (y > 0) yield return (index_ + 1 - _data.width, 1.4f);//+x-y
                if (y < _data.height - 1) yield return (index_ + 1 + _data.width, 1.4f);//+x+y
            }
            if (y < _data.height - 1) yield return (index_ + _data.width, 1);//+y
        }

        public float HeuristicCost(int a_, int b_) {
            var a = _data.field[a_];
            var b = _data.field[b_];
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }
    }

    public class AxialHexGrid : IGraph {
        private readonly GridData _data;
        public IGraphData data => _data;

        public AxialHexGrid(GridData data_) {
            _data = data_;
        }

        public void FindPath(AStarPathfinding astar_, short startX_, short startY_, short goalX_, short goalY_)
            => astar_.FindPath(this, _data.Coord2Index(startX_, startY_), _data.Coord2Index(goalX_, goalY_));

        public IEnumerable<(int, float)> EnumerateLink(int index_) {
            int x = _data.field[index_].x;
            int y = _data.field[index_].y;
            //6 directions
            if (x > 0) {
                yield return (index_ - 1, 1);//-x
                if (y > 0) yield return (index_ - 1 - _data.width, 1);//-x-y
            }
            if (y > 0) yield return (index_ - _data.width, 1);//-y
            if (x < _data.width - 1) {
                yield return (index_ + 1, 1);//+x
                if (y < _data.height - 1) yield return (index_ + 1 + _data.width, 1);//+x+y
            }
            if (y < _data.height - 1) yield return (index_ + _data.width, 1);//+y
        }

        public float HeuristicCost(int a_, int b_) {
            var a = _data.field[a_];
            var b = _data.field[b_];
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }
    }
}

#endregion grid based

#region waypoint based

namespace Transient.Pathfinding {
    public struct Waypoint {
        public Vector3 position;
        public Quaternion rotation;
        public byte weight;
        public List<int> link;
    }

    public class WaypointData : IGraphData {
        public Waypoint[] waypoint;
        public int Size => waypoint.Length;
        public int InaccessibleMask { get; set; }

        public int Position2Index(Vector3 position, float maxDistanceSqr = float.MaxValue) {
            var min = maxDistanceSqr;
            int ret = -1;
            for (int o = 0; o < waypoint.Length; ++o) {
                var dist = (waypoint[o].position - position).sqrMagnitude;
                if (dist < min) {
                    ret = o;
                    min = dist;
                }
            }
            return ret;
        }

        public void MakeBidirectional() {
            for (int r = 0; r < waypoint.Length; ++r) {
                foreach (var link in waypoint[r].link) {
                    var other = waypoint[link];
                    if (!other.link.Contains(r)) {
                        other.link.Add(r);
                    }
                }
            }
        }

        public Vector3 Position(int index_) {
            ref var node = ref waypoint[index_];
            return node.position;
        }

        public float Cost(int index_) => waypoint[index_].weight;

        public StringBuilder PrintNode(int index_, StringBuilder text_) {
            text_.Append(index_);
            return text_;
        }
    }

    public class WaypointGraph : IGraph {
        private readonly WaypointData _data;
        public IGraphData data => _data;
        public uint HeuristicScale { get; set; } = 100;

        public WaypointGraph(WaypointData data_) {
            _data = data_;
        }

        public IEnumerable<(int, float)> EnumerateLink(int index_) {
            foreach (var link in _data.waypoint[index_].link) {
                yield return (link, 1);//TODO distance
            }
        }

        public float HeuristicCost(int a_, int b_) {
            var a = _data.waypoint[a_].position;
            var b = _data.waypoint[b_].position;
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
        }
    }
}

#endregion waypoint based

namespace Transient.Pathfinding {
    public class AStarPathfinding {

        struct IntermediateState {
            public byte visited;
            public int from;
            public float value;
            public float cost;
        }

        private IntermediateState[] _state;
        private readonly List<int> _open;
        private IGraph _graph;
        private int _start;
        private int _goal;
        private int _current;
#if DebugPathfinding
        private StringBuilder _text = new StringBuilder();
#endif

        public AStarPathfinding(int capacity_ = 128) {
            _state = new IntermediateState[capacity_];
            _open = new(capacity_ >> 1);
        }

        private void Setup(IGraph graph_) {
            _graph = graph_;
            if (_state.Length < _graph.data.Size) {
                _state = new IntermediateState[_graph.data.Size];
            }
            else {
                Array.Clear(_state, 0, _state.Length);
            }
            _open.Clear();
        }

        public void FillPath(PathMoveGrid movement_, int goal_) {
            movement_.Reset(_graph.data, FillPath(goal_, movement_.path));
        }

        public void FillPath(PathMoveGrid movement_) => FillPath(movement_, _goal);

        public bool FillPath(int goal_, List<int> buffer_) {
            buffer_.Clear();
            int next = goal_;
            int loop = -1;
            while (next != _start && ++loop < _graph.data.Size) {
                //handle unreachable
                if (_state[next].visited == 0) {
                    return false;
                }
                buffer_.Add(next);
                next = _state[next].from;
            }
            return true;
        }

        public bool FillPath(List<int> buffer_) => FillPath(_goal, buffer_);
        public bool FillPathNearest(List<int> buffer_) => FillPath(FindNearest(_goal), buffer_);
        public bool FillPathNearest(int goal_, List<int> buffer_) => FillPath(FindNearest(goal_), buffer_);

        public string FormattedPath(int goal_ = -1, StringBuilder text_ = null, string linkSymbol_ = "-") {
            var text = text_ ?? new StringBuilder("path:");
            int next = goal_ < 0 ? _goal : goal_;
            int loop = -1;
            while (next != _start && ++loop < _graph.data.Size) {
                _graph.data.PrintNode(next, text);
                //handle unreachable
                if (_state[next].visited == 0) {
                    text.Append(linkSymbol_).Append("x").Append(linkSymbol_);
                    return text_ == null ? FormattedPath(FindNearest(goal_), text) : text.ToString();
                }
                text.Append(linkSymbol_);
                next = _state[next].from;
            }
            _graph.data.PrintNode(_start, text);
            return text.ToString();
        }

        /*
        public string FormattedGraph(StringBuilder text_ = null) {
            var text = text_ ?? new StringBuilder("graph:\n");
            var data = (GridData)_graph.data;
            var dirLookup = new System.Collections.Generic.Dictionary<int, string>();
            dirLookup.Add(0, "*");
            dirLookup.Add(1, "←");
            dirLookup.Add(-1, "→");
            dirLookup.Add(data.width, "↑");
            dirLookup.Add(-data.width, "↓");
            dirLookup.Add(data.width + 1, "↖");
            dirLookup.Add(data.width - 1, "↗");
            dirLookup.Add(-data.width + 1, "↙");
            dirLookup.Add(-data.width - 1, "↘");
            for (var k = 0; k < _graph.data.Size; ++k) {
                var state = _state[k];
                if (k == _start) text.Append("+s|\t");
                else if (k == _goal) text.Append("+e|\t");
                else if (state.value == 0) text.Append("  |\t");
                else {
                    if (!dirLookup.TryGetValue(state.from - k, out var dir))
                        dir = (state.from - k).ToString();
                    text.Append(state.value).Append(dir).Append("|").Append("\t");
                }
                if ((k + 1) % data.width == 0) text.AppendLine();
            }
            return text.ToString();
        }
        */

        //only works when:
        //1. unreachable
        //2. state.value represents wave generation = uniform node cost/travel cost
        private int FindNearest(int goal_) {
            var nearest = _start;
            var min = float.MaxValue;
            var max = 0f;
            for (int i = 0; i < _graph.data.Size; ++i) {
                //supplement heuristic cost if not calculated in pathfinding
                var cost = _state[i].cost + (_goal < 0 ? _graph.HeuristicCost(i, goal_) : 0);
                var value = _state[i].value;
                if (value > max) {
                    min = float.MaxValue;
                    max = value;
                }
                //Debug.Log($"{i} {cost} {value}");
                if (value >= max && cost < min && cost > 0) {
                    nearest = i;
                    min = cost;
                    max = value;
                }
            }
            //Debug.Log($"{nearest} {_state[nearest].value}|{_state[nearest].cost}, {_goal} {_state[_goal].value}|{_state[_goal].cost}");
            return nearest;
        }

        private float HeuristicCost(int a_, int b_) => _goal < 0 ? 0 : _graph.HeuristicCost(a_, b_);

        public bool FindPath(IGraph graph_, int start_, int goal_) {
            Setup(graph_);
            _start = start_;
            _goal = goal_;
            _open.Add(_start);
            _state[_start].value = 0;
            _state[_start].cost = HeuristicCost(_start, _goal);
            _current = 0;
            float min;
            var currentIndex = 0;
            int next;
            var iter = 0;
#if DebugPathfinding
            _text.Clear();
            _text.AppendLine("find path:");
#endif
            while (_open.Count > 0 && ++iter <= _state.Length) {
                min = float.MaxValue;
                for (int i = 0; i < _open.Count; ++i) {
                    next = _open[i];
#if DebugPathfinding
                    _graph.data.PrintNode(next, _text).AppendLine($" {_state[next].cost}");
#endif
                    if (_state[next].cost < min) {
                        currentIndex = i;
                        _current = next;
                        min = _state[next].cost;
                    }
                }
#if DebugPathfinding
                _text.Append("select ");
                _graph.data.PrintNode(_current, _text).AppendLine($" {min}");
#endif
                if (_current == _goal) {
                    _state[_current].visited = 1;
#if DebugPathfinding
                    Log.Debug(_text.ToString());
#endif
                    return true;
                }
                _open.OutOfOrderRemoveAt(currentIndex);
                _state[_current].visited = 1;
                foreach (var (link, cost) in _graph.EnumerateLink(_current)) {
                    TryAdd(link, cost);
                }
            }
            if (iter == _state.Length) {
                Log.Warn("iteration overflown");
#if DebugPathfinding
                Log.Debug(_text.ToString());
#endif
            }
            return false;
        }

        private void TryAdd(int next_, float travelCost_) {
            float tentative = _graph.data.Cost(next_);
            if (tentative == _graph.data.InaccessibleMask) return;//inaccessible
            tentative += _state[_current].value + travelCost_;
            if (_state[next_].value > 0 && tentative >= _state[next_].value) {
                return;
            }
            _open.Add(next_);
            _state[next_].from = _current;
            _state[next_].value = tentative;
            _state[next_].cost = tentative + HeuristicCost(next_, _goal);
#if DebugPathfinding
            _text.Append("add ");
            _graph.data.PrintNode(next_, _text).AppendLine($" value={tentative} cost={_state[next_].cost}");
#endif
        }
    }

    public class PathMoveGrid {
        public IGraphData dataRef;
        public List<int> path = new(16);
        public int target;
        public int stop = 0;
        public Vector2 pos;
        public Vector2 dir;
        public bool Reachable { get; private set; }
        public bool Reached => target < stop;

        public void Reset(IGraphData data_, bool reachable_) {
            dataRef = data_;
            target = path.Count - 1;
            Reachable = reachable_;
            NextSegment(target);
        }

        private void NextSegment(int t_) {
            pos = dataRef.Position(path[t_]);
        }

        public (Vector2, Vector2) Move(float dist_, Vector2 pos_) {
            Vector2 position = pos_;
        move_to_next:
            dir = pos - position;
            var dist = dir.magnitude;
            if (dist < dist_) {
                dist_ -= dist;
                //passed next node
                position = pos;
                if (--target < stop) {
                    //reached
                    return (position, dir);
                }
                NextSegment(target);
                goto move_to_next;
            }
            return (position + dir * dist_ / dist, dir);
        }
    }

    public class PathMoveBezier {
        public IGraphData dataRef;
        public List<(int index, float distance)> path = new(16);
        public int target;
        public int stop = 0;
        public Vector2 pos;
        public Vector2 dir;
        public Vector2 p0, p1, p2;
        public float move;
        public float distance;
        public float segment0;
        public float segment1;
        public bool Reachable { get; private set; }
        public bool Reached => target < stop;

        public void Reset(IGraphData data_, bool reachable_) {
            dataRef = data_;
            target = path.Count - 1;
            Reachable = reachable_;
            NextSegment(target);
        }

        private void NextSegment(int t_) {
            var (index1, distance1) = path[t_];
            //TODO
            //pos = dataRef.Position(path[t_]);
        }

        private Vector2 EvaluateCurve() {
            //TODO
            return Vector2.zero;
        }

        public (Vector2, Vector2) Move(float dist_, Vector2 pos_) {
            Vector2 position = pos_;
        move_to_next:
            dir = pos - position;
            var dist = dir.magnitude;
            if (dist < dist_) {
                //passed next node
                position = pos;
                if (--target < stop) {
                    //reached
                    return (position, dir);
                }
                NextSegment(target);
                goto move_to_next;
            }
            return (position + dir * dist_ / dist, dir);
        }
    }
}
