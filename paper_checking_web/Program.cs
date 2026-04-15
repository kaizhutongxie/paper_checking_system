using Microsoft.EntityFrameworkCore;
using paper_checking_web.Data;
using paper_checking_web.Hubs;
using paper_checking_web.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "论文查重系统 API", 
        Version = "v1",
        Description = "基于.NET 10 的跨平台论文查重系统，支持麒麟 V10 等 Linux 环境"
    });
});

// 添加 SignalR 实时通信
builder.Services.AddSignalR();

// 添加数据库上下文（使用 SQLite）
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=paper_check.db"));

// 注册文档转换服务
builder.Services.AddScoped<IDocumentConverter, TxtConverter>();
builder.Services.AddScoped<IDocumentConverter, PdfConverter>();
builder.Services.AddScoped<IDocumentConverter, WordConverter>();

// 注册核心查重服务
builder.Services.Configure<PaperCheckServiceOptions>(options =>
{
    options.UseWindowsService = true; // 默认使用 Windows 代理服务
    options.ServiceUrl = builder.Configuration["WindowsService:Url"] ?? "http://localhost:5001";
    options.TimeoutSeconds = 300;
});
builder.Services.AddHttpClient<IPaperCheckService, PaperCheckService>();
builder.Services.AddScoped<IPaperCheckService, PaperCheckService>();

// 注册报告生成服务
builder.Services.AddScoped<IReportGenerator, ReportGenerator>();

// 注册进度通知服务
builder.Services.AddScoped<IProgressNotificationService, ProgressNotificationService>();
builder.Services.AddSingleton<TaskStateManager>();

// 配置静态文件服务（用于前端页面）
builder.Services.AddStaticFiles();

// 添加 CORS 支持（前端分离部署时需要）
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

// 启用静态文件服务（用于前端页面）
app.UseStaticFiles();

// 映射 SignalR Hub
app.MapHub<CheckProgressHub>("/hubs/check-progress");

// 回退路由到前端页面
app.MapFallbackToFile("index.html");

app.MapControllers();

// 确保数据目录存在
EnsureDataDirectories();

// 初始化数据库
InitializeDatabase(app);

app.Run();

void EnsureDataDirectories()
{
    var directories = new[]
    {
        paper_checking_web.Config.AppConfig.ProgramParam.TxtPaperSourcePath,
        paper_checking_web.Config.AppConfig.ProgramParam.ToCheckTxtPaperPath,
        paper_checking_web.Config.AppConfig.ProgramParam.ReportPath,
        paper_checking_web.Config.AppConfig.ProgramParam.ReportDataPath,
        "/data/uploads",
        "/data/reports",
        "/data/temp"
    };

    foreach (var dir in directories)
    {
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}

void InitializeDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // 确保数据库创建并应用种子数据
    context.Database.EnsureCreated();
    
    Console.WriteLine("数据库初始化完成");
}
