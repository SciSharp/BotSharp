---
name: split-pdf
description: Split PDF files into separate single-page documents or extract specific page ranges. Use when you need to divide a PDF into multiple files, extract particular pages, or process PDF pages individually. Works with multi-page PDF documents.
license: MIT
---

# Split PDF

将 PDF 文件拆分为多个单页文件或提取指定页面范围。

## 使用场景

- 将多页 PDF 拆分为独立的单页文件
- 提取 PDF 的特定页面范围
- 需要单独处理 PDF 各个页面时

## 使用方法

使用 `scripts/split-pdf.cs` 脚本进行 PDF 拆分：

### 拆分所有页面
```bash
dotnet scripts/split-pdf.cs input.pdf output-dir/
```

### 拆分指定页面范围
```bash
# 拆分第 1-5 页
dotnet scripts/split-pdf.cs input.pdf output-dir/ 1-5

# 拆分第 10-20 页
dotnet scripts/split-pdf.cs input.pdf output-dir/ 10-20
```

### 示例

```bash
# 将 document.pdf 的所有页面拆分到 pages/ 目录
dotnet scripts/split-pdf.cs document.pdf pages/

# 只提取前 3 页
dotnet scripts/split-pdf.cs document.pdf output/ 1-3
```

## 输出格式

拆分后的文件命名格式：`{原文件名}_page_{页码}.pdf`

例如，拆分 `report.pdf` 后会生成：
- `report_page_001.pdf`
- `report_page_002.pdf`
- `report_page_003.pdf`
- ...

## 依赖项

脚本使用以下 NuGet 包（已在脚本中声明）：
- **PdfSharpCore 1.3.65** - PDF 操作核心库
- **Spectre.Console 0.49.1** - 美化的控制台输出


## 注意事项

- 页码从 1 开始计数
- 如果指定的页面范围超出实际页数，会自动调整到有效范围
- 输出目录不存在时会自动创建
- 支持中文文件名和路径
