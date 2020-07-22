using UnityEngine;
using Transient.Pathfinding;
using NUnit.Framework;
using Transient.SimpleContainer;

namespace Tests {
    class PathfindingTest {
        private void CheckPath(string path, string pathToCompare) {
            Debug.Log(path);
            Assert.AreEqual(pathToCompare, path);
        }

        [Test]
        public void GridPathfinding() {
            ushort width = 5;
            ushort height = 5;
            byte[] raw = new byte[] {
                1,  1,  1,  1,  1,
                1,  1,  1,  1,  1,
                1,128,128,128,  1,
                1,  1,  1,  1,  1,
                1,  1,  1,  1,  1,
            };
            var data00 = new GridData();
            data00.Fill(width, height, raw);
            var data22 = new GridData();
            data22.Fill(width, height, raw, 2, 2);
            var rect4 = new RectGrid4Dir(data00);
            var rect4_offset = new RectGrid4Dir(data22);
            var rect8 = new RectGrid8Dir(data00);
            var axialHex = new AxialHexGrid(data00);
            var astar = new AStarPathfinding();
            Debug.Log(nameof(rect4));
            rect4.FindPath(astar, 1, 0, 2, 4);
            CheckPath(astar.FormattedPath(), "path:2,4-2,3-1,3-0,3-0,2-0,1-0,0-1,0");
            Debug.Log(nameof(rect4_offset));
            rect4_offset.FindPath(astar, -1, -2, 0, 2);
            CheckPath(astar.FormattedPath(), "path:2,4-2,3-1,3-0,3-0,2-0,1-0,0-1,0");
            Debug.Log(nameof(rect8));
            rect8.FindPath(astar, 1, 0, 2, 4);
            CheckPath(astar.FormattedPath(), "path:2,4-1,3-0,2-0,1-1,0");
            Debug.Log(nameof(axialHex));
            axialHex.FindPath(astar, 1, 0, 2, 4);
            CheckPath(astar.FormattedPath(), "path:2,4-1,3-0,2-0,1-1,1-0,0-1,0");
        }

        [Test]
        public void WaypointPathfinding() {
            var waypointData = new WaypointData();
            var waypoint = new Waypoint[32];
            for (int i = 0; i < waypoint.Length; ++i) {
                waypoint[i] = new Waypoint() {
                    link = new List<int>(4),
                    position = new Vector2(0, i),
                };
            }
            waypoint[0].link.Add(1);
            waypoint[0].link.Add(2);
            waypoint[1].link.Add(6);
            waypoint[2].link.Add(6);
            waypoint[2].link.Add(3);
            waypoint[3].link.Add(4);
            waypoint[4].link.Add(5);
            waypoint[6].link.Add(7);
            waypoint[7].link.Add(8);
            waypoint[7].link.Add(9);
            waypointData.waypoint = waypoint;
            waypointData.MakeBidirectional();
            var graph = new WaypointGraph(waypointData);
            var astar = new AStarPathfinding();
            astar.FindPath(graph, 0, 9);
            CheckPath(astar.FormattedPath(), "path:9-7-6-2-0");
            astar.FindPath(graph, 5, 8);
            CheckPath(astar.FormattedPath(), "path:8-7-6-2-3-4-5");
            astar.FindPath(graph, 5, 31);
            CheckPath(astar.FormattedPath(), "path:31-x-9-7-6-2-3-4-5");

            astar.FindPath(graph, 0, -1);
            CheckPath(astar.FormattedPath(9), "path:9-7-6-2-0");
            CheckPath(astar.FormattedPath(6), "path:6-2-0");
            CheckPath(astar.FormattedPath(4), "path:4-3-2-0");
            CheckPath(astar.FormattedPath(11), "path:11-x-9-7-6-2-0");
        }
    }
}
