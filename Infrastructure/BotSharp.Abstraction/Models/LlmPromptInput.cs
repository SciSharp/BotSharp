using Abstraction.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Abstraction.Models;

public class LlmPromptInput
{
    public string Model { get; set; } = string.Empty;
    public LlmResponseExtractType Format { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public List<string> Prompts { get; set; } = new List<string>();

    public override int GetHashCode()
    {
        var value = SessionId + "-" + string.Join(" | ", Prompts);
        return Md5(value).GetHashCode();
    }

    private string Md5(string input)
    {
        using var md5 = MD5.Create();
        var byteHash = md5.ComputeHash(Encoding.UTF8.GetBytes(input.ToLower()));
        return BitConverter.ToString(byteHash).Replace("-", "").ToLower();
    }

    public override string ToString()
    {
        return $"{SessionId}: {Prompts.FirstOrDefault()}";
    }
}
