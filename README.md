# 班级点名器

一个面向 Windows 的轻量课堂点名工具，适合课堂提问、投屏抽取和随机分组。点一下开始滚动，再点一下停止并抽出结果；也可以切换到可拖动的小窗口，放在课件旁边使用。

项目提供 **C# / Windows Forms 桌面版**和 **Python / tkinter 源码版**。下载 Windows 成品即可使用，无需安装 Python；源码版便于学习和继续修改。

## 可以做什么

- **两种点名模式**：放回模式允许重复抽中；不放回模式在同一轮内不重复，点完后可重置。
- **自己的班级名单**：导入、编辑、保存 TXT 名单；支持一行一个名字，也支持逗号、分号分隔。重复名字会合并，同名同学可加编号区分。
- **小窗口点名**：可拖动，点击开始／停止，支持返回主界面；不放回模式显示剩余人数。
- **四季主题**：春日樱花、夏日海风、秋日枫糖、冬日初雪。
- **课堂趣味玩法**：一次连抽多人、按每组人数随机分组、抽取幸运星、倒计时后进入滚动点名。
- **本次运行的点名记录**：记录最近的抽取结果，最多保留 80 条。

程序不需要联网。发布包只提供 1—50 数字名单和虚构示例名单，自己的班级名单保存在本机。

## 下载与使用

在本仓库的 [Releases 下载页面](https://github.com/ni228740/class-roll-call/releases/latest) 选择：

- `class-roll-call-portable.zip`：免安装版。解压整个文件夹后，双击 `班级点名器.exe`。请保留同目录的 `name` 文件夹。
- `class-roll-call-setup.exe`：安装版。可选择安装目录，以及是否创建桌面快捷方式。

运行环境：Windows，需要 .NET Framework 4.x。Windows 成品使用 C# 实现，不依赖 Python。其他操作系统和 Windows 版本的兼容性尚未逐一验证。

使用步骤：

1. 先用默认数字名单体验，或导入自己的 TXT 名单。
2. 选择“放回点名”或“不放回点名”。
3. 点击“开始点名”，再点击“停止点名”获得结果。
4. 需要配合课件时，切换到缩小模式。

编辑名单后，可点击“应用编辑”，并通过“保存名单”保存为 TXT 文件。

## 名单格式与数据说明

推荐使用 UTF-8 编码的 TXT 文件，每行一个名字，例如：

```text
示例同学01
示例同学02
示例同学03
```

将 TXT 文件放入程序目录下的 `name` 文件夹，即可从名单选择框使用；也可以通过“导入 txt”读取其他位置的文件。

点名记录在内存中显示，不会自动保存为考勤表。这个工具用于随机抽取，不提供出勤统计或请假管理。

**卸载前请备份名单。** 安装版的卸载程序会删除安装目录中的 `name` 文件夹，包括自己保存到其中的名单。

## 从源码运行或构建

### C# 桌面版

在 Windows 上使用系统中的 .NET Framework 4.x 编译器构建：

```bat
build.bat
```

生成 `班级点名器.exe`。脚本使用 `%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe`，若找不到编译器，构建会停止并提示。

构建安装版：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build_installer.ps1
```

生成 `班级点名器_安装程序.exe`。安装器会把 `name` 中的数字名单及一份示例名单嵌入程序。自行发布时，请检查名单内容，避免将真实学生信息一起打包。

### Python 源码版

需要 Python 3 和可用的 tkinter：

```bat
python python_app\class_roll_call.py
```

如需打包 Python 版，可自行安装 PyInstaller 后运行：

```bat
python -m pip install pyinstaller
powershell -NoProfile -ExecutionPolicy Bypass -File .\build_python_exe.ps1
```

生成 `班级点名器_Python版.exe`。GitHub 提供的 Windows 成品为 C# 版，Python 打包流程尚未在本次发布准备中验证。

## 项目结构

```text
src/                         C# 主程序、安装器、卸载器源码
python_app/class_roll_call.py Python / tkinter 源码
name/                        数字名单与虚构示例名单
build.bat                    C# 构建入口
build.ps1                    C# 主程序构建脚本
build_installer.ps1           安装包构建脚本
build_python_exe.ps1          Python 打包脚本
```

## 反馈

欢迎通过 Issues 反馈使用问题。请说明系统版本、使用的是免安装版还是安装版、操作步骤和错误提示；上传截图前请遮挡真实班级名单。

## 开源许可

本项目采用 [MIT 许可证](LICENSE)，允许使用、修改和再分发，使用时请保留版权与许可声明。
