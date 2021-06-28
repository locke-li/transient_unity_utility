
using System;
using System.Text;
using Transient.SimpleContainer;
using UnityEngine;

namespace Transient.Pathfinding {
    public interface IGraphData {
        int Size { get; }
        Vector3 Position(int index_);
        byte Cost(int index_);
        void PrintNode(int index_, StringBuilder text_);
    }

    public interface IGraph {
        IGraphData data { get; }
        System.Collections.Generic.IEnumerable<(int, uint)> EnumerateLink(int index_);
        uint HeuristicCost(int a_, int b_);
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
        public ushort widthBorder;
        public ushort heighBorder;
        public ushort width;
        public ushort height;
        //origin point coordinates in a system where the origin is in the top-left corner
        public ushort originX;
        public ushort originY;
        public Vector3 gridX;
        public Vector3 gridY;

        public void Fill(ushort w_, ushort h_, byte[] v_ = null, byte vd_ = 0, ushort originX_ = 0, ushort originY_ = 0) {
            field = new Grid[w_ * h_];
            //calculated for ease of comparison
            widthBorder = (ushort)(w_ - 1);
            heighBorder = (ushort)(h_ - 1);
            width = w_;
            height = h_;
            originX = originX_;
            originY = originY_;
            int i;
            for (ushort x = 0; x < w_; ++x) {
                for (ushort y = 0; y < h_; ++y) {
                    i = x + y * w_;
                    field[i] = new Grid() {
                        x = x,
                        y = y,
                        v = v_ != null ? v_[i] : vd_
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
            return (grid.x + 0.5f) * gridX + (grid.y + 0.5f) * gridY;
        }

        public byte Cost(int index_) => field[index_].v;

        public void PrintNode(int index_, StringBuilder text_) {
            text_.Append(index_ % width);
            text_.Append(",");
            text_.Append(index_ / width);
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

        public System.Collections.Generic.IEnumerable<(int, uint)> EnumerateLink(int index_) {
            int x = _data.field[index_].x;
            int y = _data.field[index_].y;
            //4 directions
            if (x > 0) yield return (index_ - 1, 1);
            if (y > 0) yield return (index_ - _data.width, 1);
            if (x < _data.widthBorder) yield return (index_ + 1, 1);
            if (y < _data.heighBorder) yield return (index_ + _data.width, 1);
        }

        public uint HeuristicCost(int a_, int b_) {
            var a = _data.field[a_];
            var b = _data.field[b_];
            return (uint)(Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y));
        }
    }

    public class RectGrid8Dir : IGraph {
        private readonly GridData _data;
        public IGraphData data => _data;

        public RectGrid8Dir(GridData data_) {
            _data = data_;
        }

        public void FindPath(AStarPathfinding astar_, short startX_, short startY_, short goalX_, short goalY_) {
            astar_.FindPath(this, _data.Coord2Index(startX_, startY_), _data.Coord2Index(goalX_, goalY_));
        }

        public System.Collections.Generic.IEnumerable<(int, uint)> EnumerateLink(int index_) {
            int x = _data.field[index_].x;
            int y = _data.field[index_].y;
            //8 directions
            if (x > 0) {
                yield return (index_ - 1, 1);//-x
                if (y > 0) yield return (index_ - 1 - _data.width, 1);//-x-y
                if (y < _data.heighBorder) yield return (index_ - 1 + _data.width, 1);//-x+y
            }
            if (y > 0) yield return (index_ - _data.widthBorder, 1);//-y
            if (x < _data.widthBorder) {
                yield return (index_ + 1, 1);//+x
                if (y > 0) yield return (index_ + 1 - _data.width, 1);//+x-y
                if (y < _data.heighBorder) yield return (index_ + 1 + _data.width, 1);//+x+y
            }
            if (y < _data.heighBorder) yield return (index_ + _data.width, 1);//+y
        }

        public uint HeuristicCost(int a_, int b_) {
            var a = _data.field[a_];
            var b = _data.field[b_];
            return (uint)(Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y));
        }
    }

    public class AxialHexGrid : IGraph {
        private readonly GridData _data;
        public IGraphData data => _data;

        public AxialHexGrid(GridData data_) {
            _data = data_;
        }

        public void FindPath(AStarPathfinding astar_, short startX_, short startY_, short goalX_, short goalY_) {
            astar_.FindPath(this, _data.Coord2Index(startX_, startY_), _data.Coord2Index(goalX_, goalY_));
        }

        public System.Collections.Generic.IEnumerable<(int, uint)> EnumerateLink(int index_) {
            int x = _data.field[index_].x;
            int y = _data.field[index_].y;
            //6 directions
            if (x > 0) {
                yield return (index_ - 1, 1);//-x
                if (y > 0) yield return (index_ - 1 - _data.width, 1);//-x-y
            }
            if (y > 0) yield return (index_ - _data.width, 1);//-y
            if (x < _data.widthBorder) {
                yield return (index_ + 1, 1);//+x
                if (y < _data.heighBorder) yield return (index_ + 1 + _data.width, 1);//+x+y
            }
            if (y < _data.heighBorder) yield return (index_ + _data.width, 1);//+y
        }

        public uint HeuristicCost(int a_, int b_) {
            var a = _data.field[a_];
            var b = _data.field[b_];
            return (uint)(Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y));
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

        public byte Cost(int index_) => waypoint[index_].weight;

        public void PrintNode(int index_, StringBuilder text_) {
            text_.Append(index_);
        }
    }

    public class WaypointGraph : IGraph {
        private readonly WaypointData _data;
        public IGraphData data => _data;
        public uint HeuristicScale { get; set; } = 100;

        public WaypointGraph(WaypointData data_) {
            _data = data_;
        }

        public System.Collections.Generic.IEnumerable<(int, uint)> EnumerateLink(int index_) {
            foreach (var link in _data.waypoint[index_].link) {
                yield return (link, 1);
            }
        }

        public uint HeuristicCost(int a_, int b_) {
            var a = _data.waypoint[a_];
            var b = _data.waypoint[b_];
            var line = a.position - b.position;
            return (uint)Mathf.Round(line.sqrMagnitude * HeuristicScale);
        }
    }
}

#endregion waypoint based

namespace Transient.Pathfinding {
    public class AStarPathfinding {

        struct IntermediateState {
            public byte visited;
            public int from;
            public uint value;
            public uint cost;
        }

        private IntermediateState[] _state;
        private readonly List<int> _open;
        private IGraph _graph;
        private int _start;
        private int _goal;
        private int _current;
        public int inaccessibleValue = byte.MaxValue;

        public AStarPathfinding(int capacity_ = 128) {
            _state = new IntermediateState[capacity_];
            _open = new List<int>(capacity_ >> 1);
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

        public string FormattedPath(int goal_, StringBuilder text_ = null) {
            var text = text_ ?? new StringBuilder("path:");
            int next = goal_;
            int loop = -1;
            while (next != _start && ++loop < _graph.data.Size) {
                _graph.data.PrintNode(next, text);
                //handle unreachable
                if (_state[next].visited == 0) {
                    text.Append("-x-");
                    return text_ == null ? FormattedPath(FindNearest(goal_), text) : text.ToString();
                }
                text.Append("-");
                next = _state[next].from;
            }
            _graph.data.PrintNode(_start, text);
            return text.ToString();
        }

        public string FormattedPath() => FormattedPath(_goal);

        //only works when:
        //1. unreachable
        //2. state.value represents wave generation = uniform node cost/travel cost
        private int FindNearest(int goal_) {
            int nearest = _start;
            var min = uint.MaxValue;
            uint max = 0;
            for (int i = 0; i < _graph.data.Size; ++i) {
                //supplement heuristic cost if not calculated in pathfinding
                var cost = _state[i].cost + (_goal < 0 ? _graph.HeuristicCost(i, goal_) : 0);
                var value = _state[i].value;
                if (value > max) {
                    min = uint.MaxValue;
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

        private uint HeuristicCost(int a_, int b_) => _goal < 0 ? 0 : _graph.HeuristicCost(a_, b_);

        public bool FindPath(IGraph graph_, int start_, int goal_) {
            Setup(graph_);
            _start = start_;
            _goal = goal_;
            _open.Add(_start);
            _state[_start].value = 0;
            _state[_start].cost = HeuristicCost(_start, _goal);
            _current = 0;
            uint min;
            int currentIndex = 0;
            int next;
            while (_open.Count > 0) {
                min = uint.MaxValue;
                for (int i = 0; i < _open.Count; ++i) {
                    next = _open[i];
                    if (_state[next].cost < min) {
                        currentIndex = i;
                        _current = next;
                        min = _state[next].cost;
                    }
                }
                //Log.Debug($"select {_current}");
                if (_current == _goal) {
                    _state[_current].visited = 1;
                    return true;
                }
                _open.OutOfOrderRemoveAt(currentIndex);
                _state[_current].visited = 1;
                foreach (var (link, cost) in _graph.EnumerateLink(_current)) {
                    TryAdd(link, cost);
                }
            }
            return false;
        }

        private void TryAdd(int next_, uint travelCost_) {
            uint tentative = _graph.data.Cost(next_);
            if (tentative == inaccessibleValue) return;//inaccessible
            tentative += _state[_current].value + travelCost_;
            if (_state[next_].visited == 0) {
                _open.Add(next_);
            }
            else if (tentative > _state[next_].value) {
                return;
            }
            _state[next_].from = _current;
            _state[next_].value = tentative;
            _state[next_].cost = tentative + HeuristicCost(next_, _goal);
            //Log.Debug($"add node {next_} value={tentative} cost={_state[next_].cost}");
        }
    }

    public class PathMoveGrid {
        public IGraphData dataRef;
        public List<int> path;
        public int target;
        public int stop;
        public Vector2 pos;
        public Vector2 dir;
        public bool Reachable { get; private set; }
        public bool Reached => target < stop;

        public PathMoveGrid() {
            path = new List<int>(16);
            stop = 0;
        }

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
        public List<(int index, float distance)> path;
        public int target;
        public int stop;
        public Vector2 pos;
        public Vector2 dir;
        public Vector2 p0, p1, p2;
        public float move;
        public float distance;
        public float segment0;
        public float segment1;
        public bool Reachable { get; private set; }
        public bool Reached => target < stop;

        public PathMoveBezier() {
            path = new List<(int, float)>(16);
            stop = 0;
        }

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
