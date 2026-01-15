#!/usr/bin/env dotnet
#:package PdfSharpCore@1.3.65
#:package Spectre.Console@0.49.1
#:property PublishAot=true

using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Spectre.Console;
using System;
using System.IO;

// ==================== 参数校验 ====================
if (args.Length < 2)
{
    AnsiConsole.MarkupLine("[red]错误: 参数不足[/]");
    AnsiConsole.MarkupLine("[yellow]用法: dotnet split-pdf.cs <PDF文件> <输出目录> [页面范围][/]");
    AnsiConsole.MarkupLine("[gray]示例: dotnet split-pdf.cs input.pdf ./output/[/]");
    AnsiConsole.MarkupLine("[gray]      dotnet split-pdf.cs input.pdf ./output/ 1-5[/]");
    return 1;
}

var pdfPath = args[0];
var outputDir = args[1];
var pageRange = args.Length >= 3 ? args[2] : null;

// 验证 PDF 文件
if (!File.Exists(pdfPath))
{
    AnsiConsole.MarkupLine($"[red]错误: 文件不存在: {pdfPath}[/]");
    return 1;
}

// 创建输出目录
if (!Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
    AnsiConsole.MarkupLine($"[green]✓[/] 创建目录: {outputDir}");
}

// ==================== 拆分 PDF ====================
try
{
    AnsiConsole.MarkupLine($"[cyan]📄 处理文件:[/] {Path.GetFileName(pdfPath)}");
    AnsiConsole.MarkupLine($"[cyan]📂 输出目录:[/] {outputDir}");

    using var inputDocument = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Import);
    var totalPages = inputDocument.PageCount;
    
    // 解析页面范围
    int startPage = 1, endPage = totalPages;
    if (!string.IsNullOrEmpty(pageRange))
    {
        var parts = pageRange.Split('-');
        if (parts.Length == 2 && 
            int.TryParse(parts[0], out startPage) && 
            int.TryParse(parts[1], out endPage))
        {
            startPage = Math.Max(1, Math.Min(startPage, totalPages));
            endPage = Math.Max(startPage, Math.Min(endPage, totalPages));
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]警告: 无效的页面范围 '{pageRange}'，将拆分所有页面[/]");
            startPage = 1;
            endPage = totalPages;
        }
    }

    AnsiConsole.MarkupLine($"[blue]ℹ️  总页数:[/] {totalPages}");
    AnsiConsole.MarkupLine($"[blue]ℹ️  拆分范围:[/] 第 {startPage} - {endPage} 页");
    Console.WriteLine();

    var baseName = Path.GetFileNameWithoutExtension(pdfPath);
    var savedCount = 0;

    await AnsiConsole.Progress()
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("[green]拆分 PDF 页面[/]", maxValue: endPage - startPage + 1);

            for (int i = startPage; i <= endPage; i++)
            {
                task.Description = $"[green]拆分第 {i}/{endPage} 页[/]";

                // 创建单页 PDF
                using var outputDocument = new PdfDocument();
                outputDocument.AddPage(inputDocument.Pages[i - 1]);

                var outputPath = Path.Combine(outputDir, $"{baseName}_page_{i:D3}.pdf");
                outputDocument.Save(outputPath);
                
                savedCount++;
                AnsiConsole.MarkupLine($"  [gray]✓ 已保存: {Path.GetFileName(outputPath)}[/]");

                task.Increment(1);
                await Task.CompletedTask;
            }
        });

    Console.WriteLine();
    AnsiConsole.MarkupLine($"[green]✅ 拆分完成![/]");
    AnsiConsole.MarkupLine($"[gray]已生成 {savedCount} 个 PDF 文件[/]");
    AnsiConsole.MarkupLine($"[gray]保存位置: {Path.GetFullPath(outputDir)}[/]");

    return 0;
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]❌ 错误: {ex.Message}[/]");
    AnsiConsole.WriteException(ex);
    return 1;
}
