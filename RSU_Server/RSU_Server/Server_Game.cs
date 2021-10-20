using System;
using System.Collections.Generic;
using System.Linq;

namespace RSU_Server
{
    public class Server_Game
    {
        //things for server
        public Server server;
        public Clock clock;

        //map and general game stuff
        public const int n_neutral = 0, n_factory = 1, n_capital = 2, n_powerplant = 3, n_fort = 4, n_artillery = 5;
        public const int baseCapitalProd = 2, baseFactoryProd = 1;
        public const int factoryCost = 500, fortCost = 400, powerplantCost = 2500, artilleryCost = 4000;
        public static int[] buildingCost = { 0, factoryCost, 0, powerplantCost, fortCost, artilleryCost };
        public const int endOfPath = -1;
        public const float defaultTroopCount = 25f;
        public const float defaultStartingTroops = 500f;
        public const int neutralTeam = -1;
        public int uniqueBlobStartingIndex = 0;
        public int nodesInRow = 10; public float radiusOfNode = 5; //probably should make this variable something to collect in the future
        public float distanceBetweenNodes = 3f; public float shakeFactor = 1f; public float startingXValue = 0f; public float startingYValue = 0f;

        //variables for game
        public int[][] connections;
        public Server_Nodes[] nodes;
        public List<GameAction> gameActions = new List<GameAction>();
        public IDictionary<int, Blob> blobIDPairs = new Dictionary<int, Blob>();

        public Server_Game(Server server1)
        {
            server = server1;
        }

        public void CreateMap() //this idea is by mumisa
        {
            Console.WriteLine("Creating Map.");

            nodes = new Server_Nodes[nodesInRow * nodesInRow];

            startingYValue = -(nodesInRow * distanceBetweenNodes) / 2;
            startingXValue = -(nodesInRow * distanceBetweenNodes) / 2;

            for (int i = 0; i < nodesInRow; i++)
            {
                for (int p = 0; p < nodesInRow; p++)
                {
                    nodes[i + p * nodesInRow] = new Server_Nodes(new Vector3(startingXValue + distanceBetweenNodes * i, startingYValue + distanceBetweenNodes * p, 0));
                }
            }

            //translates each node by random variables
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector3 adjustVector = new Vector3(Functions.Range(shakeFactor), Functions.Range(shakeFactor), 0);
                nodes[i].position = nodes[i].position + adjustVector;
            }

            Console.WriteLine("Map Created!");
            InitialiseConnectionsArray();
        }


        public void InitialiseConnectionsArray()
        {
            connections = new int[nodes.Length][];
            List<int> connects = new List<int>();

            float distanceToNodes;
            for (int i = 0; i < nodes.Length; i++)
            {
                for (int j = 0; j < nodes.Length; j++)
                {
                    distanceToNodes = Vector3.Distance(nodes[i].position, nodes[j].position);
                    if (distanceToNodes < radiusOfNode && distanceToNodes != 0)
                    {
                        connects.Add(j);
                    }
                }

                connections[i] = connects.ToArray();
                connects.Clear();
            }

            //creates connections for any node with connections less than 3
            for (int i = 0; i < nodes.Length; i++)
            {
                if (connections[i].Length < 3)
                {
                    List<int> sortList = new List<int>();
                    for (int j = 0; j < nodes.Length; j++)
                    {
                        sortList.Add(j);
                    }

                    sortList = sortList.OrderBy(x => Vector3.Distance(nodes[i].position, nodes[x].position)).ToList();

                    connections[i] = new int[3];

                    for (int k = 1; k < 4; k++)
                    {
                        connections[i][k - 1] = sortList[k];
                        List<int> list = new List<int>(connections[sortList[k]]);
                        list.Add(i);
                        connections[sortList[k]] = list.ToArray();
                    }

                }
            }
        }

        public void StartGame()
        {
            clock = new Clock(this);
            clock.StartInternalClock();
        }

        public void UpdateGame()
        {   
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Update();
            }

            for (int i = 0; i < gameActions.Count; i++)
            {
                if (DoGameAction(gameActions[i]))
                {
                    Server_Client[] clients = ClientsInGameActionFOV(gameActions[i]);
                    foreach (Server_Client client in clients)
                    {
                        client.SendGameAction(gameActions[i], clock.ticksSinceStartUp);
                    }
                }
            }

            List<int> removeBlobs = new List<int>();
            if (blobIDPairs.Count > 0)  //this should be in it's own frame
            {
                foreach (KeyValuePair<int, Blob> mBlobPair in blobIDPairs)
                {
                    if (mBlobPair.Value.Move())
                    {
                        removeBlobs.Add(mBlobPair.Key);
                        nodes[mBlobPair.Value.path[mBlobPair.Value.index]].teamDictionary[mBlobPair.Value.team].troops += mBlobPair.Value.troops;
                    }
                }

                for (int i = 0; i < removeBlobs.Count; i++)
                {
                    blobIDPairs.Remove(removeBlobs[i]);
                }
            }
        }

        public void UpdateGameObjects()
        {
            throw new NotImplementedException();
        }

        public void QueueGameAction(GameAction gameAction)
        {
            lock (gameActions)
            {
                gameActions.Add(gameAction);
            }
        }

        public bool DoGameAction(GameAction gameAction)
        {
            switch (gameAction.action)
            {
                case GameAction.buildingAction:
                    {
                        Server_Nodes mNode = nodes[gameAction.node];
                        lock (mNode)
                        {
                            int team = mNode.CurrentTeam();
                            if (team == -2) { return false; } //check if team is in war
                            if (team != gameAction.team) { return false; } //possible validation here???
                            if (mNode.teamDictionary[team].troops < buildingCost[gameAction.building]) { return false; } //not enough troops lol

                            //if we have enough troops
                            mNode.teamDictionary[team].troops = mNode.teamDictionary[team].troops - buildingCost[gameAction.building];
                            mNode.building = gameAction.building; //update building

                            //update node for itself and its neighbours
                            UpdateNodeNSurrounding(gameAction.node);
                        }
                        break;
                    }

                case GameAction.moveAction:
                    {
                        Server_Nodes mNode = nodes[gameAction.node];
                        lock (mNode)
                        {
                            //checks if node has enough troops
                            if (mNode.teamDictionary[gameAction.team].troops < gameAction.troops)
                            {
                                //not enough troops exist
                                return false;
                            }

                            //checks if the node it wants to move to is a valid node
                            int[] path = FindPath(gameAction.node, gameAction.destinationNode, gameAction.team);
                            if (path == null)
                            {
                                return false; //troops arent able to move there
                            }

                            int blobID = GenerateBlobID();
                            gameAction.blobID = blobID;
                            blobIDPairs.Add(blobID, new Blob(this, path, gameAction.troops, gameAction.team)); //creates blob
                                                                                                               //make sure to send the player the blob ID
                            mNode.teamDictionary[gameAction.team].troops = mNode.teamDictionary[gameAction.team].troops - gameAction.troops;
                        }
                        break;
                    }

                case GameAction.retreatAction:
                    {
                        //check team XDDDDDD
                        if (!blobIDPairs.ContainsKey(gameAction.blobID))
                        {
                            return false; //invalid blob ID
                        }

                        blobIDPairs[gameAction.blobID].Retreat();
                        break;
                    }
            }
            return true;
        }

        public void Spawn(int spawnInt, int team)
        {
            if (nodes[spawnInt].teamDictionary.ContainsKey(team))
            {
                Console.WriteLine("Error, key value pair already exists: " + team);
            }
            else
            {
                nodes[spawnInt].teamDictionary.Remove(-2);
                nodes[spawnInt].teamDictionary.Add(team, new Team(team, defaultStartingTroops));
                nodes[spawnInt].building = n_capital;
                UpdateNodeNSurrounding(spawnInt);
            }
        }

        public void UpdateNodeNSurrounding(int nUpdate)
        {
            UpdateNode(nUpdate);
            for (int i = 0; i < connections[nUpdate].Length; i++)
            {
                UpdateNode(connections[nUpdate][i]);
            }
        }

        public void UpdateNode(int nUpdate)
        {
            int team = nodes[nUpdate].CurrentTeam();
            if (team > -2) //means no war yayyyy!!!!!
            {
                int prod = 0;
                int artillery = 0;

                if (nodes[nUpdate].building == n_factory) { prod += baseFactoryProd; }
                else if (nodes[nUpdate].building == n_capital) { prod += baseCapitalProd; }

                for (int i = 0; i < connections[nUpdate].Length; i++)
                {
                    Server_Nodes mBuilding = nodes[connections[nUpdate][i]];
                    if (mBuilding.CurrentTeam() != team) { continue; }
                    if (prod > 0 && mBuilding.building == 3) { prod += 2; }
                    else if (mBuilding.building == 5) { artillery++; }
                }

                nodes[nUpdate].teamDictionary[team].production = prod;
                nodes[nUpdate].teamDictionary[team].artillery = artillery;
            }
        }

        public int[] FindPath(int startingNode, int destinationNode, int team)
        {
            if (nodes[startingNode].CurrentTeam() != team || nodes[destinationNode].CurrentTeam() != team)
            {
                return null;
            }

            List<int> unvisited = new List<int>();
            List<int> visited = new List<int>();
            int[] previousVertex = new int[nodes.Length];
            float[] shortestDistance = new float[nodes.Length];

            //intialising components
            for (int i = 0; i < shortestDistance.Length; i++)
            {
                shortestDistance[i] = 9999999f; //arbitrarily large value pepelaugh
            }

            for (int i = 0; i < previousVertex.Length; i++)
            {
                previousVertex[i] = endOfPath;
            }

            shortestDistance[destinationNode] = 0f;
            unvisited.Add(destinationNode);

            //start of algorithm

            while (unvisited.Count > 0)
            {
                //A* search
                unvisited.Sort((x, y) => (shortestDistance[x] + Vector3.Distance(nodes[x].position, nodes[startingNode].position)).
                                         CompareTo(shortestDistance[y] + Vector3.Distance(nodes[y].position, nodes[startingNode].position)));
                //A* search is over

                int currentNode = unvisited[0];
                unvisited.Remove(currentNode);

                //check looking node neighbours

                for (int i = 0; i < connections[currentNode].Length; i++)
                {
                    int neighbour = connections[currentNode][i];
                    if (visited.Contains(neighbour) == false && nodes[neighbour].CurrentTeam() == team)
                    {
                        if (unvisited.Contains(neighbour) == false)
                        {
                            unvisited.Add(neighbour);
                        }

                        float distance = Vector3.Distance(nodes[currentNode].position, nodes[neighbour].position);
                        if (distance + shortestDistance[currentNode] < shortestDistance[neighbour])
                        {
                            shortestDistance[neighbour] = distance + shortestDistance[currentNode];
                            previousVertex[neighbour] = currentNode;
                        }
                    }
                }

                if (visited.Contains(startingNode) == true) //i believe this is the end condition and should be editted
                {
                    unvisited.Clear();
                    unvisited.TrimExcess();
                    break;
                }

                if (visited.Contains(currentNode))
                {
                    throw new Exception(currentNode.ToString() + " is already an element of visited");
                }

                visited.Add(currentNode);
            }

            //creates an array for the path from startig node to destination node and returns it.

            List<int> path = new List<int>();
            int nNode = startingNode;

            if (previousVertex[startingNode] == endOfPath) //no path was found
            {
                return null;
            }

            while (previousVertex[nNode] != destinationNode)
            {
                path.Add(nNode);
                nNode = previousVertex[nNode];
            }

            path.Add(nNode);
            path.Add(destinationNode);
            int[] finalPath = path.ToArray();
            return finalPath;
        }

        public int GenerateBlobID()
        {
            uniqueBlobStartingIndex++;
            return uniqueBlobStartingIndex;
        }

        public int[] GetNodesInFOV(int team)
        {
            List<int> connectedNodes = new List<int>();

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].CurrentTeam() == team && !connectedNodes.Contains(i))
                {
                    connectedNodes.Add(i);
                    for (int j = 0; j < connections[i].Length; j++)
                    {
                        if (!connectedNodes.Contains(connections[i][j]))
                        {
                            connectedNodes.Add(connections[i][j]);
                        }
                    }
                }
            }
            return connectedNodes.ToArray();
        }

        public int[] GetBlobsInFOV(int team)
        {
            List<int> list = new List<int>();
            foreach (KeyValuePair<int, Blob> keyValuePair in blobIDPairs)
            {
                if (nodes[keyValuePair.Value.path[keyValuePair.Value.index]].CurrentTeam() == team || nodes[keyValuePair.Value.path[keyValuePair.Value.index - 1]].CurrentTeam() == team)
                {
                    if (!list.Contains(keyValuePair.Value.path[keyValuePair.Value.index]))
                        list.Add(keyValuePair.Value.path[keyValuePair.Value.index]);
                }
            }
            return list.ToArray();
        }

        public Server_Client[] ClientsInGameActionFOV(GameAction gameAction)
        {
            List<Server_Client> clients = new List<Server_Client>();
            List<int> teams = new List<int>();
            teams.Add(gameAction.team);
            switch (gameAction.action)
            {
                case GameAction.buildingAction:
                    {
                        for (int i = 0; i < connections[gameAction.node].Length; i++)
                        {
                            int _team = nodes[connections[gameAction.node][i]].CurrentTeam();
                            if (_team > -2 && !teams.Contains(_team)) teams.Add(_team);
                        }
                        break;
                    }

                case GameAction.retreatAction:
                case GameAction.moveAction:
                    {
                        Blob blob = blobIDPairs[gameAction.blobID];
                        for (int i = 0; i < 2; i++)
                        {
                            int _team = nodes[blob.path[blob.index - i]].CurrentTeam();
                            if (_team > -2 && !teams.Contains(_team)) teams.Add(_team);
                        }
                        break;
                    }
            }
            for (int i = 0; i < teams.Count; i++)
            {
                clients.AddRange(server.GetClientsInTeam(teams[i]));
            }

            foreach (Server_Client client in server.clients)
            {
                if (client.playerState == Server.GameState_Intermission && !clients.Contains(client)) clients.Add(client);
            }
            return clients.ToArray();
        }
    }

    public class GameAction
    {
        public const int buildingAction = 1;
        public const int moveAction = 2;
        public const int retreatAction = 3;

        public int action;
        public int team;
        public int node;
        public int destinationNode;
        public int building;
        public float troops;
        public int blobID;

        //retreat stuff
        //possible hashing

        public void Build(int team1, int node1, int building1)
        {
            action = buildingAction;
            team = team1;
            node = node1;
            building = building1;
        }
        public void Move(int team1, float troops1, int node1, int destination1)
        {
            action = buildingAction;
            team = team1;
            node = node1;
            destinationNode = destination1;
            troops = troops1;
        }

        public void Retreat(int team1, int blobID1)
        {
            action = retreatAction;
            team = team1;
            blobID = blobID1;
        }
    }

}