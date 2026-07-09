# Qiyana

英雄联盟战绩查询桌面工具（Avalonia UI / .NET 10）

## 构建与运行

```powershell
# 调试运行
dotnet run

# 发布为单个 exe（无需 .NET 运行时）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

输出在 `dist\qiyana.exe`。
