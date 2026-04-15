using Microsoft.EntityFrameworkCore;
using paper_checking_web.Data;
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
        Description = "基于.NET 8 的跨平台论文查重系统，支持麒麟 V10 等 Linux 环境"
    });
});

// 添加数据库上下文（使用 SQLite）
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=paper_check.db"));

// 注册文档转换服务
builder.Services.AddScoped<ConverterFactory>();
builder.Services.AddScoped<TxtConverter>();
builder.Services.AddScoped<PdfConverter>();
builder.Services.AddScoped<WordConverter>();

// 注册核心查重服务
builder.Services.AddScoped<PaperCheckService>();

// 注册报告生成服务
builder.Services.AddScoped<ReportGenerator>();

// 添加 CORS 支持
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

app.MapControllers();

// 确保数据目录存在
EnsureDataDirectories();

// 初始化数据库
InitializeDatabase(app);

Console.WriteLine("论文查重系统启动成功！");
Console.WriteLine("访问 Swagger UI: http://localhost:5000/swagger");

app.Run();

void EnsureDataDirectories()
{
    var directories = new[]
    {
        "data/uploads",
        "data/reports",
        "data/temp",
        "reports"
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
