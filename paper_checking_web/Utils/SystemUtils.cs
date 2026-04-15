using System.Security.Cryptography;
using System.Text;

namespace paper_checking_web.Utils;

/// <summary>
/// 系统工具类 - Linux 兼容版本
/// </summary>
public static class SystemUtils
{
    /// <summary>
    /// 获取 MAC 地址 (Linux 兼容)
    /// </summary>
    public static string GetMacAddress()
    {
        try
        {
            // 在 Linux 上读取网络接口信息
            if (OperatingSystem.IsLinux())
            {
                var networkInterfaces = Directory.GetFiles("/sys/class/net");
                foreach (var iface in networkInterfaces)
                {
                    var addressPath = Path.Combine(iface, "address");
                    if (File.Exists(addressPath))
                    {
                        var mac = File.ReadAllText(addressPath).Trim();
                        if (!string.IsNullOrEmpty(mac) && mac != "00:00:00:00:00:00")
                        {
                            return mac.ToUpper().Replace(':', '-');
                        }
                    }
                }
            }
            
            // 回退方案：使用 .NET API
            var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (var nic in nics)
            {
                if (nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                {
                    var mac = nic.GetPhysicalAddress().ToString();
                    if (!string.IsNullOrEmpty(mac))
                    {
                        return string.Join("-", Enumerable.Range(0, mac.Length / 2)
                            .Select(i => mac.Substring(i * 2, 2)).ToArray());
                    }
                }
            }
        }
        catch
        {
            // 忽略错误
        }
        
        return "00-00-00-00-00-00";
    }

    /// <summary>
    /// 获取磁盘信息 (Linux 兼容)
    /// </summary>
    public static string GetDiskInfo()
    {
        try
        {
            if (OperatingSystem.IsLinux())
            {
                // 读取 /dev/sda 或 /dev/nvme0n1 信息
                var diskDevices = new[] { "/dev/sda", "/dev/nvme0n1", "/dev/vda" };
                foreach (var disk in diskDevices)
                {
                    if (File.Exists(disk))
                    {
                        // 尝试读取模型信息
                        var modelPath = $"/sys/block/{Path.GetFileName(disk)}/device/model";
                        if (File.Exists(modelPath))
                        {
                            return File.ReadAllText(modelPath).Trim();
                        }
                        return Path.GetFileName(disk);
                    }
                }
            }
            
            // Windows 回退方案（如果需要）
            // 可以使用 System.Management 但需要额外包
        }
        catch
        {
            // 忽略错误
        }
        
        return "unknown";
    }

    /// <summary>
    /// 获取 CPU 核心数
    /// </summary>
    public static int GetProcessorCount()
    {
        return Environment.ProcessorCount;
    }

    /// <summary>
    /// 创建 MD5 哈希
    /// </summary>
    public static string CreateMD5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        
        var sb = new StringBuilder();
        for (int i = 0; i < 16; i++)
        {
            if (i < hashBytes.Length)
                sb.Append(hashBytes[i].ToString("x2"));
            else
                sb.Append("xx");
        }
        return sb.ToString().ToLower();
    }

    /// <summary>
    /// RSA 签名验证
    /// </summary>
    public static bool VerifySignature(string publicKey, string hashData, string signature)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKey);
            
            var deformatterData = Convert.FromBase64String(signature);
            var hashbyteDeformatter = Convert.FromBase64String(CreateMD5Hash(hashData));
            
            using var sha512 = SHA512.Create();
            var hashedData = sha512.ComputeHash(hashbyteDeformatter);
            
            return rsa.VerifyData(hashedData, deformatterData, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// AES 加密
    /// </summary>
    public static string AesEncrypt(string rawInput, byte[] key, byte[] iv)
    {
        if (string.IsNullOrEmpty(rawInput))
            return string.Empty;
        
        using var rijndael = Aes.Create();
        rijndael.Key = key;
        rijndael.IV = iv;
        rijndael.KeySize = 256;
        rijndael.BlockSize = 128;
        rijndael.Mode = CipherMode.CBC;
        rijndael.Padding = PaddingMode.PKCS7;
        
        using var transform = rijndael.CreateEncryptor(key, iv);
        var inputBytes = Encoding.UTF8.GetBytes(rawInput);
        var encryptedBytes = transform.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
        
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// 递归获取目录下所有文件
    /// </summary>
    public static List<FileInfo> GetFilesRecursively(DirectoryInfo directory)
    {
        var result = new List<FileInfo>();
        try
        {
            foreach (var file in directory.GetFiles())
            {
                result.Add(file);
            }
            
            foreach (var subDir in directory.GetDirectories())
            {
                result.AddRange(GetFilesRecursively(subDir));
            }
        }
        catch
        {
            // 忽略访问权限错误
        }
        
        return result;
    }

    /// <summary>
    /// 清理文件名中的非法字符
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        // 保留中文字符和常见标点
        var validName = Regex.Replace(fileName, @"[^\u4e00-\u9fa5\u0022\《\》\（\）\—\；\，\。\\""！\#\\_\-\.\,\:\(\)\'\[\]\【\】\+\·\：\<\>\w]", string.Empty);
        
        // 移除路径非法字符
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            validName = validName.Replace(invalidChar.ToString(), string.Empty);
        }
        
        return validName;
    }
}
