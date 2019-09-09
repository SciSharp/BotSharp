因为目前很多人不知道怎么样运行，或者运行报错，故今天写下这样一个教程。

首先第一步先获取代码，可以通过命令行或者SourceTree下载源码，我这里主要介绍SourceTree的下载源码方法:

![SourceTree](https://images.gitee.com/uploads/images/2019/0909/191331_59109574_130171.jpeg "1.JPG")

通过文件下==>克隆/新建或者是标签栏+号进入上图所示窗体，按照圈中所圈填写和操作即可将源码同步到本地指定目录。我的是在“L:\完成项目\智能聊天机器人\BotSharp\Net core 2.2”文件夹中，后面会以此文件夹路径来做演示。

进入BotSharp.WebHost文件夹并在地址栏里输入cmd并回车，即弹出cmd窗口，输入dotnet watch run命令会运行项目。

![项目运行](https://images.gitee.com/uploads/images/2019/0909/191850_a4f0fd89_130171.jpeg "2.JPG")

注意：上图中红色字体显示部分可以不用理会，如果有强迫症的话，可以在BotSharp.WebHost右键引入BotSharp.Platform.Rasa和BotSharp.Platform.Articulate这两个项目，或者在BotSharp.WebHost/Settings/app.json中删除下图中圈中部分

![移除红色项目](https://images.gitee.com/uploads/images/2019/0909/192213_87385c4d_130171.jpeg "3.JPG")

接下来我们通过第二张图片上运行之后的地址打开查看Swagger的接口文档是否正常显示。