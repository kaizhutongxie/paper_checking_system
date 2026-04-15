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
    public int TotalFiles { get; set; }
    public int ConvertedFiles { get; set; }
    public int CheckedFiles { get; set; }
    public int ExportedFiles { get; set; }
    public int ProgressPercent { get; set; }
    public string Status { get; set; } = "idle";
    public List<string> ErrorPapers { get; set; } = new();
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
