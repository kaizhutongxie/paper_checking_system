using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using paper_checking_web.Models;

namespace paper_checking_web.Hubs
{
    /// <summary>
    /// 查重进度 SignalR Hub
    /// 用于实时推送查重任务进度到前端
    /// </summary>
    public class CheckProgressHub : Hub
    {
        private readonly ILogger<CheckProgressHub> _logger;

        public CheckProgressHub(ILogger<CheckProgressHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 客户端加入任务监控组
        /// </summary>
        public async Task JoinTaskGroup(string taskId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"task-{taskId}");
            _logger.LogInformation("客户端 {ConnectionId} 加入任务组：task-{TaskId}", 
                Context.ConnectionId, taskId);
        }

        /// <summary>
        /// 客户端离开任务监控组
        /// </summary>
        public async Task LeaveTaskGroup(string taskId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"task-{taskId}");
            _logger.LogInformation("客户端 {ConnectionId} 离开任务组：task-{TaskId}", 
                Context.ConnectionId, taskId);
        }

        /// <summary>
        /// 客户端断开连接时清理
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("客户端 {ConnectionId} 断开连接", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }

    /// <summary>
    /// 进度通知服务
    /// </summary>
    public interface IProgressNotificationService
    {
        Task SendProgressAsync(string taskId, CheckProgress progress);
        Task SendCompletedAsync(string taskId, CheckResult result);
        Task SendFailedAsync(string taskId, string errorMessage);
    }

    public class ProgressNotificationService : IProgressNotificationService
    {
        private readonly IHubContext<CheckProgressHub> _hubContext;
        private readonly ILogger<ProgressNotificationService> _logger;

        public ProgressNotificationService(
            IHubContext<CheckProgressHub> hubContext,
            ILogger<ProgressNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// 发送进度更新
        /// </summary>
        public async Task SendProgressAsync(string taskId, CheckProgress progress)
        {
            try
            {
                await _hubContext.Clients.Group($"task-{taskId}")
                    .SendAsync("ReceiveProgress", progress);
                
                _logger.LogDebug("已发送进度更新到任务 {TaskId}: {Percent}%", 
                    taskId, progress.Percent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送进度更新失败：{TaskId}", taskId);
            }
        }

        /// <summary>
        /// 发送完成通知
        /// </summary>
        public async Task SendCompletedAsync(string taskId, CheckResult result)
        {
            try
            {
                await _hubContext.Clients.Group($"task-{taskId}")
                    .SendAsync("ReceiveCompleted", result);
                
                _logger.LogInformation("已发送完成通知到任务 {TaskId}", taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送完成通知失败：{TaskId}", taskId);
            }
        }

        /// <summary>
        /// 发送失败通知
        /// </summary>
        public async Task SendFailedAsync(string taskId, string errorMessage)
        {
            try
            {
                await _hubContext.Clients.Group($"task-{taskId}")
                    .SendAsync("ReceiveFailed", new { TaskId = taskId, ErrorMessage = errorMessage });
                
                _logger.LogError("任务 {TaskId} 失败：{ErrorMessage}", taskId, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送失败通知失败：{TaskId}", taskId);
            }
        }
    }

    /// <summary>
    /// 任务状态管理器
    /// 管理所有查重任务的状态和进度
    /// </summary>
    public class TaskStateManager
    {
        private readonly ConcurrentDictionary<string, TaskStatus> _tasks = new();
        private readonly IProgressNotificationService _notificationService;
        private readonly ILogger<TaskStateManager> _logger;

        public TaskStateManager(
            IProgressNotificationService notificationService,
            ILogger<TaskStateManager> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// 创建新任务
        /// </summary>
        public void CreateTask(string taskId, string fileName)
        {
            var status = new TaskStatus
            {
                TaskId = taskId,
                FileName = fileName,
                IsCompleted = false,
                IsSuccess = false,
                CreatedAt = DateTime.Now,
                Progress = new CheckProgress
                {
                    TaskId = taskId,
                    Percent = 0,
                    CurrentStage = "初始化",
                    Message = "任务已创建"
                }
            };

            _tasks[taskId] = status;
            _logger.LogInformation("创建任务：{TaskId}, 文件：{FileName}", taskId, fileName);
        }

        /// <summary>
        /// 更新任务进度
        /// </summary>
        public async Task UpdateProgressAsync(string taskId, CheckProgress progress)
        {
            if (_tasks.TryGetValue(taskId, out var status))
            {
                status.Progress = progress;
                
                // 发送实时通知
                await _notificationService.SendProgressAsync(taskId, progress);
            }
        }

        /// <summary>
        /// 标记任务完成
        /// </summary>
        public async Task CompleteTaskAsync(string taskId, CheckResult result)
        {
            if (_tasks.TryGetValue(taskId, out var status))
            {
                status.IsCompleted = true;
                status.IsSuccess = true;
                status.CompletedAt = DateTime.Now;
                status.Result = result;
                
                // 发送完成通知
                await _notificationService.SendCompletedAsync(taskId, result);
                
                _logger.LogInformation("任务完成：{TaskId}, 相似度：{Similarity}%", 
                    taskId, result.TotalSimilarity);
            }
        }

        /// <summary>
        /// 标记任务失败
        /// </summary>
        public async Task FailTaskAsync(string taskId, string errorMessage)
        {
            if (_tasks.TryGetValue(taskId, out var status))
            {
                status.IsCompleted = true;
                status.IsSuccess = false;
                status.ErrorMessage = errorMessage;
                status.CompletedAt = DateTime.Now;
                
                // 发送失败通知
                await _notificationService.SendFailedAsync(taskId, errorMessage);
                
                _logger.LogError("任务失败：{TaskId}, 错误：{ErrorMessage}", taskId, errorMessage);
            }
        }

        /// <summary>
        /// 获取任务状态
        /// </summary>
        public TaskStatus? GetTaskStatus(string taskId)
        {
            return _tasks.TryGetValue(taskId, out var status) ? status : null;
        }

        /// <summary>
        /// 获取所有任务列表
        /// </summary>
        public IEnumerable<TaskStatus> GetAllTasks()
        {
            return _tasks.Values;
        }

        /// <summary>
        /// 清理已完成的任务（超过指定时间）
        /// </summary>
        public void CleanupOldTasks(TimeSpan retentionPeriod)
        {
            var cutoff = DateTime.Now - retentionPeriod;
            
            foreach (var kvp in _tasks)
            {
                if (kvp.Value.IsCompleted && 
                    kvp.Value.CompletedAt.HasValue && 
                    kvp.Value.CompletedAt < cutoff)
                {
                    _tasks.TryRemove(kvp.Key, out _);
                    _logger.LogDebug("清理旧任务：{TaskId}", kvp.Key);
                }
            }
        }
    }
}
