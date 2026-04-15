using paper_checking_web.Models;

namespace paper_checking_web.Config;

/// <summary>
/// 应用程序配置
/// </summary>
public class AppConfig
{
    /// <summary>
    /// 程序常量参数
    /// </summary>
    public static class ProgramParam
    {
        public const string SecurityKey = "Ubzrfax@3&Yl1rf&cw7ZE4zXsm8ZdIAtyJZ71L48f3yW*TXzylZq7Hqb1moG*xeQQnkFdkqYYXFfyPAS$CeETMw#1qDAPJehBM8";
        public const int MaxWords = 99998;
        
        // 数据存储路径 (相对于工作目录)
        public static readonly string TxtPaperSourcePath = Path.Combine("data", "txtPaperSource");
        public static readonly string ToCheckTxtPaperPath = Path.Combine("data", "toCheckTxtPaper");
        public static readonly string ReportPath = Path.Combine("data", "report");
        public static readonly string ReportDataPath = Path.Combine("data", "reportData");
    }
    
    /// <summary>
    /// 默认查重配置
    /// </summary>
    public static CheckConfig DefaultCheckConfig => new()
    {
        CheckWay = 0,
        CheckThreshold = 13,
        Recover = false,
        StatisTable = true,
        MinBytes = 1,
        MinWords = 1,
        Blocklist = string.Empty
    };
    
    /// <summary>
    /// 默认系统设置
    /// </summary>
    public static SystemSettings DefaultSystemSettings => new()
    {
        CheckThreadCnt = Math.Max(1, Environment.ProcessorCount - 2),
        ConvertThreadCnt = Math.Max(1, Environment.ProcessorCount - 2),
        SupportPdf = true,
        SupportDoc = true,
        SupportDocx = true,
        SupportTxt = true
    };
}
