using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace RSU_Server
{
    public class Blob
    {
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
        }

        public void Retreat()
        {
            int[] newPath = { path[index - 1], path[index] };
        }
    }
}