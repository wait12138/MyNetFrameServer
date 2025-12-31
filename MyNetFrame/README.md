# MyNetFrameServer
我的网络小框架-服务器端

这个要配合我的Unity客户端小框架使用，百度网盘地址：https://pan.baidu.com/s/1_GzZtgfqcprw2Fh9BEplyA?pwd=1234

Unity版本：2022.3.34f1c1 这个网络场景路径：Assets\Scenes\MyNetFrame.unity

演示讲解视频在B站，链接：https://www.bilibili.com/video/BV1LqyPBSEB2/

这个小框架只能说实现功能，多多少少也有一些发现的和没有发现的问题，各位大佬愿意的话可以看一看。

## 图形化界面（WinForms）

已为项目添加 Windows 图形化界面，方便启动/停止服务与查看日志：

- 启动方式：
	- 源码运行：
		```powershell
		dotnet run --project .\MyNetFrame.csproj -c Debug
		```
	- 可执行文件：构建后位于 `bin/Debug/net9.0-windows/`。
- 功能说明：
	- IP 与端口：默认 `127.0.0.1:8080`，可在窗口顶部修改。
	- 启动/停止：点击“启动”或“停止”按钮进行服务控制。
	- 日志输出：原控制台日志已重定向到右侧日志框。
	- 在线列表：点击“刷新在线列表”获取当前在线玩家稳定ID。

备注：核心网络逻辑未改动，GUI 仅负责控制与展示。
