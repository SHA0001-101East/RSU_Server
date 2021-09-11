using System;

namespace RSU_Server
{
    public class Blob
    {
        public static float uncertaintyThreshhold = 0.1f;

        public Game game;
        public Vector3 position;
        public float speed;
        public int[] path;
        public float troops;
        public int team;
        public int index = 1;

        //instantiate blob class with team, path array and speed
        public Blob(Game game1, int[] path1, float troops1, int team1)
        {
            game = game1;
            path = path1;
            troops = troops1;
            team = team1;

            //set speed
        }

        public void Retreat()
        {
            int[] newPath = { path[index - 1], path[index] };
        }

        public bool Move()
        {
            Vector3 dir = game.nodes[path[index]].position - game.nodes[path[index - 1]].position;
            position += dir.Normalized() * speed; //moves the blob

            //check if the blob is close enough to the node
            Vector3 delta = position - game.nodes[path[index]].position;
            
            if (delta.Magnitude() < uncertaintyThreshhold)
            {
                if (index == path.Length - 1) { return true; }
                else if (index < path.Length - 1)
                {
                    position = game.nodes[path[index]].position;
                    index++;
                }
                else { throw new Exception("Path array out of bounds."); }
            }
            return false;
        }
    }
}