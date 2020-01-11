using CreamRoll.Routing;
using System;
using System.Threading;

namespace pgom.lightpost {
    class Program {
        static void Main(string[] args) {
            if (args.Length == 0) {
                Console.WriteLine("usage: pgom.lightpost {auth}");
                return;
            }
            var post = new LPServer("posts.db", args[0]);
            var statics = new StaticsServer("statics", "index.html");

            var server = new RouteServer(port: 4001);
            server.AppendRoutes(post, "lp");
            server.AppendRoutes(statics, "lp");

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
