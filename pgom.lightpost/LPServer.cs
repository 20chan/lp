﻿using CreamRoll.Routing;
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

        private Template postTemplate;
        private static JsonSerializerOptions options;

        public LPServer(string dbPath) {
            db = new LiteDatabase(dbPath);
            db.Checkpoint();
            posts = db.GetCollection<LitePost>("posts");
            LoadPostTemplate();

            options = new JsonSerializerOptions {
                PropertyNamingPolicy = new CamelBack(),
                PropertyNameCaseInsensitive = true,
            };
        }

        ~LPServer() {
            db.Checkpoint();
        }

        private void LoadPostTemplate() {
            var content = File.ReadAllText("pages/post.html", Encoding.UTF8);
            postTemplate = Template.Parse(content);
        }

        [Get("/posts")]
        public Response GetPosts(Request req) {
            return new JsonResponse(JsonSerializer.Serialize(posts.FindAll(), options));
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

        [Get("/{id}")]
        public async Task<Response> GetPage(Request req) {
            if (!int.TryParse((string)req.Query["id"], out var id)) {
                return ErrorResp("not valid id format");
            }
            var post = posts.FindById(id);
            if (post == null) {
                return ErrorResp("post not found");
            }
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
