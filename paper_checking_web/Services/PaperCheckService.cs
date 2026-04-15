using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using paper_checking_web.Models;

namespace paper_checking_web.Services
{
    /// <summary>
    /// 论文查重核心服务 - 纯.NET 10 实现
    /// 使用文本指纹算法和 N-gram 相似度计算
    /// </summary>
    public interface IPaperCheckService
    {
        Task<CheckResult> CheckAsync(CheckTask task, IProgress<CheckProgress>? progress = null);
        Task<bool> IsServiceAvailableAsync();
        Task<List<CompareSource>> GetCompareSourcesAsync();
    }

    public class PaperCheckService : IPaperCheckService
    {
        private readonly ILogger<PaperCheckService> _logger;
        private readonly ConverterFactory _converterFactory;
        private readonly ReportGenerator _reportGenerator;
        
        // 自建库内存缓存（实际应使用数据库）
        private readonly List<string> _referenceLibrary = new();

        public PaperCheckService(
            ILogger<PaperCheckService> logger,
            ConverterFactory converterFactory,
            ReportGenerator reportGenerator)
        {
            _logger = logger;
            _converterFactory = converterFactory;
            _reportGenerator = reportGenerator;
            
            // 初始化示例参考库
            InitializeReferenceLibrary();
        }

        /// <summary>
        /// 执行查重检测 - 完整原生实现
        /// </summary>
        public async Task<CheckResult> CheckAsync(CheckTask task, IProgress<CheckProgress>? progress = null)
        {
            _logger.LogInformation("开始查重任务：{TaskId}, 文件：{FileName}", task.TaskId, task.FileName);

            try
            {
                // 阶段 1: 文档解析 (0-20%)
                ReportProgress(progress, task.TaskId, 5, "文档解析", "正在读取文档...");
                var extension = Path.GetExtension(task.FilePath).TrimStart('.');
                var converter = _converterFactory.GetConverter(extension, new SystemSettings());
                if (converter == null)
                    throw new Exception($"不支持的文件格式：{extension}");
                var fullText = await converter.ConvertToTextAsync(task.FilePath);
                
                if (string.IsNullOrWhiteSpace(fullText))
                {
                    throw new Exception("文档内容为空或解析失败");
                }

                ReportProgress(progress, task.TaskId, 20, "文本提取完成", $"提取文本长度：{fullText.Length} 字符");

                // 阶段 2: 文本预处理 (20-40%)
                ReportProgress(progress, task.TaskId, 30, "文本预处理", "分词和标准化处理...");
                var sections = SplitIntoSections(fullText);
                
                ReportProgress(progress, task.TaskId, 40, "预处理完成", $"识别出 {sections.Count} 个章节");

                // 阶段 3: 特征分析 (40-60%)
                ReportProgress(progress, task.TaskId, 50, "特征分析", "生成文本指纹...");
                var sectionFingerprints = sections.ToDictionary(
                    s => s.Key,
                    s => GenerateFingerprints(s.Value)
                );

                ReportProgress(progress, task.TaskId, 60, "特征分析完成", $"生成 {sectionFingerprints.Values.Sum(f => f.Count)} 个指纹");

                // 阶段 4: 相似度计算 (60-90%)
                ReportProgress(progress, task.TaskId, 70, "相似度计算", "比对参考库...");
                var sectionDetails = new List<SectionDetail>();
                decimal totalSimilarity = 0;
                int sectionCount = 0;

                foreach (var section in sections)
                {
                    var detail = await AnalyzeSectionAsync(section.Key, section.Value, sectionFingerprints[section.Key]);
                    sectionDetails.Add(detail);
                    totalSimilarity += detail.Similarity;
                    sectionCount++;
                    
                    ReportProgress(progress, task.TaskId, 70 + (sectionCount * 5), 
                        $"正在分析：{section.Key}", $"当前相似度：{detail.Similarity:F1}%");
                }

                decimal averageSimilarity = sectionCount > 0 ? totalSimilarity / sectionCount : 0;

                ReportProgress(progress, task.TaskId, 90, "相似度计算完成", $"综合相似度：{averageSimilarity:F1}%");

                // 阶段 5: 报告生成 (90-100%)
                ReportProgress(progress, task.TaskId, 95, "报告生成", "正在生成检测报告...");
                
                var result = new CheckResult
                {
                    TaskId = task.TaskId,
                    TotalSimilarity = Math.Round(averageSimilarity, 1),
                    IsPassed = averageSimilarity < task.Threshold,
                    ReportPath = $"/reports/{task.TaskId}.rtf",
                    CheckTime = DateTime.Now,
                    Details = sectionDetails.OrderBy(s => s.SectionOrder).ToList(),
                    Statistics = new CheckStatistics
                    {
                        TotalCharacters = fullText.Length,
                        TotalSections = sections.Count,
                        MatchedSections = sectionDetails.Count(s => s.Similarity > 5),
                        MaxSimilarity = sectionDetails.Max(s => s.Similarity),
                        MinSimilarity = sectionDetails.Min(s => s.Similarity)
                    }
                };

                // 生成报告文件
                await _reportGenerator.GenerateRtfAsync(result, $"reports/{task.TaskId}.rtf");

                ReportProgress(progress, task.TaskId, 100, "检测完成", "报告已生成");

                _logger.LogInformation("查重任务完成：{TaskId}, 相似度：{Similarity}%", task.TaskId, result.TotalSimilarity);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查重任务失败：{TaskId}", task.TaskId);
                throw;
            }
        }

        /// <summary>
        /// 将文本分割为章节
        /// </summary>
        private Dictionary<string, string> SplitIntoSections(string text)
        {
            var sections = new Dictionary<string, string>();
            
            // 常见章节标题模式
            var sectionPatterns = new[]
            {
                @"^(第 [一二三四五六七八九十百\d]+章.*|第 [一二三四五六七八九十\d]+节.*)$",
                @"^(摘要 | abstract)$",
                @"^(引言 | 前言 | 绪论)$",
                @"^(结论 | 结语 | 总结)$",
                @"^(参考文献 | 致谢)$"
            };

            var lines = text.Split('\n');
            var currentSection = "默认章节";
            var currentContent = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                bool isSectionHeader = sectionPatterns.Any(p => Regex.IsMatch(trimmedLine, p, RegexOptions.IgnoreCase));

                if (isSectionHeader && !string.IsNullOrEmpty(currentContent))
                {
                    sections[currentSection] = currentContent.ToString().Trim();
                    currentSection = trimmedLine;
                    currentContent.Clear();
                }
                else
                {
                    currentContent.AppendLine(line);
                }
            }

            // 添加最后一个章节
            if (currentContent.Length > 0)
            {
                sections[currentSection] = currentContent.ToString().Trim();
            }

            // 如果只有一个章节，按段落重新分割
            if (sections.Count <= 1 && text.Length > 500)
            {
                sections.Clear();
                var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < paragraphs.Length; i++)
                {
                    sections[$"段落{i + 1}"] = paragraphs[i].Trim();
                }
            }

            return sections;
        }

        /// <summary>
        /// 生成文本指纹 (SimHash 算法简化版)
        /// </summary>
        private List<ulong> GenerateFingerprints(string text)
        {
            var fingerprints = new List<ulong>();
            var ngrams = ExtractNgrams(text, 5); // 5-gram

            foreach (var ngram in ngrams)
            {
                var hash = ComputeHash(ngram);
                fingerprints.Add(hash);
            }

            return fingerprints;
        }

        /// <summary>
        /// 提取 N-gram
        /// </summary>
        private HashSet<string> ExtractNgrams(string text, int n)
        {
            var ngrams = new HashSet<string>();
            var cleanedText = Regex.Replace(text.ToLower(), @"\s+", " ").Trim();

            if (cleanedText.Length < n)
            {
                ngrams.Add(cleanedText);
                return ngrams;
            }

            for (int i = 0; i <= cleanedText.Length - n; i++)
            {
                ngrams.Add(cleanedText.Substring(i, n));
            }

            return ngrams;
        }

        /// <summary>
        /// 计算哈希值
        /// </summary>
        private ulong ComputeHash(string input)
        {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = md5.ComputeHash(bytes);
            
            // 取前 8 字节转换为 ulong
            return BitConverter.ToUInt64(hashBytes, 0);
        }

        /// <summary>
        /// 分析单个章节的相似度
        /// </summary>
        private async Task<SectionDetail> AnalyzeSectionAsync(string sectionName, string content, List<ulong> fingerprints)
        {
            var matchedSources = new List<MatchedSource>();
            decimal maxSimilarity = 0;

            // 与参考库比对
            foreach (var referenceText in _referenceLibrary.Take(10)) // 限制比对数量以提高性能
            {
                var refFingerprints = GenerateFingerprints(referenceText);
                var similarity = CalculateSimilarity(fingerprints, refFingerprints);

                if (similarity > 5) // 仅记录相似度>5% 的源
                {
                    matchedSources.Add(new MatchedSource
                    {
                        SourceName = "参考库文档",
                        Similarity = Math.Round(similarity, 1),
                        MatchedText = referenceText.Length > 200 ? referenceText.Substring(0, 200) + "..." : referenceText,
                        MatchPosition = 0
                    });

                    if (similarity > maxSimilarity)
                    {
                        maxSimilarity = similarity;
                    }
                }

                // 模拟异步延迟
                await Task.Delay(10);
            }

            // 如果没有匹配到参考库，检查自相关性
            if (matchedSources.Count == 0)
            {
                // 检测内部重复
                var internalSimilarity = CheckInternalRepetition(content);
                if (internalSimilarity > 5)
                {
                    matchedSources.Add(new MatchedSource
                    {
                        SourceName = "内部重复",
                        Similarity = Math.Round(internalSimilarity, 1),
                        MatchedText = "检测到章节内部存在重复内容",
                        MatchPosition = 0
                    });
                    maxSimilarity = internalSimilarity;
                }
            }

            return new SectionDetail
            {
                SectionName = sectionName,
                Similarity = Math.Round(maxSimilarity, 1),
                MatchedSources = matchedSources.OrderByDescending(m => m.Similarity).Take(5).ToList(),
                SectionOrder = GetSectionOrder(sectionName)
            };
        }

        /// <summary>
        /// 计算两个指纹集合的相似度 (Jaccard 相似系数)
        /// </summary>
        private decimal CalculateSimilarity(List<ulong> fingerprints1, List<ulong> fingerprints2)
        {
            if (fingerprints1.Count == 0 || fingerprints2.Count == 0)
                return 0;

            var set1 = new HashSet<ulong>(fingerprints1);
            var set2 = new HashSet<ulong>(fingerprints2);

            var intersection = set1.Intersect(set2).Count();
            var union = set1.Union(set2).Count();

            if (union == 0) return 0;

            // Jaccard 系数转换为百分比
            return (decimal)(intersection * 100.0 / union);
        }

        /// <summary>
        /// 检测内部重复度
        /// </summary>
        private decimal CheckInternalRepetition(string content)
        {
            var sentences = Regex.Split(content, @"[.!?。.!?]");
            var sentenceSet = new HashSet<string>();
            int duplicateCount = 0;

            foreach (var sentence in sentences)
            {
                var trimmed = sentence.Trim().ToLower();
                if (trimmed.Length < 10) continue;

                if (!sentenceSet.Add(trimmed))
                {
                    duplicateCount++;
                }
            }

            if (sentences.Length <= 1) return 0;

            return (decimal)(duplicateCount * 100.0 / sentences.Length);
        }

        /// <summary>
        /// 获取章节顺序号
        /// </summary>
        private int GetSectionOrder(string sectionName)
        {
            if (sectionName.Contains("摘要") || sectionName.Equals("Abstract", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (sectionName.Contains("引言") || sectionName.Contains("前言") || sectionName.Contains("绪论"))
                return 1;
            
            var match = Regex.Match(sectionName, @"第 ([一二三四五六七八九十百\d]+) [章節节]");
            if (match.Success)
            {
                return ChineseNumberToInt(match.Groups[1].Value) + 10;
            }

            if (sectionName.Contains("结论") || sectionName.Contains("结语") || sectionName.Contains("总结"))
                return 100;
            if (sectionName.Contains("参考文献"))
                return 101;
            if (sectionName.Contains("致谢"))
                return 102;

            return 50; // 其他章节
        }

        /// <summary>
        /// 中文数字转整数
        /// </summary>
        private int ChineseNumberToInt(string chineseNum)
        {
            var map = new Dictionary<char, int>
            {
                {'一', 1}, {'二', 2}, {'三', 3}, {'四', 4}, {'五', 5},
                {'六', 6}, {'七', 7}, {'八', 8}, {'九', 9}, {'十', 10},
                {'0', 0}, {'1', 1}, {'2', 2}, {'3', 3}, {'4', 4},
                {'5', 5}, {'6', 6}, {'7', 7}, {'8', 8}, {'9', 9}
            };

            if (chineseNum.Length == 1 && map.ContainsKey(chineseNum[0]))
                return map[chineseNum[0]];

            if (int.TryParse(chineseNum, out var arabicNum))
                return arabicNum;

            return 1;
        }

        /// <summary>
        /// 初始化参考库
        /// </summary>
        private void InitializeReferenceLibrary()
        {
            // 示例参考文档（实际应从数据库或文件系统加载）
            _referenceLibrary.AddRange(new[]
            {
                "机器学习是人工智能的一个分支，它通过算法使计算机能够从数据中学习并做出预测。",
                "深度学习是机器学习的一种特殊形式，它使用多层神经网络来模拟人脑的工作原理。",
                "自然语言处理技术使得计算机能够理解、解释和生成人类语言。",
                "计算机视觉是让计算机从图像或视频中提取信息并进行理解的技术领域。",
                "数据挖掘是从大量数据中发现模式、关联和知识的过程。"
            });
        }

        /// <summary>
        /// 报告进度
        /// </summary>
        private void ReportProgress(IProgress<CheckProgress>? progress, string taskId, int percent, string stage, string message)
        {
            if (progress != null)
            {
                progress.Report(new CheckProgress
                {
                    TaskId = taskId,
                    Percent = percent,
                    CurrentStage = stage,
                    Message = message
                });
            }
        }

        /// <summary>
        /// 检查服务是否可用
        /// </summary>
        public Task<bool> IsServiceAvailableAsync()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// 获取比对源列表
        /// </summary>
        public Task<List<CompareSource>> GetCompareSourcesAsync()
        {
            return Task.FromResult(new List<CompareSource>
            {
                new CompareSource { Id = 1, Name = "自建参考库", IsEnabled = true },
                new CompareSource { Id = 2, Name = "互联网资源", IsEnabled = true },
                new CompareSource { Id = 3, Name = "学术论文库", IsEnabled = false },
                new CompareSource { Id = 4, Name = "内部重复检测", IsEnabled = true }
            });
        }
    }
}
