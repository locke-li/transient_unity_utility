
using System;
using System.Text;
using Transient.SimpleContainer;
using UnityEngine;

namespace Transient.Pathfinding {
    public class PathMovement {
        public GridData dataRef;
        public List<int> path;
        public int pathTarget;
        public Grid fieldTarget;
        public Vector2 gridPos;
        public Vector2 dir;
        public bool Reached { get { return pathTarget < 0; } }

        public PathMovement() {
            path = new List<int>(16);
        }

        public void Reset(GridData data_, int target_) {
            dataRef = data_;
            pathTarget = target_-1;
            AtGrid(pathTarget);
        }

        private void AtGrid(int t_) {
            if(t_ < 0) return;
            fieldTarget = dataRef.field[path[t_]];
            gridPos = new Vector3((fieldTarget.x + 0.5f) * dataRef.gridSize, -(fieldTarget.y + 0.5f) * dataRef.gridSize);
        }

        public Vector2 Move(float dist_, Vector2 pos_) {
            dir = gridPos - pos_;
            Vector2 ret;
            if(dir.sqrMagnitude < dist_ * dist_) {
                ret = gridPos;
                AtGrid(--pathTarget);
            }
            else {
                ret = pos_ + dir.normalized * dist_;
            }
            return ret;
        }
    }

    public struct Grid {
        //virtual position when origin of the grid field is in top-left corner
        public ushort x;
        public ushort y;
        public byte v;//path finding related cost value
    }

    public class GridData {
        public Grid[] field;
        //a size limit of 65536x65536 should be reasonable for grid based data
        public ushort width;
        public ushort height;
        //origin point coordination in a system where the origin is in the top-left corner
        public ushort originX;
        public ushort originY;
        public float gridSize;

        public int Coord2Index (short x, short y) {
            return (x + originX) + (y + originY) * width;
        }

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
    }

    public interface IGraph {
        GridData data { get; set; }
        void AddNeighbour(int current_, AStarPathfinding astar_);
        uint HeuristicCost(Grid a, Grid b);
    }

    public class RectGrid4Dir : IGraph {
        public GridData data { get; set; }
        private readonly ushort widthExpandLimit;
        private readonly ushort heightExpandLimit;

        public RectGrid4Dir(GridData data_) {
            data = data_;
            widthExpandLimit = (ushort)(data.width - 1);
            heightExpandLimit = (ushort)(data.height - 1);
        }

        public void AddNeighbour(int current_, AStarPathfinding astar_) {
            int x = data.field[current_].x;
            int y = data.field[current_].y;
            if(x > 0) astar_.TryAdd(current_-1, 1);
            if(y > 0) astar_.TryAdd(current_-data.width, 1);
            if(x < widthExpandLimit) astar_.TryAdd(current_+1, 1);
            if(y < heightExpandLimit) astar_.TryAdd(current_+data.width, 1);
        }

        public uint HeuristicCost(Grid a, Grid b) {
            return (uint)(Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y));
        }
    }

    public class RectGrid8Dir : IGraph {
        public GridData data { get; set; }
        private readonly int widthExpandLimit;
        private readonly int heightExpandLimit;

        public RectGrid8Dir(GridData data_) {
            data = data_;
            widthExpandLimit = data.width - 1;
            heightExpandLimit = data.height - 1;
        }

        public void AddNeighbour(int current_, AStarPathfinding astar_) {
            int x = data.field[current_].x;
            int y = data.field[current_].y;
            if(x > 0) {
                astar_.TryAdd(current_-1, 1);//-x
                if(y > 0) astar_.TryAdd(current_-1-data.width, 1);//-x-y
                if(y < heightExpandLimit) astar_.TryAdd(current_-1+data.width, 1);//-x+y
            }
            if(y > 0) astar_.TryAdd(current_-data.width, 1);//-y
            if(x < widthExpandLimit) {
                astar_.TryAdd(current_+1, 1);//+x
                if(y > 0) astar_.TryAdd(current_+1-data.width, 1);//+x-y
                if(y < heightExpandLimit) astar_.TryAdd(current_+1+data.width, 1);//+x+y
            }
            if(y < heightExpandLimit) astar_.TryAdd(current_+data.width, 1);//+y
        }

        public uint HeuristicCost(Grid a, Grid b) {
            return (uint)(Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y));
        }
    }

    public class AxialHexGrid : IGraph {
        public GridData data { get; set; }
        private readonly int widthExpandLimit;
        private readonly int heightExpandLimit;

        public AxialHexGrid(GridData data_) {
            data = data_;
            widthExpandLimit = data.width - 1;
            heightExpandLimit = data.height - 1;
        }

        public void AddNeighbour(int current_, AStarPathfinding astar_) {
            int x = data.field[current_].x;
            int y = data.field[current_].y;
            if (x > 0) {
                astar_.TryAdd(current_ - 1, 1);//-x
                if (y > 0) astar_.TryAdd(current_ - 1 - data.width, 1);//-x-y
            }
            if (y > 0) astar_.TryAdd(current_ - data.width, 1);//-y
            if (x < widthExpandLimit) {
                astar_.TryAdd(current_ + 1, 1);//+x
                if (y < heightExpandLimit) astar_.TryAdd(current_ + 1 + data.width, 1);//+x+y
            }
            if (y < heightExpandLimit) astar_.TryAdd(current_ + data.width, 1);//+y
        }

        public uint HeuristicCost(Grid a, Grid b) {
            return (uint)(Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y));
        }
    }

    public class AStarPathfinding {

        struct IntermediateState {
            public byte state;
            public int from;
            public uint value;
            public uint cost;
        }

        private IntermediateState[] _state;//1: in open list, 2: in closed list
        private readonly List<int> _open;
        private GridData _data;
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
            _data = graph_.data;
            if(_state.Length < _data.field.Length) {
                _state = new IntermediateState[_data.field.Length];
            }
            else {
                Array.Clear(_state, 0, _state.Length);
            }
            _open.Clear();
        }

        public void FillPath(PathMovement movement_) {
            movement_.Reset(_data, FillPath(movement_.path));
        }

        public int FillPath(List<int> buffer_) {
            buffer_.Clear();
            int next = _goal;
            int loop = 0;
            while(next != _start && loop < _data.field.Length) {
                ++loop;
                buffer_.Add(next);
                next = _state[next].from;
            }
            return buffer_.Count;
        }

        public void PrintPath() {
            StringBuilder text = new StringBuilder("path:");
            int next = _goal;
            int loop = 0;
            while(next != _start && loop < _data.field.Length) {
                ++loop;
                text.Append(next % _data.width);
                text.Append(",");
                text.Append(next / _data.width);
                text.Append("-");
                next = _state[next].from;
            }
            text.Append(_start % _data.width);
            text.Append(",");
            text.Append(_start / _data.width);
            Log.Debug(text.ToString());
        }

        public void FindPath(IGraph graph_, short startX_, short startY_, short goalX_, short goalY_) {
            Setup(graph_);
            _start = graph_.data.Coord2Index(startX_, startY_);
            _goal = graph_.data.Coord2Index(goalX_, goalY_);
            _open.Add(_start);
            _state[_start].value = 0;
            _state[_start].cost = _graph.HeuristicCost(_data.field[_start], _data.field[_goal]);
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
                //Logger.Debug($"select {_current}");
                if(_current == _goal) {
                    PrintPath();
                    return;
                }
                _open.OutOfOrderRemoveAt(currentIndex);
                ++_state[_current].state;
                _graph.AddNeighbour(_current, this);
            }
        }

        public void TryAdd(int next_, uint travelCost_) {
            uint tentative = _state[_current].value + _data.field[next_].v + travelCost_;
            switch (_state[next_].state) {
                case 0:
                    _open.Add(next_);
                    ++_state[next_].state;
                    break;
                case 1:
                    if(tentative > _state[next_].value) return;
                    break;
                default:
                    return;
            }
            _state[next_].from = _current;
            _state[next_].value = tentative;
            _state[next_].cost = tentative + _graph.HeuristicCost(_data.field[next_], _data.field[_goal]);
            //Logger.Debug($"add node {next_} value={tentative} cost={_state[next_].cost}");
        }
    }
}
