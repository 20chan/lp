using CreamRoll.Routing;
using System;
using System.Threading;

namespace pgom.lightpost {
    class Program {
        static void Main(string[] args) {
            var post = new LPServer("posts.db");
            var statics = new StaticsServer("statics", "index.html");

            var server = new RouteServer();
            server.AppendRoutes(post, "/lp");
            server.AppendRoutes(statics);

            StartDaemon(server);
        }

        public static void StartDaemon(RouteServer server) {
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
