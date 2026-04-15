# 论文查重系统 .NET 10 Web 重构方案

## 一、系统功能全景分析

### 1.1 核心业务流程

```
用户操作 → 文件上传 → 格式转换 → 查重比对 → 报告生成 → 报告查看/导出
```

### 1.2 功能模块划分

#### 模块一：用户界面层（需完全重写）
| 原 WinForms 窗体 | 功能描述 | Web 改造方案 |
|-----------------|---------|-------------|
| MainForm | 主界面，含 5 个选项卡 | Blazor 页面/Razor 页面 |
| Licence | 许可证信息展示 | 弹窗组件/独立页面 |
| ReportListForm | 报告列表展示（分页、搜索） | 数据表格组件 + 分页 |
| ReportDetailForm | 单个报告详情（富文本标注） | 富文本编辑器 + 高亮 |

#### 模块二：业务逻辑层（可复用 60%）
| 类名 | 功能 | 迁移策略 |
|------|------|---------|
| PaperManager | 查重任务调度、DLL 调用 | 保留核心逻辑，改为异步 |
| RunningEnv | 运行环境配置封装 | 直接复用 |
| Utils | 工具方法（文件操作、配置读写） | 适配后复用 |
| ConverterFactory | 转换器工厂 | 保留模式，替换实现 |

#### 模块三：文档转换层（需替换依赖）
| 转换器 | 原技术栈 | 替代方案 |
|--------|---------|---------|
| WordConverter | Spire.Doc (免费版) | Aspose.Words / NPOI / 调用 LibreOffice |
| PdfConverter | IKVM + PDFBox | PdfSharp / iText7 / 调用 pdftotext |
| TxtConverter | 原生 C# | 直接复用 |

#### 模块四：核心算法层（关键瓶颈）
| 组件 | 问题 | 解决方案 |
|------|------|---------|
| paper_check.dll | 无源码 Windows 原生 DLL | **方案 A**: Windows 服务封装 + HTTP/gRPC 调用<br>**方案 B**: 逆向重写（高风险） |

#### 模块五：系统适配层（需重写）
| 功能 | 原实现 | Linux 适配 |
|------|--------|-----------|
| 硬件指纹获取 | WMI (Win32_DiskDrive) | /dev/disk/by-id + lshw |
| 许可证验证 | RSA 签名 | 保留算法，修改硬件源 |
| 文件路径 | Windows 风格 | 跨平台 Path API |
| 进程启动 | explorer.exe | xdg-open (Linux) |

---

## 二、技术架构设计

### 2.1 推荐技术栈

```
┌─────────────────────────────────────────────────────┐
│                    前端层                            │
│  Blazor Server / Blazor WASM + MudBlazor 组件库     │
└─────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────┐
│                  API 网关层                          │
│         ASP.NET Core Web API (.NET 10)              │
│  ┌─────────────┬─────────────┬─────────────────┐   │
│  │  认证中间件  │  日志中间件  │  异常处理中间件  │   │
│  └─────────────┴─────────────┴─────────────────┘   │
└─────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────┐
│                  业务服务层                          │
│  ┌──────────┬──────────┬──────────┬────────────┐   │
│  │论文管理服务│转换服务  │查重服务  │ 报告服务    │   │
│  └──────────┴──────────┴──────────┴────────────┘   │
└─────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────┐
│                  基础设施层                          │
│  ┌──────────┬──────────┬──────────────────────┐   │
│  │文件存储  │缓存服务  │ Windows 查重代理服务  │   │
│  │(本地/MinIO)│(Redis)  │(gRPC/HTTP 远程调用)   │   │
│  └──────────┴──────────┴──────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

### 2.2 项目结构

```
PaperCheck.Web/
├── PaperCheck.Web.Api/              # ASP.NET Core Web API 项目
│   ├── Controllers/
│   │   ├── PapersController.cs      # 论文管理 API
│   │   ├── CheckController.cs       # 查重任务 API
│   │   ├── ReportsController.cs     # 报告查询 API
│   │   └── SystemController.cs      # 系统设置 API
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── RequestLoggingMiddleware.cs
│   └── Program.cs
│
├── PaperCheck.Core/                 # 核心业务逻辑（类库）
│   ├── Entities/
│   │   ├── CheckTask.cs
│   │   ├── Paper.cs
│   │   └── Report.cs
│   ├── Services/
│   │   ├── PaperService.cs
│   │   ├── ConvertService.cs
│   │   ├── CheckService.cs
│   │   └── ReportService.cs
│   ├── Converters/
│   │   ├── IConverter.cs
│   │   ├── WordConverter.cs
│   │   ├── PdfConverter.cs
│   │   └── TxtConverter.cs
│   └── Utils/
│       ├── FileHelper.cs
│       └── ConfigHelper.cs
│
├── PaperCheck.Infrastructure/       # 基础设施（类库）
│   ├── HardwareFingerprint/
│   │   ├── IFingerprintProvider.cs
│   │   ├── WindowsFingerprintProvider.cs
│   │   └── LinuxFingerprintProvider.cs
│   ├── License/
│   │   └── LicenseValidator.cs
│   └── PaperCheckProxy/             # paper_check.dll 代理调用
│       └── PaperCheckClient.cs
│
├── PaperCheck.Web.UI/               # Blazor 前端项目
│   ├── Pages/
│   │   ├── Index.razor              # 主页（任务提交）
│   │   ├── Library.razor            # 论文库管理
│   │   ├── Reports.razor            # 报告列表
│   │   ├── ReportDetail.razor       # 报告详情
│   │   └── Settings.razor           # 系统设置
│   ├── Components/
│   │   ├── PaperUpload.razor
│   │   ├── ProgressIndicator.razor
│   │   └── ReportViewer.razor
│   └── wwwroot/
│
└── PaperCheck.WindowsService/       # Windows 查重代理服务（单独部署）
    ├── PaperCheckWrapper.cs         # 封装 paper_check.dll
    └── Program.cs
```

---

## 三、核心难点解决方案

### 3.1 paper_check.dll 处理方案（最关键）

**方案 A：Windows 代理服务（推荐）**

```
麒麟 V10 Web 应用 ←gRPC/HTTP→ Windows 服务器 (paper_check.dll)
```

**实现步骤：**
1. 创建独立的 .NET Framework 4.6 Windows 服务项目
2. 封装原有 PaperManager.RealCheckShell 方法为 gRPC/HTTP 接口
3. 在麒麟 V10 上通过 HttpClient/gRPC Client 调用
4. 网络通信加密（HTTPS/mTLS）

**优点：** 
- 零代码改动核心算法
- 保持原有查重准确性
- 开发周期短（1-2 周）

**缺点：**
- 需要额外 Windows 服务器
- 网络延迟影响性能

**代码示例（Windows 服务端）：**
```csharp
// PaperCheckService.cs - Windows 服务
public class PaperCheckService : IPaperCheckService
{
    [DllImport(@"paper_check.dll", EntryPoint = "real_check", ...)]
    extern static int real_check(string s1, string s2, ...);
    
    public async Task<CheckResult> CheckAsync(CheckRequest request)
    {
        int result = await Task.Run(() => 
            real_check(request.Threshold, request.ThreadCnt, ...));
        return new CheckResult { Success = result == 0 };
    }
}
```

**代码示例（麒麟 V10 客户端）：**
```csharp
// PaperCheckClient.cs - Linux 端调用
public class PaperCheckClient
{
    private readonly HttpClient _httpClient;
    
    public async Task<CheckResult> CheckAsync(CheckRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/papercheck/check", request);
        return await response.Content.ReadFromJsonAsync<CheckResult>();
    }
}
```

---

### 3.2 文档转换依赖替换

#### 3.2.1 Word 文档转换

**方案对比：**

| 方案 | 成本 | Linux 支持 | 中文兼容性 | 推荐度 |
|------|------|-----------|-----------|--------|
| Aspose.Words | 付费 ($$$) | ✅ | 优秀 | ⭐⭐⭐⭐ |
| NPOI | 免费 | ✅ | 良好 | ⭐⭐⭐ |
| LibreOffice CLI | 免费 | ✅ | 优秀 | ⭐⭐⭐⭐⭐ |
| DocX | 免费 | ⚠️ 部分 | 一般 | ⭐⭐ |

**推荐方案：LibreOffice 命令行调用**
```bash
# 安装
sudo apt install libreoffice-writer

# C# 调用
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "libreoffice",
        Arguments = $"--headless --convert-to txt \"{inputPath}\"",
        RedirectStandardOutput = true,
        UseShellExecute = false
    }
};
process.Start();
```

#### 3.2.2 PDF 文档转换

**方案对比：**

| 方案 | 成本 | Linux 支持 | 中文兼容性 | 推荐度 |
|------|------|-----------|-----------|--------|
| iText7 | 付费 (AGPL) | ✅ | 优秀 | ⭐⭐⭐⭐ |
| PdfSharp | 免费 | ✅ | 良好 | ⭐⭐⭐ |
| pdftotext (poppler) | 免费 | ✅ | 优秀 | ⭐⭐⭐⭐⭐ |

**推荐方案：pdftotext 命令行调用**
```bash
# 安装
sudo apt install poppler-utils

# C# 调用
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "pdftotext",
        Arguments = $"-enc UTF-8 \"{inputPath}\" -",
        RedirectStandardOutput = true,
        UseShellExecute = false
    }
};
```

---

### 3.3 硬件指纹获取（Linux 适配）

**原 Windows 实现：**
```csharp
// 使用 WMI 获取硬盘序列号
ManagementClass mc = new ManagementClass("Win32_DiskDrive");
```

**Linux 替代方案：**
```csharp
public class LinuxFingerprintProvider : IFingerprintProvider
{
    public string GetDiskSerial()
    {
        // 方法 1: 读取 /dev/disk/by-id
        var directory = new DirectoryInfo("/dev/disk/by-id");
        var disk = directory.GetFiles("ata-*").FirstOrDefault();
        if (disk != null)
        {
            return disk.Name.Replace("ata-", "");
        }
        
        // 方法 2: 执行 lshw 命令
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "sudo",
                Arguments = "lshw -class disk -serial",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        process.Start();
        return process.StandardOutput.ReadToEnd().Trim();
    }
    
    public string GetMacAddress()
    {
        var nics = NetworkInterface.GetAllNetworkInterfaces();
        foreach (var nic in nics)
        {
            if (nic.OperationalStatus == OperationalStatus.Up)
            {
                return nic.GetPhysicalAddress().ToString();
            }
        }
        return string.Empty;
    }
}
```

---

### 3.4 报告富文本展示

**原 WinForms 实现：**
```csharp
// 使用 RichTextBox 显示 RTF 格式
richTextBox1.LoadFile(reportPath, RichTextBoxStreamType.RichText);
richTextBox1.Select(position, length);
richTextBox1.SelectionColor = Color.Red; // 标红重复部分
```

**Web 端替代方案：**

**方案 A：Quill.js 富文本编辑器**
```html
<!-- Blazor 组件 -->
<div id="editor"></div>
<script>
  var quill = new Quill('#editor', { theme: 'snow' });
  // 加载 RTF 转换后的 HTML
  quill.root.innerHTML = rtfToHtml(rtfContent);
  // 高亮重复部分
  highlightDuplicates(quill, duplicatePositions);
</script>
```

**方案 B：自定义 HTML 渲染**
```csharp
// 后端生成带标记的 HTML
public string GenerateReportHtml(string text, List<int> duplicatePositions)
{
    var sb = new StringBuilder();
    for (int i = 0; i < text.Length; i++)
    {
        if (duplicatePositions.Contains(i))
        {
            sb.Append($"<span class=\"duplicate\">{text[i]}</span>");
        }
        else
        {
            sb.Append(text[i]);
        }
    }
    return sb.ToString();
}
```

```css
/* CSS 样式 */
.duplicate {
    background-color: #ffcccc;
    color: #cc0000;
    font-weight: bold;
}
```

---

## 四、开发工作量评估

### 4.1 模块工作量分解

| 模块 | 工作内容 | 人天 | 难度 |
|------|---------|------|------|
| **前端 UI** | Blazor 页面开发（5 个主页面 + 组件） | 25 | ⭐⭐⭐ |
| **API 层** | Controller + DTO + 验证 | 15 | ⭐⭐ |
| **业务逻辑** | 服务层重构 + 异步改造 | 20 | ⭐⭐⭐ |
| **文档转换** | LibreOffice/pdftotext 集成 | 10 | ⭐⭐ |
| **硬件指纹** | Linux 适配 + 跨平台抽象 | 8 | ⭐⭐⭐ |
| **Windows 代理** | gRPC/HTTP 服务封装 | 10 | ⭐⭐ |
| **报告生成** | RTF→HTML 转换 + 高亮 | 12 | ⭐⭐⭐ |
| **测试调试** | 单元测试 + 集成测试 | 20 | ⭐⭐ |
| **部署配置** | Docker + CI/CD | 8 | ⭐⭐ |
| **文档编写** | 技术文档 + 用户手册 | 7 | ⭐ |
| **合计** | | **135 人天** | |

### 4.2 时间规划

**团队配置：** 5 人团队（1 架构 + 2 后端 + 1 前端 + 1 测试）

| 阶段 | 时间 | 交付物 |
|------|------|--------|
| 第一阶段：架构搭建 | 2 周 | 项目框架、CI/CD、基础组件 |
| 第二阶段：核心开发 | 6 周 | API、业务逻辑、文档转换 |
| 第三阶段：前端开发 | 4 周 | Blazor 页面、交互逻辑 |
| 第四阶段：集成测试 | 2 周 | 测试报告、BUG 修复 |
| 第五阶段：部署上线 | 1 周 | 生产环境、运维文档 |
| **总计** | **15 周（约 3.5 个月）** | |

---

## 五、风险与应对

### 5.1 技术风险

| 风险 | 概率 | 影响 | 应对措施 |
|------|------|------|---------|
| paper_check.dll 网络调用延迟高 | 中 | 高 | 批量调用优化 + 连接池 |
| LibreOffice 转换质量不佳 | 低 | 中 | 备用方案：Aspose.Words 试用 |
| Linux 硬件指纹不稳定 | 中 | 高 | 多因子融合（MAC+ 磁盘 +CPU） |
| 中文字符编码问题 | 高 | 中 | 统一使用 UTF-8，充分测试 |

### 5.2 非技术风险

| 风险 | 影响 | 应对措施 |
|------|------|---------|
| 需求变更 | 进度延期 | 敏捷开发，每 2 周演示确认 |
| 人员流动 | 知识断层 | 代码审查 + 文档同步 |
| 第三方库授权 | 法律风险 | 提前确认 AGPL/MIT 等协议 |

---

## 六、实施建议

### 6.1 分阶段实施路线

**Phase 1（第 1-2 周）：可行性验证**
- [ ] 搭建 .NET 10 开发环境
- [ ] 验证 LibreOffice/pdftotext 转换效果
- [ ] 实现 Windows 代理服务原型
- [ ] 完成硬件指纹 Linux 适配 POC

**Phase 2（第 3-8 周）：核心功能开发**
- [ ] 完成 API 层和业务逻辑层
- [ ] 实现文档转换服务
- [ ] 集成 Windows 查重代理
- [ ] 开发报告生成模块

**Phase 3（第 9-12 周）：前端开发**
- [ ] Blazor 页面开发
- [ ] 文件上传/进度展示组件
- [ ] 报告查看器（高亮功能）
- [ ] 系统设置页面

**Phase 4（第 13-15 周）：测试部署**
- [ ] 全链路测试
- [ ] 性能压测
- [ ] 麒麟 V10 适配测试
- [ ] 生产环境部署

### 6.2 关键决策点

1. **是否接受混合部署方案？**
   - 是 → 采用 Windows 代理服务（推荐）
   - 否 → 需要逆向重写查重算法（成本增加 3-5 倍）

2. **前端技术选型？**
   - Blazor Server → 开发快，但需持续连接
   - Blazor WASM → 离线可用，但首次加载慢
   - Vue/React → 生态丰富，但需额外后端 API

3. **文档转换精度要求？**
   - 高精度 → 采购 Aspose.Words + iText7（年费约$5000）
   - 可接受 → 使用 LibreOffice + pdftotext（免费）

---

## 七、总结

### 7.1 项目可行性结论

✅ **技术上可行**，但需注意：
- paper_check.dll 必须通过 Windows 代理服务调用
- 文档转换需充分测试中文兼容性
- 硬件指纹机制需重新设计

### 7.2 投入产出比

| 指标 | 评估 |
|------|------|
| 开发成本 | 约 135 人天（5 人团队 3.5 个月） |
| 维护成本 | 低于原 WinForms 版本 |
| 用户体验 | 显著提升（Web 访问、跨平台） |
| 可扩展性 | 大幅提升（微服务架构） |

### 7.3 最终建议

**推荐采用本方案**，理由：
1. 保留核心算法准确性（通过 Windows 代理）
2. 实现真正的跨平台（麒麟 V10 + 其他 Linux 发行版）
3. 技术栈现代化（.NET 10 + Blazor）
4. 便于后续功能扩展（云部署、多租户等）

**下一步行动：**
1. 确认 paper_check.dll 所有者是否提供 Linux 版本
2. 搭建 POC 环境验证关键技术点
3. 制定详细的需求规格说明书
4. 组建开发团队并启动项目
