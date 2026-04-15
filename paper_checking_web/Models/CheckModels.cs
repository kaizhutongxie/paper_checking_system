namespace paper_checking_web.Models;

/// <summary>
/// 查重配置参数
/// </summary>
public class CheckConfig
{
    /// <summary>
    /// 查重方式：0=纵向查重，1=横向查重
    /// </summary>
    public int CheckWay { get; set; } = 0;
    
    /// <summary>
    /// 查重阈值 (1-99)
    /// </summary>
    public int CheckThreshold { get; set; } = 13;
    
    /// <summary>
    /// 是否恢复中断的任务
    /// </summary>
    public bool Recover { get; set; } = false;
    
    /// <summary>
    /// 是否生成统计表
    /// </summary>
    public bool StatisTable { get; set; } = true;
    
    /// <summary>
    /// 待查论文路径
    /// </summary>
    public string ToCheckPaperPath { get; set; } = string.Empty;
    
    /// <summary>
    /// 最终报告保存路径
    /// </summary>
    public string FinalReportPath { get; set; } = string.Empty;
    
    /// <summary>
    /// 最小字节数限制
    /// </summary>
    public int MinBytes { get; set; } = 1;
    
    /// <summary>
    /// 最小字数限制
    /// </summary>
    public int MinWords { get; set; } = 1;
    
    /// <summary>
    /// 屏蔽词列表 (用逗号分隔)
    /// </summary>
    public string Blocklist { get; set; } = string.Empty;
}

/// <summary>
/// 论文库配置
/// </summary>
public class LibraryConfig
{
    /// <summary>
    /// 论文库源路径
    /// </summary>
    public string PaperSourcePath { get; set; } = string.Empty;
}

/// <summary>
/// 系统设置
/// </summary>
public class SystemSettings
{
    /// <summary>
    /// 查重线程数
    /// </summary>
    public int CheckThreadCnt { get; set; } = 3;
    
    /// <summary>
    /// 文件转换线程数
    /// </summary>
    public int ConvertThreadCnt { get; set; } = 2;
    
    /// <summary>
    /// 是否支持 PDF
    /// </summary>
    public bool SupportPdf { get; set; } = true;
    
    /// <summary>
    /// 是否支持 DOC
    /// </summary>
    public bool SupportDoc { get; set; } = true;
    
    /// <summary>
    /// 是否支持 DOCX
    /// </summary>
    public bool SupportDocx { get; set; } = true;
    
    /// <summary>
    /// 是否支持 TXT
    /// </summary>
    public bool SupportTxt { get; set; } = true;
}

/// <summary>
/// 查重进度信息
/// </summary>
public class CheckProgress
{
    public string TaskId { get; set; } = string.Empty;
    public int Percent { get; set; }
    public string CurrentStage { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    // 兼容旧版字段
    public int TotalFiles { get; set; }
    public int ConvertedFiles { get; set; }
    public int CheckedFiles { get; set; }
    public int ExportedFiles { get; set; }
    public int ProgressPercent => Percent;
    public string Status { get; set; } = "idle";
    public List<string> ErrorPapers { get; set; } = new();
}

/// <summary>
/// 查重任务
/// </summary>
public class CheckTask
{
    public string TaskId { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int Threshold { get; set; } = 13;
    public bool IncludeStatisTable { get; set; } = true;
    public DateTime CreateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 查重结果
/// </summary>
public class CheckResult
{
    public string TaskId { get; set; } = string.Empty;
    public decimal TotalSimilarity { get; set; }
    public bool IsPassed { get; set; }
    public string ReportPath { get; set; } = string.Empty;
    public DateTime CheckTime { get; set; }
    public List<SectionDetail> Details { get; set; } = new();
    public CheckStatistics Statistics { get; set; } = new();
}

/// <summary>
/// 章节详情
/// </summary>
public class SectionDetail
{
    public string SectionName { get; set; } = string.Empty;
    public decimal Similarity { get; set; }
    public List<MatchedSource> MatchedSources { get; set; } = new();
    public int SectionOrder { get; set; }
}

/// <summary>
/// 匹配源
/// </summary>
public class MatchedSource
{
    public string SourceName { get; set; } = string.Empty;
    public decimal Similarity { get; set; }
    public string MatchedText { get; set; } = string.Empty;
    public int MatchPosition { get; set; }
}

/// <summary>
/// 查重统计
/// </summary>
public class CheckStatistics
{
    public int TotalCharacters { get; set; }
    public int TotalSections { get; set; }
    public int MatchedSections { get; set; }
    public decimal MaxSimilarity { get; set; }
    public decimal MinSimilarity { get; set; }
}

/// <summary>
/// 比对源配置
/// </summary>
public class CompareSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 查重报告摘要
/// </summary>
public class ReportSummary
{
    public string PaperName { get; set; } = string.Empty;
    public double SimilarityRate { get; set; }
    public int TotalWords { get; set; }
    public int RepeatedWords { get; set; }
    public DateTime CheckTime { get; set; }
}

/// <summary>
/// 查重报告详情
/// </summary>
public class ReportDetail
{
    public string PaperName { get; set; } = string.Empty;
    public double SimilarityRate { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<int> RepeatedPositions { get; set; } = new();
    public string RtfContent { get; set; } = string.Empty;
}
