using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using paper_checking_web.Models;

namespace paper_checking_web.Services
{
    /// <summary>
    /// RTF 报告生成服务 - 纯原生实现
    /// 生成与原版 WinForms 应用格式一致的查重报告
    /// </summary>
    public interface IReportGenerator
    {
        Task<string> GenerateRtfReportAsync(CheckResult result, CheckTask task);
        Task<string> GenerateRtfAsync(CheckResult result, string outputPath);
        Task<string> GeneratePdfAsync(CheckResult result, string outputPath);
        Task<string> GenerateWordAsync(CheckResult result, string outputPath);
    }

    public class ReportGenerator : IReportGenerator
    {
        private readonly ILogger<ReportGenerator> _logger;

        public ReportGenerator(ILogger<ReportGenerator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 生成 RTF 格式报告 (适配 PaperCheckService 调用)
        /// </summary>
        public async Task<string> GenerateRtfReportAsync(CheckResult result, CheckTask task)
        {
            var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "reports");
            if (!Directory.Exists(reportsDir))
            {
                Directory.CreateDirectory(reportsDir);
            }

            var outputPath = Path.Combine(reportsDir, $"{result.TaskId}.rtf");
            return await GenerateRtfAsync(result, outputPath);
        }

        /// <summary>
        /// 生成 RTF 格式报告
        /// </summary>
        public Task<string> GenerateRtfAsync(CheckResult result, string outputPath)
        {
            _logger.LogInformation("开始生成 RTF 报告：{Path}", outputPath);

            var rtfContent = BuildRtfContent(result);
            
            File.WriteAllText(outputPath, rtfContent, Encoding.UTF8);
            
            _logger.LogInformation("RTF 报告生成完成：{Path}", outputPath);
            
            return Task.FromResult(outputPath);
        }

        /// <summary>
        /// 构建 RTF 内容
        /// </summary>
        private string BuildRtfContent(CheckResult result)
        {
            var sb = new StringBuilder();
            
            // RTF 头部
            sb.AppendLine(@"{\rtf1\ansi\deff0");
            sb.AppendLine(@"{\fonttbl");
            sb.AppendLine(@"{\f0\fswiss\fprq2\fcharset134 SimHei;}");
            sb.AppendLine(@"{\f1\fswiss\fprq2\fcharset134 Microsoft YaHei;}");
            sb.AppendLine(@"{\f2\froman\fprq2\fcharset134 Songti SC;}");
            sb.AppendLine(@"}");
            sb.AppendLine(@"{\colortbl;\red0\green0\blue0;\red255\green0\blue0;\red0\green128\blue0;}");
            sb.AppendLine();

            // 标题
            sb.AppendLine(@"\f0\fs48\b 论文查重检测报告\b0\par");
            sb.AppendLine(@"\par");
            
            // 基本信息
            sb.AppendLine(@"\f1\fs24 报告编号：" + result.TaskId + @"\par");
            sb.AppendLine(@"检测时间：" + result.CheckTime.ToString("yyyy-MM-dd HH:mm:ss") + @"\par");
            sb.AppendLine(@"总文字复制比：" + result.TotalSimilarity.ToString("F1") + @"%\par");
            sb.AppendLine(@"检测结果：" + (result.IsPassed ? @"\cf3 通过\cf0" : @"\cf2 未通过\cf0") + @"\par");
            sb.AppendLine(@"\par");

            // 统计信息
            if (result.Statistics != null)
            {
                sb.AppendLine(@"\b 统计信息\b0\par");
                sb.AppendLine(@"总字符数：" + result.Statistics.TotalCharacters + @"\par");
                sb.AppendLine(@"章节总数：" + result.Statistics.TotalSections + @"\par");
                sb.AppendLine(@"重复章节数：" + result.Statistics.MatchedSections + @"\par");
                sb.AppendLine(@"最高相似度：" + result.Statistics.MaxSimilarity.ToString("F1") + @"%\par");
                sb.AppendLine(@"最低相似度：" + result.Statistics.MinSimilarity.ToString("F1") + @"%\par");
                sb.AppendLine(@"\par");
            }

            // 章节详情
            sb.AppendLine(@"\b 各章节检测结果\b0\par");
            sb.AppendLine(@"\par");
            
            if (result.Details != null)
            {
                foreach (var detail in result.Details)
                {
                    sb.AppendLine(@"\f1\fs22 " + detail.SectionName + @": ");
                    sb.AppendLine(@"复制比 " + detail.Similarity.ToString("F1") + @"%\par");
                    
                    if (detail.MatchedSources != null && detail.MatchedSources.Count > 0)
                    {
                        sb.AppendLine(@"\li360 主要来源:\par");
                        foreach (var source in detail.MatchedSources)
                        {
                            sb.AppendLine(@"\li720 \bullet  " + source.SourceName + 
                                        @" (" + source.Similarity.ToString("F1") + @"%)\par");
                            if (!string.IsNullOrEmpty(source.MatchedText))
                            {
                                sb.AppendLine(@"\li720 \i " + source.MatchedText + @"\i0\par");
                            }
                        }
                    }
                    sb.AppendLine(@"\par");
                }
            }

            // 结论
            sb.AppendLine(@"\par");
            sb.AppendLine(@"\b 检测结论\b0\par");
            sb.AppendLine(@"\par");
            sb.AppendLine(@"\f2\fs22 本文档总文字复制比为 " + result.TotalSimilarity.ToString("F1") + 
                          @"%，" + (result.IsPassed ? "符合学术规范要求。" : "超出学术规范允许范围，建议修改。") + @"\par");
            
            // 页脚
            sb.AppendLine(@"\par");
            sb.AppendLine(@"\f1\fs18\i 本报告由论文查重系统自动生成\i0\par");
            
            // RTF 结尾
            sb.AppendLine(@"}");
            
            return sb.ToString();
        }

        /// <summary>
        /// 生成 PDF 格式报告
        /// </summary>
        public async Task<string> GeneratePdfAsync(CheckResult result, string outputPath)
        {
            _logger.LogInformation("开始生成 PDF 报告：{Path}", outputPath);

            try
            {
                // 注意：实际项目中需要 iText7 库支持
                // 这里暂时返回一个提示文件
                var tempPath = Path.ChangeExtension(outputPath, ".txt");
                var content = $@"论文查重检测报告

报告编号：{result.TaskId}
检测时间：{result.CheckTime:yyyy-MM-dd HH:mm:ss}
总文字复制比：{result.TotalSimilarity:F1}%
检测结果：{(result.IsPassed ? "通过" : "未通过")}

各章节检测结果：
";
                if (result.Details != null)
                {
                    foreach (var detail in result.Details)
                    {
                        content += $"{detail.SectionName}: {detail.Similarity:F1}%\n";
                    }
                }

                content += $"\n结论：本文档总文字复制比为 {result.TotalSimilarity:F1}%，{(result.IsPassed ? "符合学术规范要求。" : "超出学术规范允许范围，建议修改。")}";
                
                File.WriteAllText(tempPath, content, Encoding.UTF8);
                
                _logger.LogWarning("PDF 生成功能需要 iText7 完整授权，已生成文本版本：{Path}", tempPath);
                
                return tempPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF 报告生成失败：{Path}", outputPath);
                throw;
            }
        }

        /// <summary>
        /// 生成 Word 格式报告
        /// </summary>
        public async Task<string> GenerateWordAsync(CheckResult result, string outputPath)
        {
            _logger.LogInformation("开始生成 Word 报告：{Path}", outputPath);

            // 使用 Open XML SDK 生成真正的 Word 文档
            // 这里暂时生成 RTF 并重命名（Word 可以打开 RTF）
            
            var rtfPath = Path.ChangeExtension(outputPath, ".rtf");
            await GenerateRtfAsync(result, rtfPath);
            
            // 实际项目中应使用 DocumentFormat.OpenXml 生成真正的.docx
            File.Copy(rtfPath, outputPath, true);
            
            _logger.LogInformation("Word 报告生成完成：{Path}", outputPath);
            
            return outputPath;
        }
    }
}
