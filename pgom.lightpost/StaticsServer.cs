﻿using CreamRoll.Routing;
using System.IO;

namespace pgom.lightpost {
    public class StaticsServer {
        public string DirectoryPath;
        public string Index;

        public StaticsServer(string path, string index) {
            DirectoryPath = path;
            Index = index;
        }

        [Get("/{f0}")]
        [Get("/{f0}/{f1}")]
        [Get("/{f0}/{f1}/{f2}")]
        public Response RouteFile(Request req) {
            var path = req.AbsolutePathWithoutBaseUri.Substring(1);
            if (string.IsNullOrEmpty(path)) {
                path = Index;
            }

            var fullPath = Path.Combine(DirectoryPath, path);
            if (!File.Exists(fullPath)) {
                return new TextResponse("file not found", status: StatusCode.NotFound);
            }

            return new FileResponse(fullPath);
        }
    }
}
