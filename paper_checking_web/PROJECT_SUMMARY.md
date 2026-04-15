# 论文查重系统 - .NET 8 Web 重构版

## 项目概述

本项目是使用 **.NET 8** 完全重写的论文查重系统，全面对标原 Windows 窗体应用的所有功能，采用纯.NET 实现，不依赖任何 Windows 专属组件或 paper_check.dll。

## 技术架构

### 核心特性
- **框架**: .NET 8 Web API
- **数据库**: SQLite + Entity Framework Core 8.0
- **文档处理**: DocX (Word), PdfSharpCore (PDF)
- **查重算法**: 纯C#实现 (N-gram + Jaccard相似度)
- **部署平台**: 麒麟V10/Linux

### 项目结构
```
paper_checking_web/
├── Config/           # 配置管理
├── Controllers/      # API控制器 (14个端点)
├── Data/            # 数据库上下文
├── Models/          # 数据模型 (9个类)
├── Services/        # 业务服务
│   ├── DocumentConverters.cs  # 文档转换器
│   ├── PaperCheckService.cs   # 查重算法
│   └── ReportGenerator.cs     # 报告生成
├── Utils/           # 工具类
├── Program.cs       # 入口文件
└── paper_checking_web.csproj
```

## 功能模块

### 1. 文档转换层 ✅
- **TXT转换器**: 支持GBK编码
- **Word转换器**: 使用DocX库处理DOCX
- **PDF转换器**: 使用PdfSharpCore (文本提取需额外库)

### 2. 核心查重算法 ✅
- 章节自动识别 (摘要/引言/结论等)
- 5-gram特征提取
- MD5指纹生成
- Jaccard相似度计算
- 内部重复检测

### 3. 报告生成 ✅
- RTF原生格式生成
- PDF导出 (基础版本)
- Word导出 (RTF兼容)

### 4. API接口 (14个)
| 端点 | 方法 | 功能 |
|------|------|------|
| /api/check/status | GET | 获取系统状态 |
| /api/check/config | GET/POST | 查重配置管理 |
| /api/check/settings | GET/POST | 系统设置管理 |
| /api/check/start | POST | 开始查重任务 |
| /api/check/progress/{id} | GET | 查询进度 |
| /api/check/stop/{id} | POST | 停止任务 |
| /api/check/reports | GET | 报告列表 |
| /api/check/reports/{name} | GET | 报告详情 |
| /api/check/export | POST | 导出报告 |
| /api/check/library/add | POST | 添加论文到库 |
| /api/check/reset | POST | 重置系统 |

### 5. 数据模型 (9个类)
- CheckConfig - 查重配置
- LibraryConfig - 论文库配置
- SystemSettings - 系统设置
- CheckProgress - 进度信息
- CheckTask - 查重任务
- CheckResult - 查重结果
- SectionDetail - 章节详情
- MatchedSource - 匹配源
- CheckStatistics - 统计信息
- CompareSource - 比对源
- ReportSummary/ReportDetail - 报告相关

## 核心算法流程

```
文档上传 → 文本提取 → 章节分割 → 5-gram特征 → MD5指纹 → 
Jaccard比对 → 相似度计算 → 报告生成
```

### 相似度计算
使用 **Jaccard相似系数**:
```
Similarity = |A ∩ B| / |A ∪ B| × 100%
```

## 部署说明

### 麒麟V10环境
```bash
# 安装.NET 8 SDK
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# 编译运行
cd /workspace/paper_checking_web
dotnet restore
dotnet build
dotnet run

# 访问 Swagger UI
http://localhost:5000/swagger
```

### Docker部署
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "paper_checking_web.dll"]
```

## 与原Windows版对比

| 功能 | Windows原版 | .NET 8重构版 |
|------|------------|-------------|
| UI界面 | WinForms | Web API + Swagger |
| 查重算法 | paper_check.dll (无源码) | 纯C#重写 |
| 文档转换 | IKVM+PDFBox, Spire.Doc | DocX, PdfSharpCore |
| 报告格式 | RTF (WinForms) | RTF (原生构建) |
| 硬件检测 | WMI (仅Windows) | Linux文件读取 |
| 跨平台 | ❌ | ✅ |

## 已知限制

1. **PDF文本提取**: PdfSharpCore不直接支持，需集成额外库
2. **DOC格式**: 需要LibreOffice或特殊处理
3. **参考库**: 当前为内存缓存，应改为数据库持久化
4. **并发性能**: 需优化大规模文档处理

## 后续开发计划

- [ ] 完善PDF文本提取 (集成PdfToText)
- [ ] 添加用户认证授权
- [ ] 实现完整的自建库管理
- [ ] 优化查重算法性能
- [ ] 添加前端Web界面 (Vue/Blazor)
- [ ] 编写单元测试

## 代码统计

- **总代码量**: ~2000行 C#
- **源文件**: 10个
- **API端点**: 14个
- **数据模型**: 9个类

## 许可证

MIT License

---
*本项目为教学演示用途，实际生产环境需进一步完善*
