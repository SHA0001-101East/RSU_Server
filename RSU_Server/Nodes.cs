using System;
using System.Collections.Generic;

namespace RSU_Server
{
    public class Node
    {
        public Vector3 position;

        //note -1 = neutral;
        public int building;
        public IDictionary<int, Team> teamDictionary = new Dictionary<int, Team>();

        public Node(Vector3 nodesPos1)
        {
            position = nodesPos1;
            teamDictionary.Add(Game.neutralTeam, new Team(Game.neutralTeam, Game.defaultTroopCount));
        }

        public void Update()
        {
            //old idea:
            //          make sure to check if buildings are in battle later...
            //          troops = v + a*t , where a = production on that node, value current calculated value
            //          the idea here is that the troops is not always calculated nice, instead the troops value is stored.

            int team = CurrentTeam(); //remember that if there is no war, this will return an integer that represents the team that owns that node
            if (team > -2)
            {
                teamDictionary[team].troops += teamDictionary[team].production;
                return;
            }

            Battle();
        }

        public void Battle()
        {

        }

        public int CurrentTeam()
        {
            if (teamDictionary.Count == 1)
            {
                foreach (KeyValuePair<int, Team> keyValuePair in teamDictionary) //make it so that it doesnt have to go through each keyvalue pair.
                {
                    return keyValuePair.Key;
                }

                throw new ArgumentNullException("No elements inside teamAndTroopPairs.");
            }

            else return -2; //means there is war
        }
    }

    public class Team
    {
        public int team;
        public float troops;
        public int artillery;
        public int production;

        public Team(int team1, float troops1)
        {
            team = team1;
            troops = troops1;
        }
    }
}
