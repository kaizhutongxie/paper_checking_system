using paper_checking_web.Models;
using paper_checking_web.Services;
using paper_checking_web.Utils;

namespace paper_checking_web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckController : ControllerBase
{
    private readonly ILogger<CheckController> _logger;
    private readonly ConverterFactory _converterFactory;

    public CheckController(ILogger<CheckController> logger, ConverterFactory converterFactory)
    {
        _logger = logger;
        _converterFactory = converterFactory;
    }

    /// <summary>
    /// 获取系统状态和配置
    /// </summary>
    [HttpGet("status")]
    public ActionResult<Dictionary<string, object>> GetStatus()
    {
        var status = new Dictionary<string, object>
        {
            ["processorCount"] = SystemUtils.GetProcessorCount(),
            ["macAddress"] = SystemUtils.GetMacAddress(),
            ["diskInfo"] = SystemUtils.GetDiskInfo(),
            ["isLinux"] = OperatingSystem.IsLinux(),
            ["version"] = typeof(CheckController).Assembly.GetName().Version?.ToString() ?? "1.0.0"
        };
        
        return Ok(status);
    }

    /// <summary>
    /// 获取当前查重配置
    /// </summary>
    [HttpGet("config")]
    public ActionResult<CheckConfig> GetConfig()
    {
        // TODO: 从配置文件或数据库读取
        return Ok(AppConfig.DefaultCheckConfig);
    }

    /// <summary>
    /// 更新查重配置
    /// </summary>
    [HttpPost("config")]
    public IActionResult UpdateConfig([FromBody] CheckConfig config)
    {
        // 参数验证
        if (config.CheckThreshold < 1 || config.CheckThreshold >= 100)
        {
            return BadRequest("查重阈值必须在 1-99 之间");
        }
        
        if (config.CheckWay != 0 && config.CheckWay != 1)
        {
            return BadRequest("查重方式必须为 0 或 1");
        }

        // TODO: 保存配置到文件或数据库
        
        _logger.LogInformation("配置已更新：{@Config}", config);
        return Ok(config);
    }

    /// <summary>
    /// 获取系统设置
    /// </summary>
    [HttpGet("settings")]
    public ActionResult<SystemSettings> GetSettings()
    {
        // TODO: 从配置文件读取
        return Ok(AppConfig.DefaultSystemSettings);
    }

    /// <summary>
    /// 更新系统设置
    /// </summary>
    [HttpPost("settings")]
    public IActionResult UpdateSettings([FromBody] SystemSettings settings)
    {
        // 参数验证
        if (settings.CheckThreadCnt < 1 || settings.CheckThreadCnt >= 100)
        {
            return BadRequest("查重线程数必须在 1-99 之间");
        }
        
        if (settings.ConvertThreadCnt < 1 || settings.ConvertThreadCnt >= 100)
        {
            return BadRequest("转换线程数必须在 1-99 之间");
        }

        // TODO: 保存设置到文件或数据库
        
        _logger.LogInformation("设置已更新：{@Settings}", settings);
        return Ok(settings);
    }

    /// <summary>
    /// 开始查重任务
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult> StartCheck([FromBody] CheckRequest request)
    {
        // TODO: 实现查重逻辑
        // 1. 验证路径是否存在
        // 2. 启动后台任务进行文件转换和查重
        // 3. 返回任务 ID
        
        return Accepted(new { taskId = Guid.NewGuid().ToString(), message = "查重任务已启动" });
    }

    /// <summary>
    /// 获取查重进度
    /// </summary>
    [HttpGet("progress/{taskId}")]
    public ActionResult<CheckProgress> GetProgress(string taskId)
    {
        // TODO: 根据任务 ID 查询进度
        return Ok(new CheckProgress
        {
            Status = "running",
            ProgressPercent = 0,
            TotalFiles = 0,
            ConvertedFiles = 0,
            CheckedFiles = 0
        });
    }

    /// <summary>
    /// 停止查重任务
    /// </summary>
    [HttpPost("stop/{taskId}")]
    public IActionResult StopCheck(string taskId)
    {
        // TODO: 停止指定任务
        return Ok(new { message = $"任务 {taskId} 已停止" });
    }

    /// <summary>
    /// 获取报告列表
    /// </summary>
    [HttpGet("reports")]
    public ActionResult<List<ReportSummary>> GetReports()
    {
        // TODO: 从报告目录读取报告列表
        return Ok(new List<ReportSummary>());
    }

    /// <summary>
    /// 获取报告详情
    /// </summary>
    [HttpGet("reports/{paperName}")]
    public ActionResult<ReportDetail> GetReportDetail(string paperName)
    {
        // TODO: 读取指定报告的详情
        return Ok(new ReportDetail
        {
            PaperName = paperName,
            SimilarityRate = 0,
            Content = "",
            RepeatedPositions = new List<int>()
        });
    }

    /// <summary>
    /// 导出报告
    /// </summary>
    [HttpPost("export")]
    public async Task<ActionResult> ExportReports([FromBody] ExportRequest request)
    {
        // TODO: 导出报告到指定路径
        return Ok(new { message = "报告导出完成" });
    }

    /// <summary>
    /// 添加论文到库
    /// </summary>
    [HttpPost("library/add")]
    public async Task<ActionResult> AddToLibrary([FromBody] LibraryRequest request)
    {
        // TODO: 将指定路径的论文添加到论文库
        return Accepted(new { taskId = Guid.NewGuid().ToString(), message = "论文添加任务已启动" });
    }

    /// <summary>
    /// 重置系统
    /// </summary>
    [HttpPost("reset")]
    public IActionResult Reset()
    {
        // TODO: 删除临时文件和报告
        return Ok(new { message = "系统已重置" });
    }
}

/// <summary>
/// 查重请求
/// </summary>
public class CheckRequest
{
    public int CheckWay { get; set; }
    public int CheckThreshold { get; set; }
    public bool Recover { get; set; }
    public string ToCheckPaperPath { get; set; } = string.Empty;
    public string FinalReportPath { get; set; } = string.Empty;
}

/// <summary>
/// 导出请求
/// </summary>
public class ExportRequest
{
    public string ExportPath { get; set; } = string.Empty;
    public bool IncludeStatisTable { get; set; } = true;
}

/// <summary>
/// 论文库请求
/// </summary>
public class LibraryRequest
{
    public string SourcePath { get; set; } = string.Empty;
}
