using System.Text;
using System.Text.RegularExpressions;

namespace paper_checking_web.Services;

/// <summary>
/// 文档转换器接口
/// </summary>
public interface IDocumentConverter
{
    string? ConvertToString(string filePath, string blockText);
    Task<string> ConvertToTextAsync(string filePath);
    bool SupportsExtension(string extension);
}

/// <summary>
/// 文本文件转换器
/// </summary>
public class TxtConverter : IDocumentConverter
{
    public bool SupportsExtension(string extension)
    {
        return extension.Equals("txt", StringComparison.OrdinalIgnoreCase);
    }

    public string? ConvertToString(string filePath, string blockText)
    {
        try
        {
            var text = File.ReadAllText(filePath, Encoding.GetEncoding("GBK"));
            return TextFormat(text, blockText);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> ConvertToTextAsync(string filePath)
    {
        var result = ConvertToString(filePath, string.Empty);
        if (result == null)
            throw new Exception($"无法读取文件：{filePath}");
        
        return await Task.FromResult(result);
    }

    protected string TextFormat(string text, string blockText)
    {
        if (string.IsNullOrEmpty(blockText))
            return text;

        var blocks = blockText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var block in blocks)
        {
            text = text.Replace(block.Trim(), string.Empty);
        }
        return text;
    }
}

/// <summary>
/// Word 文档转换器 (使用 DocX)
/// </summary>
public class WordConverter : IDocumentConverter
{
    public bool SupportsExtension(string extension)
    {
        return extension.Equals("docx", StringComparison.OrdinalIgnoreCase);
    }

    public string? ConvertToString(string filePath, string blockText)
    {
        try
        {
            // 使用 DocX 库读取 DOCX 文件
            using var doc = DocX.Load(filePath);
            var text = doc.Text;
            
            // 清理文本：替换换行符，只保留中文和标点
            text = text.Replace("#", "").Replace('\r', '#').Replace('\n', '#');
            text = Regex.Replace(text, @"[^\u4e00-\u9fa5\《\》\（\）\——\；\，\。\""\！\#]", "");
            text = new Regex("[#]+").Replace(text, "@@").Trim();
            
            return TextFormat(text, blockText);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> ConvertToTextAsync(string filePath)
    {
        var result = ConvertToString(filePath, string.Empty);
        if (result == null)
            throw new Exception($"无法读取文件：{filePath}");
        
        return await Task.FromResult(result);
    }

    protected string TextFormat(string text, string blockText)
    {
        if (string.IsNullOrEmpty(blockText))
            return text;

        var blocks = blockText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var block in blocks)
        {
            text = text.Replace(block.Trim(), string.Empty);
        }
        return text;
    }
}

/// <summary>
/// PDF 文档转换器 (使用 PdfSharpCore)
/// </summary>
public class PdfConverter : IDocumentConverter
{
    public bool SupportsExtension(string extension)
    {
        return extension.Equals("pdf", StringComparison.OrdinalIgnoreCase);
    }

    public string? ConvertToString(string filePath, string blockText)
    {
        try
        {
            // 使用 PdfSharpCore 读取 PDF 文件
            using var document = PdfSharp.Pdf.IO.PdfReader.Open(filePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            var text = new StringBuilder();
            
            for (int i = 0; i < document.PageCount; i++)
            {
                var page = document.Pages[i];
                // PdfSharpCore 不直接支持文本提取，需要使用其他方法
                // 这里暂时返回空字符串，实际项目中可以使用 PdfToText 或其他库
                text.Append("[PDF 文本提取需要额外库支持] ");
            }
            
            var result = text.ToString();
            if (string.IsNullOrEmpty(result))
                return null;
                
            return TextFormat(result, blockText);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> ConvertToTextAsync(string filePath)
    {
        var result = ConvertToString(filePath, string.Empty);
        if (result == null)
            throw new Exception($"无法读取文件：{filePath}");
        
        return await Task.FromResult(result);
    }

    protected string TextFormat(string text, string blockText)
    {
        if (string.IsNullOrEmpty(blockText))
            return text;

        var blocks = blockText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var block in blocks)
        {
            text = text.Replace(block.Trim(), string.Empty);
        }
        return text;
    }
}

/// <summary>
/// 转换器工厂
/// </summary>
public class ConverterFactory
{
    private readonly List<IDocumentConverter> _converters;

    public ConverterFactory()
    {
        _converters = new List<IDocumentConverter>
        {
            new TxtConverter(),
            new WordConverter(),
            new PdfConverter()
        };
    }

    public IDocumentConverter? GetConverter(string extension, SystemSettings settings)
    {
        var lowerExt = extension.ToLowerInvariant();
        
        if (lowerExt == "txt" && settings.SupportTxt)
            return _converters.FirstOrDefault(c => c.SupportsExtension("txt"));
        
        if (lowerExt == "docx" && settings.SupportDocx)
            return _converters.FirstOrDefault(c => c.SupportsExtension("docx"));
        
        if (lowerExt == "doc" && settings.SupportDoc)
        {
            // DOC 格式需要特殊处理，暂时返回 null
            // 实际项目中可以使用 Spire.Doc 或调用 LibreOffice
            return null;
        }
        
        if (lowerExt == "pdf" && settings.SupportPdf)
            return _converters.FirstOrDefault(c => c.SupportsExtension("pdf"));
        
        return null;
    }
}
