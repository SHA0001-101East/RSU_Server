using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RSU_Server
{
    public static class Terminal
    {
        static Server server;

        static void Main(string[] args)
        {
            Console.WriteLine("Creating Local Server...");
            server = new Server();
            Thread thread = new Thread(new ThreadStart(server.CreateServer));
            thread.Start();

            Console.WriteLine("..................AWATING FOR COMMAND..................");
            string command = ValidateString(Console.ReadLine());
            while (true)
            {
                RunCommand(command);
                command = ValidateString(Console.ReadLine());
            }
        }

        static string ValidateString(string argument)
        {
            return argument.ToLower();
        }

        static void RunCommand(string command)
        {
            if (command == "stop") //put commands here
            {
                Environment.Exit(0);
            }

            else if (command == "start")
            {
                server.StartGame();
            }

            else
            {
                Console.WriteLine($"No command by {command} exists.");
            }
        }
    }
}
