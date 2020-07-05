﻿
using System;
using System.Text;
using Transient.SimpleContainer;
using UnityEngine;

namespace Transient.Pathfinding {
    public interface IGraphData {
        int Size { get; }
        Vector2 Position(int index_);
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
        //a size limit of 65536x65536 should be reasonable for grid based data
        public ushort width;
        public ushort height;
        //origin point coordination in a system where the origin is in the top-left corner
        public ushort originX;
        public ushort originY;
        public float gridSize;

        public void Fill(ushort w_, ushort h_, byte[] v_, ushort originX_ = 0, ushort originY_ = 0) {
            field = new Grid[w_ * h_];
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
                        v = v_[i]
                    };
                }
            }
        }

        public int Coord2Index(short x, short y) {
            return (x + originX) + (y + originY) * width;
        }

        public Vector2 Position(int index_) {
            var grid = field[index_];
            return new Vector2((grid.x + 0.5f) * gridSize, -(grid.y + 0.5f) * gridSize);
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
        private readonly ushort widthExpandLimit;
        private readonly ushort heightExpandLimit;

        public RectGrid4Dir(GridData data_) {
            _data = data_;
            widthExpandLimit = (ushort)(_data.width - 1);
            heightExpandLimit = (ushort)(_data.height - 1);
        }

        public void FindPath(AStarPathfinding astar_, short startX_, short startY_, short goalX_, short goalY_) {
            astar_.FindPath(this, _data.Coord2Index(startX_, startY_), _data.Coord2Index(goalX_, goalY_));
        }

        public System.Collections.Generic.IEnumerable<(int, uint)> EnumerateLink(int index_) {
            int x = _data.field[index_].x;
            int y = _data.field[index_].y;
            if (x > 0) yield return (index_ - 1, 1);
            if (y > 0) yield return (index_ - _data.width, 1);
            if (x < widthExpandLimit) yield return (index_ + 1, 1);
            if (y < heightExpandLimit) yield return (index_ + _data.width, 1);
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
        private readonly int widthExpandLimit;
        private readonly int heightExpandLimit;

        public RectGrid8Dir(GridData data_) {
            _data = data_;
            widthExpandLimit = _data.width - 1;
            heightExpandLimit = _data.height - 1;
        }

        public void FindPath(AStarPathfinding astar_, short startX_, short startY_, short goalX_, short goalY_) {
            astar_.FindPath(this, _data.Coord2Index(startX_, startY_), _data.Coord2Index(goalX_, goalY_));
        }

        public System.Collections.Generic.IEnumerable<(int, uint)> EnumerateLink(int index_) {
            int x = _data.field[index_].x;
            int y = _data.field[index_].y;
            if (x > 0) {
                yield return (index_ - 1, 1);//-x
                if (y > 0) yield return (index_ - 1 - _data.width, 1);//-x-y
                if (y < heightExpandLimit) yield return (index_ - 1 + _data.width, 1);//-x+y
            }
            if (y > 0) yield return (index_ - _data.width, 1);//-y
            if (x < widthExpandLimit) {
                yield return (index_ + 1, 1);//+x
                if (y > 0) yield return (index_ + 1 - _data.width, 1);//+x-y
                if (y < heightExpandLimit) yield return (index_ + 1 + _data.width, 1);//+x+y
            }
            if (y < heightExpandLimit) yield return (index_ + _data.width, 1);//+y
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
        private readonly int widthExpandLimit;
        private readonly int heightExpandLimit;

        public AxialHexGrid(GridData data_) {
            _data = data_;
            widthExpandLimit = _data.width - 1;
            heightExpandLimit = _data.height - 1;
        }

        public void FindPath(AStarPathfinding astar_, short startX_, short startY_, short goalX_, short goalY_) {
            astar_.FindPath(this, _data.Coord2Index(startX_, startY_), _data.Coord2Index(goalX_, goalY_));
        }

        public System.Collections.Generic.IEnumerable<(int, uint)> EnumerateLink(int index_) {
            int x = _data.field[index_].x;
            int y = _data.field[index_].y;
            if (x > 0) {
                yield return (index_ - 1, 1);//-x
                if (y > 0) yield return (index_ - 1 - _data.width, 1);//-x-y
            }
            if (y > 0) yield return (index_ - _data.width, 1);//-y
            if (x < widthExpandLimit) {
                yield return (index_ + 1, 1);//+x
                if (y < heightExpandLimit) yield return (index_ + 1 + _data.width, 1);//+x+y
            }
            if (y < heightExpandLimit) yield return (index_ + _data.width, 1);//+y
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
        public Vector2 position;
        public byte weight;
        public List<int> link;
    }

    public class WaypointData : IGraphData {
        public Waypoint[] waypoint;
        public int Size => waypoint.Length;

        public int Position2Index(Vector2 position) {
            var min = float.MaxValue;
            int ret = 0;
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

        public Vector2 Position(int index_) {
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
            public uint heuristic;
        }

        private IntermediateState[] _state;//1: in open list, 2: in closed list
        private readonly List<int> _open;
        private IGraph _graph;
        private int _start;
        private int _goal;
        private int _current;

        public AStarPathfinding(int capacity_ = 128) {
            _state = new IntermediateState[capacity_];
            _open = new List<int>(capacity_ >> 1);
        }

        private void Setup(IGraph graph_) {
            _graph = graph_;
            if(_state.Length < _graph.data.Size) {
                _state = new IntermediateState[_graph.data.Size];
            }
            else {
                Array.Clear(_state, 0, _state.Length);
            }
            _open.Clear();
        }

        public void FillPath(PathMovement movement_, int goal_) {
            movement_.Reset(_graph.data, FillPath(goal_, movement_.path));
        }

        public void FillPath(PathMovement movement_) => FillPath(movement_, _goal);

        public bool FillPath(int goal_, List<int> buffer_) {
            buffer_.Clear();
            int next = goal_;
            int loop = -1;
            while(next != _start && ++loop < _graph.data.Size) {
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
            var text = text_??new StringBuilder("path:");
            int next = goal_;
            int loop = -1;
            while(next != _start && ++loop < _graph.data.Size) {
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
            while(_open.Count > 0) {
                min = uint.MaxValue;
                for(int i = 0; i < _open.Count; ++i) {
                    next = _open[i];
                    if(_state[next].cost < min) {
                        currentIndex = i;
                        _current = next;
                        min = _state[next].cost;
                    }
                }
                //Log.Debug($"select {_current}");
                if(_current == _goal) {
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
            uint tentative = _state[_current].value + _graph.data.Cost(next_) + travelCost_;
            if (_state[next_].visited == 0) {
                _open.Add(next_);
            }
            else if(tentative > _state[next_].value) {
                return;
            }
            _state[next_].from = _current;
            _state[next_].value = tentative;
            _state[next_].cost = tentative + HeuristicCost(next_, _goal);
            //Log.Debug($"add node {next_} value={tentative} cost={_state[next_].cost}");
        }
    }

    public class PathMovement {
        public IGraphData dataRef;
        public List<int> path;
        public int pathTarget;
        public Vector2 nodePos;
        public Vector2 dir;
        public bool Reachable { get; private set; }
        public bool Reached { get { return pathTarget < 0; } }

        public PathMovement() {
            path = new List<int>(16);
        }

        public void Reset(IGraphData data_, bool reachable_) {
            dataRef = data_;
            pathTarget = path.Count - 1;
            Reachable = reachable_;
            NextSegment(pathTarget);
        }

        private void NextSegment(int t_) {
            if (t_ < 0) return;
            nodePos = dataRef.Position(path[t_]);
        }

        public Vector2 Move(float dist_, Vector2 pos_) {
            Vector2 position = pos_;
            move_to_next:
            dir = nodePos - position;
            var dist = dir.magnitude;
            if (dist < dist_) {
                //passed next node
                position = nodePos;
                if (--pathTarget < 0) {
                    //reached
                    return position;
                }
                nodePos = dataRef.Position(path[pathTarget]);
                goto move_to_next;
            }
            return position + dir * dist_ / dist;
        }
    }
}
