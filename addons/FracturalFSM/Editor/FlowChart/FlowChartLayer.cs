
using System;
using System.Collections.Generic;
using System.Linq;
using Fractural.Utils;
using Godot;
using GDC = Godot.Collections;

namespace Fractural.FlowChart
{
    /// <summary>
    /// Lightweight representation of a connection between two nodes, by
    /// storing the name of each node.
    /// </summary>
    public struct ConnectionPair
    {
        public ConnectionPair(string from, string to)
        {
            From = from;
            To = to;
        }

        public string From { get; set; }
        public string To { get; set; }
    }

    /// <summary>
    /// Holds data about a connection between two nodes, as well as the line that visualizes it.
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// Control node that draw line
        /// </summary>
        public FlowChartLine Line { get; set; }

        public FlowChartNode FromNode { get; set; }
        public FlowChartNode ToNode { get; set; }
        /// <summary>
        /// Line's y offset to make space for two interconnecting lines
        /// </summary>
        public float Offset { get; set; } = 0;

        public Connection(FlowChartLine line, FlowChartNode fromNode, FlowChartNode toNode)
        {
            Line = line;
            FromNode = fromNode;
            ToNode = toNode;
        }

        /// <summary>
        /// Update line position
        /// </summary>
        public void Join()
        {
            Line.Join(GetFromPos(), GetToPos(), new Vector2(0, Offset), new Rect2[] {
                FromNode != null ? FromNode.GetRect() : new Rect2(),
                ToNode != null ? ToNode.GetRect() : new Rect2()
            });
        }

        /// <summary>
        /// Return start position of line
        /// </summary>
        /// <returns></returns>
        public Vector2 GetFromPos() => FromNode.RectPosition + FromNode.RectSize / 2f;

        /// <summary>
        /// Return destination position of line
        /// </summary>
        /// <returns></returns>
        public Vector2 GetToPos() => ToNode != null ? ToNode.RectPosition + ToNode.RectSize / 2f : Line.RectPosition;
    }

    [Tool]
    public class FlowChartLayer : Control
    {
        public Control ContentLines { get; private set; } = new Control(); // Node that hold all flowchart lines
        public Control ContentNodes { get; private set; } = new Control(); // Node that hold all flowchart nodes

        // [FlowChartNode.name] = [Connection.to] = Connection
        public GDC.Dictionary Connections { get; private set; } = new GDC.Dictionary() { };

        #region Connections Accessors
        public Connection GetConnection(ConnectionPair pair)
        {
            return Connections.Get<Connection>($"{pair.From}.{pair.To}");
        }

        public Connection GetConnection(string fromNode, string toNode)
        {
            return Connections.Get<Connection>($"{fromNode}.{toNode}");
        }

        public IList<Connection> GetNodeConnections(string fromNode)
        {
            var connectionsDict = GetNodeConnectionsDict(fromNode);
            return new List<Connection>(connectionsDict.Values.Cast<Connection>());
        }

        public GDC.Dictionary GetNodeConnectionsDict(string fromNode)
        {
            return Connections.Get<GDC.Dictionary>(fromNode);
        }
        #endregion

        public FlowChartLayer()
        {
            Name = "FlowChartLayer";
            MouseFilter = MouseFilterEnum.Ignore;

            ContentLines.Name = "content_lines";
            ContentLines.MouseFilter = MouseFilterEnum.Ignore;
            AddChild(ContentLines);
            MoveChild(ContentLines, 0);// Make sure contentLines always behind nodes

            ContentNodes.Name = "content_nodes";
            ContentNodes.MouseFilter = MouseFilterEnum.Ignore;
            AddChild(ContentNodes);
        }

        public void HideContent()
        {
            ContentNodes.Hide();
            ContentLines.Hide();
        }

        public void ShowContent()
        {
            ContentNodes.Show();
            ContentLines.Show();
        }

        /// <summary>
        /// Get required scroll rect base on content
        /// </summary>
        /// <param name="scrollMargin"></param>
        /// <returns></returns>
        public Rect2 GetScrollRect(int scrollMargin = 0)
        {
            Rect2 rect = new Rect2();
            foreach (Control child in ContentNodes.GetChildren())
            {
                var childRect = child.GetRect();
                rect = rect.Merge(childRect);
            }
            return rect.Grow(scrollMargin);
        }

        public void AddNode(Node node)
        {
            ContentNodes.AddChild(node);
        }

        public void RemoveNode(Node node)
        {
            if (node != null)
                ContentNodes.RemoveChild(node);
        }

        // TODO: Find a way to refactor out the call to AfterConnectNode
        //       from FlowChart since this method seems like it should be private.
        /// <summary>
        /// Called after connection established
        /// </summary>
        /// <param name="connection"></param>
        public void AfterConnectNode(Connection connection)
        {
            ContentLines.AddChild(connection.Line);
            connection.Join();
        }

        /// <summary>
        /// Called after connection broken
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private FlowChartLine AfterDisconnectNode(Connection connection)
        {
            ContentLines.RemoveChild(connection.Line);
            return connection.Line;
        }

        /// <summary>
        /// Rename node
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name=""></param>
        public void RenameNode(string oldName, string newName)
        {
            foreach (string from in Connections.Keys)
            {
                if (from == oldName) // Connection from
                {
                    var fromConnections = Connections.Get<GDC.Dictionary>(from);
                    Connections.Remove(oldName);
                    Connections[newName] = fromConnections;
                }
                else // Connection to
                {
                    var fromConnections = Connections.Get<GDC.Dictionary>(from);
                    foreach (string to in fromConnections.Keys)
                    {
                        if (to == oldName)
                        {
                            var value = fromConnections[oldName];
                            fromConnections.Remove(oldName);
                            fromConnections[newName] = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Connect two nodes with a line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="interconnectionOffset"></param>
        public void ConnectNode(FlowChartLine line, string from, string to, int interconnectionOffset = 0)
        {
            if (from == to)
                return; // Connect to this
            var connectionsFrom = Connections.Get<GDC.Dictionary>(from);
            if (connectionsFrom != null && connectionsFrom.Contains(to))
                return; // Connection existed

            var connection = new Connection(line, ContentNodes.GetNode<FlowChartNode>(from), ContentNodes.GetNode<FlowChartNode>(to));

            if (connectionsFrom == null)
            {
                connectionsFrom = new GDC.Dictionary() { };
                Connections[from] = connectionsFrom;
            }
            connectionsFrom[to] = connection;
            AfterConnectNode(connection);

            // Check if connection in both ways
            connectionsFrom = Connections.Get<GDC.Dictionary>(to);
            if (connectionsFrom != null)
            {
                var invConnection = connectionsFrom.Get<Connection>(from);
                if (invConnection != null)
                {
                    connection.Offset = interconnectionOffset;
                    invConnection.Offset = interconnectionOffset;
                    connection.Join();
                    invConnection.Join();

                }
            }
        }

        /// <summary>
        /// Break a connection between two node
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>Line that was the connection between the two nodes</returns>
        public FlowChartLine DisconnectNode(string from, string to)
        {
            var connectionsFrom = Connections.Get<GDC.Dictionary>(from);
            var connection = connectionsFrom.Get<Connection>(to);
            if (connection == null)
                return null;

            AfterDisconnectNode(connection);
            if (connectionsFrom.Count == 1)
                Connections.Remove(from);
            else
                connectionsFrom.Remove(to);

            connectionsFrom = Connections.Get<GDC.Dictionary>(to);
            if (connectionsFrom != null)
            {
                var invConnection = connectionsFrom.Get<Connection>(from);
                if (invConnection != null)
                {
                    invConnection.Offset = 0;
                    invConnection.Join();
                }
            }
            return connection.Line;
        }

        /// <summary>
        /// Clear all selection
        /// </summary>
        public void ClearConnections()
        {
            foreach (GDC.Dictionary connectionsFrom in Connections.Values)
                foreach (Connection connection in connectionsFrom.Values)
                    connection.Line.QueueFree();

            Connections.Clear();
        }

        /// <summary>
        /// Return GDC.Array of GDC.Dictionary of connection as such [new GDC.Dictionary(){{"from1", "to1"}}, new GDC.Dictionary(){{"from2", "to2"}}]
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<ConnectionPair> GetConnectionList()
        {
            List<ConnectionPair> connectionList = new List<ConnectionPair>();
            foreach (GDC.Dictionary connectionsFrom in Connections.Values)
            {
                foreach (Connection connection in connectionsFrom.Values)
                {
                    connectionList.Add(new ConnectionPair(connection.FromNode.Name, connection.ToNode.Name));
                }
            }
            return connectionList;
        }
    }
}