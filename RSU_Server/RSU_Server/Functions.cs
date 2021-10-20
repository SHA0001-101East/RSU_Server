using System;
using System.Collections.Generic;
using System.Text;

namespace RSU_Server
{
    public static class Functions
    {
        private static readonly Random random = new Random();

        public static float Range(int range)
        {
            return (float)random.Next(-2147483647, 2147483647) / 2147483647 * range;
        }

        public static float Range(float range)
        {
            return (float)random.Next(-2147483647, 2147483647) / 2147483647 * range;
        }

        public static float Max(float a, float b)
        {
            if (a < b) { return b; }
            else if (a > b) { return a; }
            else if (a == b) { return a; }
            else { Console.WriteLine("A problem was incountered while attempting to compare floats a and b"); return float.NaN; }
        }
    }

    public class Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float i, float j, float k)
        {
            x = i; y = j; z = k;
        }

        public static Vector3 operator +(Vector3 v, Vector3 u)
        {
            Vector3 vector3 = new Vector3(v.x + u.x, v.y + u.y, v.z + u.z);
            return vector3;
        }

        public static Vector3 operator -(Vector3 v, Vector3 u)
        {
            Vector3 vector3 = new Vector3(v.x - u.x, v.y - u.y, v.z - u.z);
            return vector3;
        }
        public static Vector3 operator *(Vector3 v, int k)
        {
            Vector3 vector3 = new Vector3(v.x * k, v.y * k, v.z * k);
            return vector3;
        }

        public static Vector3 operator *(Vector3 v, float k)
        {
            Vector3 vector3 = new Vector3(v.x * k, v.y * k, v.z * k);
            return vector3;
        }
        public static Vector3 operator /(Vector3 v, int k)
        {
            Vector3 vector3 = new Vector3(v.x / k, v.y / k, v.z / k);
            return vector3;
        }

        public static Vector3 operator /(Vector3 v, float k)
        {
            Vector3 vector3 = new Vector3(v.x / k, v.y / k, v.z / k);
            return vector3;
        }

        public static float Distance(Vector3 u, Vector3 v)
        {
            float value = (float)Math.Sqrt((u.x - v.x) * (u.x - v.x) + (u.y - v.y) * (u.y - v.y) + (u.z - v.z) * (u.z - v.z));
            return value;
        }

        public float Magnitude()
        {
            float value = (float)Math.Sqrt(x*x + y*y + z*z);
            return value;
        }

        public Vector3 Normalized()
        {
            return this / Magnitude();
        }
    }
}
