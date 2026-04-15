using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace paper_checking_web.Data
{
    /// <summary>
    /// 应用数据库上下文
    /// 存储配置、任务历史、比对库等数据
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// 查重任务历史记录
        /// </summary>
        public DbSet<CheckTaskHistory> CheckTaskHistories { get; set; } = null!;

        /// <summary>
        /// 系统配置
        /// </summary>
        public DbSet<SystemConfig> SystemConfigs { get; set; } = null!;

        /// <summary>
        /// 比对源配置
        /// </summary>
        public DbSet<CompareSourceConfig> CompareSourceConfigs { get; set; } = null!;

        /// <summary>
        /// 用户配置
        /// </summary>
        public DbSet<UserConfig> UserConfigs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置实体
            modelBuilder.Entity<SystemConfig>(entity =>
            {
                entity.HasKey(e => e.Key);
                entity.Property(e => e.Key).HasMaxLength(100);
                entity.Property(e => e.Value).HasMaxLength(2000);
            });

            // 比对源配置
            modelBuilder.Entity<CompareSourceConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200);
            });

            // 任务历史
            modelBuilder.Entity<CheckTaskHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TaskId);
                entity.HasIndex(e => e.CreatedAt);
                entity.Property(e => e.FileName).HasMaxLength(500);
                entity.Property(e => e.ReportPath).HasMaxLength(1000);
            });

            // 用户配置
            modelBuilder.Entity<UserConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(100);
                entity.Property(e => e.MachineCode).HasMaxLength(200);
            });

            // 种子数据
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // 默认比对源
            var defaultSources = new List<CompareSourceConfig>
            {
                new() { Id = 1, Name = "中国学术期刊网络出版总库", IsEnabled = true, SortOrder = 1 },
                new() { Id = 2, Name = "中国博士学位论文全文数据库", IsEnabled = true, SortOrder = 2 },
                new() { Id = 3, Name = "中国优秀硕士学位论文全文数据库", IsEnabled = true, SortOrder = 3 },
                new() { Id = 4, Name = "中国重要会议论文全文数据库", IsEnabled = false, SortOrder = 4 },
                new() { Id = 5, Name = "互联网资源", IsEnabled = true, SortOrder = 5 },
                new() { Id = 6, Name = "自建库", IsEnabled = false, SortOrder = 6 }
            };
            
            modelBuilder.Entity<CompareSourceConfig>().HasData(defaultSources);

            // 默认系统配置
            var defaultConfigs = new List<SystemConfig>
            {
                new() { Key = "UploadPath", Value = "/data/uploads" },
                new() { Key = "ReportPath", Value = "/data/reports" },
                new() { Key = "TempPath", Value = "/data/temp" },
                new() { Key = "MaxFileSize_MB", Value = "100" },
                new() { Key = "AllowedExtensions", Value = ".doc,.docx,.pdf,.txt" },
                new() { Key = "SimilarityThreshold", Value = "30" },
                new() { Key = "WindowsServiceUrl", Value = "http://localhost:5001" },
                new() { Key = "TaskRetention_Days", Value = "30" }
            };
            
            modelBuilder.Entity<SystemConfig>().HasData(defaultConfigs);
        }
    }

    /// <summary>
    /// 查重任务历史记录
    /// </summary>
    public class CheckTaskHistory
    {
        public int Id { get; set; }
        public string TaskId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public decimal? TotalSimilarity { get; set; }
        public bool? IsPassed { get; set; }
        public string? ReportPath { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// 系统配置项
    /// </summary>
    public class SystemConfig
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 比对源配置
    /// </summary>
    public class CompareSourceConfig
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public int SortOrder { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// 用户配置
    /// </summary>
    public class UserConfig
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? MachineCode { get; set; }
        public string? LicenseKey { get; set; }
        public DateTime? LicenseExpiry { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
