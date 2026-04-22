using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TaskGrowthApp
{
    /// <summary>
    /// Notion API クライアント。
    /// トークン・DB IDは本来は appsettings.json 等から読み込むこと。
    /// </summary>
    public class NotionClient
    {
        // ─── 設定値（本番では設定ファイル外出し推奨） ───────────────────
        private const string NotionToken      = "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";
        private const string PlayerDatabaseId = "2e17b12565648080841dd3cebb24b06c";
        private const string TaskDatabaseId   = "2e17b1256564808f8b51f2cefbd7d1b2";
        private const string LogDatabaseId    = "2e17b1256564808f8b51f2cefbd7d1b2"; // ★ ログ専用DBが別にある場合は差し替え
        // ──────────────────────────────────────────────────────────────

        private readonly HttpClient _httpClient;

        public NotionClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.notion.com/v1/")
            };
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {NotionToken}");
            _httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
        }

        // =========================================================
        // Player
        // =========================================================

        public async Task<PlayerStatus?> GetPlayerStatusAsync()
        {
            var response = await _httpClient.PostAsync(
                $"databases/{PlayerDatabaseId}/query",
                new StringContent("{}", Encoding.UTF8, "application/json")
            );
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var results = doc.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0) return null;

            var page  = results[0];
            var props = page.GetProperty("properties");

            DateTime? lastEmergencyDate = null;
            var dateProp = props.GetProperty("LastEmergencyCompletedDate").GetProperty("date");
            if (dateProp.ValueKind != JsonValueKind.Null)
            {
                lastEmergencyDate = DateTime.Parse(
                    dateProp.GetProperty("start").GetString()!
                );
            }

            return new PlayerStatus
            {
                PageId      = page.GetProperty("id").GetString()!,
                Level       = props.GetProperty("Level").GetProperty("number").GetInt32(),
                TotalExp    = props.GetProperty("TotalExp").GetProperty("number").GetInt32(),
                CurrentExp  = props.GetProperty("CurrentExp").GetProperty("number").GetInt32(),
                Coin        = props.GetProperty("Coin").GetProperty("number").GetInt32(),
                LastEmergencyCompletedDate = lastEmergencyDate
            };
        }

        public async Task UpdatePlayerStatusAsync(PlayerStatus status)
        {
            object properties;

            if (status.LastEmergencyCompletedDate == null)
            {
                properties = new
                {
                    Level      = new { number = status.Level },
                    TotalExp   = new { number = status.TotalExp },
                    CurrentExp = new { number = status.CurrentExp },
                    Coin       = new { number = status.Coin }
                };
            }
            else
            {
                properties = new
                {
                    Level      = new { number = status.Level },
                    TotalExp   = new { number = status.TotalExp },
                    CurrentExp = new { number = status.CurrentExp },
                    Coin       = new { number = status.Coin },
                    LastEmergencyCompletedDate = new
                    {
                        date = new
                        {
                            start = status.LastEmergencyCompletedDate.Value.ToString("yyyy-MM-dd")
                        }
                    }
                };
            }

            var body = new { properties };
            await _httpClient.PatchAsync($"pages/{status.PageId}", Serialize(body));
        }

        // =========================================================
        // Task CRUD
        // =========================================================

        /// <summary>新規タスクをNotionに追加し、払い出されたPageIdを返す。</summary>
        public async Task<string?> AddTaskAsync(TaskItem task)
        {
            // DueDate がある場合と無い場合で properties を分岐
            object properties;
            if (task.DueDate.HasValue)
            {
                properties = new
                {
                    TaskName = new { title = new[] { new { text = new { content = task.Name } } } },
                    Priority = new { number = task.Priority },
                    IsDone   = new { checkbox = false },
                    DueDate  = new { date = new { start = task.DueDate.Value.ToString("yyyy-MM-dd") } }
                };
            }
            else
            {
                properties = new
                {
                    TaskName = new { title = new[] { new { text = new { content = task.Name } } } },
                    Priority = new { number = task.Priority },
                    IsDone   = new { checkbox = false }
                };
            }

            var body     = new { parent = new { database_id = TaskDatabaseId }, properties };
            var response = await _httpClient.PostAsync("pages", Serialize(body));
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("id").GetString();
        }

        public async Task<List<TaskItem>> GetActiveTasksAsync()
        {
            var query = new
            {
                filter = new
                {
                    property = "IsDone",
                    checkbox = new { equals = false }
                }
            };

            var response = await _httpClient.PostAsync(
                $"databases/{TaskDatabaseId}/query",
                Serialize(query)
            );

            if (!response.IsSuccessStatusCode)
                return new List<TaskItem>();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var list = new List<TaskItem>();
            foreach (var page in doc.RootElement.GetProperty("results").EnumerateArray())
            {
                var props = page.GetProperty("properties");

                var title = props.GetProperty("TaskName").GetProperty("title");
                string name = title.GetArrayLength() > 0
                    ? title[0].GetProperty("text").GetProperty("content").GetString()!
                    : "(no title)";

                int priority = props.GetProperty("Priority").GetProperty("number").GetInt32();

                // ─── DueDate を読み取る ────────────────────────────────
                DateTime? dueDate = null;
                if (props.TryGetProperty("DueDate", out var dueProp))
                {
                    var inner = dueProp.GetProperty("date");
                    if (inner.ValueKind != JsonValueKind.Null)
                    {
                        var startStr = inner.GetProperty("start").GetString();
                        if (startStr != null)
                            dueDate = DateTime.Parse(startStr);
                    }
                }

                list.Add(new TaskItem
                {
                    PageId   = page.GetProperty("id").GetString()!,
                    Name     = name,
                    Priority = priority,
                    DueDate  = dueDate,
                    IsDone   = false
                });
            }

            return list;
        }

        /// <summary>タスクを完了済みとしてマークする（倒したときだけ呼ぶ）。</summary>
        public async Task MarkTaskDoneAsync(TaskItem task)
        {
            if (!task.IsSaved) return;

            var body = new
            {
                properties = new { IsDone = new { checkbox = true } }
            };
            await _httpClient.PatchAsync($"pages/{task.PageId}", Serialize(body));
        }

        /// <summary>タスク内容をNotionに保存する（終了時に呼ぶ）。IsDoneは変更しない。</summary>
        public async Task UpdateTaskAsync(TaskItem task)
        {
            if (!task.IsSaved) return;

            // DueDate がある場合と無い場合で分岐
            // nullのときは date: null を送り、Notionの期限をクリアする
            object properties;
            if (task.DueDate.HasValue)
            {
                properties = new
                {
                    TaskName = new { title = new[] { new { text = new { content = task.Name } } } },
                    Priority = new { number = task.Priority },
                    DueDate  = new { date = new { start = task.DueDate.Value.ToString("yyyy-MM-dd") } }
                };
            }
            else
            {
                properties = new
                {
                    TaskName = new { title = new[] { new { text = new { content = task.Name } } } },
                    Priority = new { number = task.Priority },
                    DueDate  = new { date = (object?)null }
                };
            }

            var body = new { properties };
            await _httpClient.PatchAsync($"pages/{task.PageId}", Serialize(body));
        }

        public async Task DeleteTaskAsync(TaskItem task)
        {
            if (!task.IsSaved) return;

            var body = new { archived = true };
            await _httpClient.PatchAsync($"pages/{task.PageId}", Serialize(body));
        }

        // =========================================================
        // Log
        // =========================================================

        public async Task AddTaskLogAsync(string taskName, int exp, int duration)
        {
            var body = new
            {
                parent     = new { database_id = LogDatabaseId },
                properties = new
                {
                    TaskName = new { title = new[] { new { text = new { content = taskName } } } },
                    Exp      = new { number = exp },
                    Duration = new { number = duration },
                    IsDone   = new { checkbox = true }
                }
            };
            await _httpClient.PostAsync("pages", Serialize(body));
        }

        // =========================================================
        // Helpers
        // =========================================================

        private static StringContent Serialize(object obj)
            => new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
    }
}
