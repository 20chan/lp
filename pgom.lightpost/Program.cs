using CreamRoll.Routing;
using System;
using System.Threading;

namespace pgom.lightpost {
    class Program {
        static void Main(string[] args) {
            var post = new LPServer();
            var frontend = new FrontEndServer("FrontEnd", "index.html");

            var server = new RouteServer<LPServer>(post);
            server.AppendRoutes(frontend);

            StartDaemon(server);
        }

        public static void StartDaemon<T>(RouteServer<T> server) {
            var waiter = new ManualResetEvent(false);
            Console.CancelKeyPress += (o, e) => {
                e.Cancel = true;
                waiter.Set();
            };

            server.StartAsync();
            Console.WriteLine("pgom.lightpost started.. press ctrl+c to stop");

            waiter.WaitOne();
            server.Stop();
        }
    }
}
