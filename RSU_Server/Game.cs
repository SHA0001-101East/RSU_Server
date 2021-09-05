using System;
using System.Collections.Generic;
using System.Linq;

namespace RSU_Server
{
    public class Game
    {
        public Clock clock;
        public int team;

        public const int n_neutral = 0, n_factory = 1, n_capital = 2, n_powerplant = 3, n_fort = 4, n_artillery = 5;
        public const int baseCapitalProd = 2, baseFactoryProd = 1;
        public const int factoryCost = 500, fortCost = 400, powerplantCost = 2500, artilleryCost = 4000;
        public static int[] buildingCost = { 0, factoryCost, 0, powerplantCost, fortCost, artilleryCost };
        public const int endOfPath = -1;
        public const float defaultTroopCount = 25f;
        public const float defaultStartingTroops = 500f;
        public const int neutralTeam = -1;
        public int uniqueBlobIndex = 0;

        public int nodesInRow = 10; public float radiusOfNode = 5; //probably should make this variable something to collect in the future
        public float distanceBetweenNodes = 3f; public float shakeFactor = 1f; private float startingXValue = 0f; private float startingYValue = 0f;
        public int count;

        public Node[] nodes;
        public bool[,] adjacencyMatrix;
        public int[][] connections;
        public List<GameAction> gameActions = new List<GameAction>();
        public IDictionary<int, Blob> blobIDPairs = new Dictionary<int, Blob>();

        public void CreateMap()
        {
            Console.WriteLine("Creating Map.");

            count = nodesInRow * nodesInRow;
            nodes = new Node[nodesInRow * nodesInRow];

            startingYValue = -(nodesInRow * distanceBetweenNodes) / 2;
            startingXValue = -(nodesInRow * distanceBetweenNodes) / 2;

            for (int i = 0; i < nodesInRow; i++)
            {
                for (int p = 0; p < nodesInRow; p++)
                {
                    nodes[i+p*nodesInRow] = new Node(new Vector3(startingXValue + distanceBetweenNodes * i, startingYValue + distanceBetweenNodes * p, 0));
                }
            }

            //translates each node by random variables
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector3 nodeTransformer = new Vector3(Functions.Range(shakeFactor), Functions.Range(shakeFactor), 0) ;
                nodes[i].position = nodes[i].position + nodeTransformer;
            }

            Console.WriteLine("Map Created!");
            AdjMatrix();
            InitialiseConnectionsArray();
        }

        #region Initialisation Stuff

        void InitialiseConnectionsArray()
        {
            connections = new int[count][];
            List<int> list;
            for (int i = 0; i < count; i++)
            {
                list = new List<int>();

                for (int j = 0; j < count; j++)
                {
                    if (adjacencyMatrix[i, j])
                    {
                        list.Add(j);
                    }
                }
                connections[i] = list.ToArray();
            }
        }

        void AdjMatrix()
        {
            bool[,] adjMatrix = new bool[nodes.Length, nodes.Length];
            float distanceToNodes;

            for (int i = 0; i < nodes.Length; i++)
            {
                for (int j = 0; j < nodes.Length; j++)
                {
                    distanceToNodes = Vector3.Distance(nodes[i].position, nodes[j].position);

                    if (distanceToNodes < radiusOfNode && distanceToNodes != 0)
                    {
                        adjMatrix[i, j] = true;
                    }
                }
            }

            //creates connections for any node with connections less than 3
            for (int w = 0; w < nodes.Length; w++)
            {
                float connections = 0;

                for (int k = 0; k < nodes.Length; k++)
                {
                    if (adjMatrix[w, k] == true) { connections += 1; }
                }

                if (connections < 3)
                {
                    List<int> sortList = new List<int>();

                    for (int i = 0; i < nodes.Length; i++)
                    {
                        sortList.Add(i);
                    }

                    sortList = sortList.OrderBy(x => Vector3.Distance(nodes[w].position, nodes[x].position)).ToList();

                    for (int kebab = 1; kebab < 4; kebab++)
                    {
                        adjMatrix[w, sortList[kebab]] = true;
                        adjMatrix[sortList[kebab], w] = true;
                    }

                }
            }

            adjacencyMatrix = adjMatrix;
        }

        #endregion

        public void StartGame()
        {
            clock = new Clock(this);
            clock.StartInternalClock();
        }

        public void UpdateGame()
        {
            //updates troops
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Update();
            }

            //checks if there are any actions to do lol

            if (gameActions.Count == 0)
            {
                return;
            }

            else if (gameActions.Count > 0) //if there are actions...
            {
                GameAction mGameAction;
                for (int i = 0; i < gameActions.Count; i++)
                {
                    mGameAction = gameActions[i];
                    switch (mGameAction.action)
                    {
                        case GameAction.buildingAction:
                            {
                                Node mNode = nodes[mGameAction.node];
                                int team = mNode.CurrentTeam();
                                if (team == -2) { break; } //check if team is in war
                                if (team != mGameAction.team) { break; } //possible validation here???
                                if (mNode.teamDictionary[team].troops < buildingCost[mGameAction.building]) { break; } //not enough troops lol

                                //if we have enough troops
                                mNode.teamDictionary[team].troops = mNode.teamDictionary[team].troops - buildingCost[mGameAction.building];
                                mNode.building = mGameAction.building; //update building

                                //update node for itself and its neighbours
                                UpdateNodeNSurrounding(mGameAction.node);
                                break;
                            }

                        case GameAction.moveAction:
                            {
                                Node mNode = nodes[mGameAction.node];

                                //checks if node has enough troops
                                if (mNode.teamDictionary[mGameAction.team].troops < mGameAction.troops)
                                {
                                    //not enough troops exist
                                    break;
                                }

                                //checks if the node it wants to move to is a valid node
                                int[] path = FindPath(mGameAction.node, mGameAction.destinationNode, mGameAction.team);
                                if (path == null)
                                {
                                    break; //troops arent able to move there
                                }

                                int blobID = GenerateBlobID();
                                blobIDPairs.Add(blobID, new Blob(this, path, mGameAction.troops, mGameAction.team)); //creates blob
                                //make sure to send the player the blob ID
                                mNode.teamDictionary[mGameAction.team].troops = mNode.teamDictionary[mGameAction.team].troops - mGameAction.troops;
                                break;
                            }

                        case GameAction.retreatAction:
                            {
                                //check team XDDDDDD
                                if (!blobIDPairs.ContainsKey(mGameAction.blobID))
                                {
                                    break; //invalid blob ID
                                }

                                blobIDPairs[mGameAction.blobID].Retreat();
                                break;
                            }
                    }
                }

                //more code
            }

            //update troops
            //update buildings
            //update battles
        }

        public void UpdateGameObjects()
        {
            
        }

        public void Spawn(int spawnInt, int team)
        {
            Console.WriteLine("Spawning player...");
            SpawnPlayer(spawnInt, team);
        }

        public void SpawnPlayer(int spawnInt, int team)
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
                    Node mBuilding = nodes[connections[nUpdate][i]];
                    if (mBuilding.CurrentTeam() != team) { continue; }
                    if (prod > 0 && mBuilding.building == 3) { prod += 2; }
                    else if (mBuilding.building == 5) { artillery++; }
                }

                nodes[nUpdate].teamDictionary[team].production = prod;
                nodes[nUpdate].teamDictionary[team].artillery = artillery;
            }
        }

        public int GenerateBlobID()
        {
            uniqueBlobIndex++;
            return uniqueBlobIndex;
        }

        int[] FindPath(int startingNode, int destinationNode, int team)
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
        public int troops;
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
        public void Move(int team1, int troops1, int node1, int destination1)
        {
            action = buildingAction;
            team = team1;
            node = node1;
            destinationNode = destination1;
            troops = troops1;
        }

        public void Retreat(int team1, int blobID1)
        {
            team = team1;
            blobID = blobID1;
        }
    }
}
