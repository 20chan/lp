using CreamRoll.Routing;
using LiteDB;
using Scriban;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace pgom.lightpost {
    public class LPServer {
        private LiteDatabase db;
        private LiteCollection<LitePost> posts;

        private Template indexTemplate, postTemplate;
        private static JsonSerializerOptions options;

        private string directoryPath = "pages/";

        public LPServer(string dbPath) {
            db = new LiteDatabase(dbPath);
            db.Checkpoint();
            posts = db.GetCollection<LitePost>("posts");

            LoadIndexTemplate();
            LoadPostTemplate();

            options = new JsonSerializerOptions {
                PropertyNamingPolicy = new CamelBack(),
                PropertyNameCaseInsensitive = true,
            };
        }

        ~LPServer() {
            db.Checkpoint();
        }

        private void LoadIndexTemplate() {
            var content = File.ReadAllText(Path.Combine(directoryPath, "index.html"), Encoding.UTF8);
            indexTemplate = Template.Parse(content);
        }

        private void LoadPostTemplate() {
            var content = File.ReadAllText(Path.Combine(directoryPath, "post.html"), Encoding.UTF8);
            postTemplate = Template.Parse(content);
        }

        [Get("/style.css")]
        public Response GetCssFile(Request req) {
            var fullPath = Path.Combine(directoryPath, "style.css");
            if (!File.Exists(fullPath)) {
                return new TextResponse("not found", status: StatusCode.NotFound);
            }

            return new FileResponse(fullPath);
        }

        [Get("/")]
        public async Task<Response> GetIndex(Request req) {
            // LoadIndexTemplate();
            var publicPosts = posts.Find(p => p.Public);
            var content = await indexTemplate.RenderAsync(new { Posts = publicPosts });
            return new HtmlResponse(content);
        }

        [Get("/posts")]
        public Response GetPosts(Request req) {
            return new JsonResponse(JsonSerializer.Serialize(posts.Find(p => p.Public), options));
        }

        [Post("/posts")]
        public async Task<Response> CreatePost(Request req) {
            try {
                var post = await JsonSerializer.DeserializeAsync<LitePost>(req.Body, options);
                post.WrittenDate = DateTime.Now;

                if (string.IsNullOrEmpty(post.Name)) {
                    return ErrorResp("name should not be empty");
                }
                if (string.IsNullOrEmpty(post.Title)) {
                    return ErrorResp("title should not be empty");
                }
                if (string.IsNullOrEmpty(post.Content)) {
                    return ErrorResp("content should not be empty");
                }

                posts.Insert(post);
                db.Checkpoint();
                return new JsonResponse(new JSON {
                    ["success"] = true,
                    ["id"] = post.Id,
                });
            }
            catch (JsonException ex) {
                return ErrorResp("invalid json", ex);
            }
            catch (Exception ex) {
                return ErrorResp("server error", ex, StatusCode.InternalServerError);
            }
        }

        [Get("/{id:int}")]
        public Response RedirectToPost(Request req) {
            if (!int.TryParse((string)req.Query["id"], out var id)) {
                return ErrorResp("not valid id format");
            }
            var post = posts.FindById(id);
            if (post == null || !post.Public) {
                return ErrorResp("post not found");
            }
            return new RedirectResponse(StatusCode.Found, $"{req.Uri.AbsoluteUri.TrimEnd('/')}/{post.Name}");
        }

        [Get("/{id:int}/{name}")]
        public async Task<Response> GetPostPage(Request req) {
            if (!int.TryParse((string)req.Query["id"], out var id)) {
                return ErrorResp("not valid id format");
            }
            var post = posts.FindById(id);
            if (post == null) {
                return ErrorResp("post not found", status: StatusCode.NotFound);
            }
            if (post.Name != (string)req.Query["name"]) {
                return ErrorResp("post not found", status: StatusCode.NotFound);
            }
            // LoadPostTemplate();
            var content = await postTemplate.RenderAsync(new { Post = post });
            return new HtmlResponse(content);
        }

        private static JsonResponse ErrorResp(string message, Exception ex = null, StatusCode status = StatusCode.BadRequest) {
            return new JsonResponse(new JSON {
                ["success"] = false,
                ["message"] = message,
                ["ex"] = ex?.Message ?? "",
            }, status: status);
        }

        class JSON : Dictionary<string, object> {
            public static implicit operator string(JSON json) {
                return JsonSerializer.Serialize(json, options);
            }
        }

        class CamelBack : JsonNamingPolicy {
            public override string ConvertName(string name) {
                return $"{char.ToLower(name[0])}{name.Substring(1)}";
            }
        }
    }
}
