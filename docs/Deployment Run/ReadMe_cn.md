因为目前很多人不知道怎么样运行，或者运行报错，故今天写下这样一个教程。

首先第一步先获取代码，可以通过命令行或者SourceTree下载源码，我这里主要介绍SourceTree的下载源码方法:

![SourceTree](https://images.gitee.com/uploads/images/2019/0909/191331_59109574_130171.jpeg "1.JPG")

通过文件下==>克隆/新建或者是标签栏+号进入上图所示窗体，按照圈中所圈填写和操作即可将源码同步到本地指定目录。我的是在“L:\完成项目\智能聊天机器人\BotSharp\Net core 2.2”文件夹中，后面会以此文件夹路径来做演示。

进入BotSharp.WebHost文件夹并在地址栏里输入cmd并回车，即弹出cmd窗口，输入dotnet watch run命令会运行项目。

![项目运行](https://images.gitee.com/uploads/images/2019/0909/191850_a4f0fd89_130171.jpeg "2.JPG")

注意：上图中红色字体显示部分可以不用理会，如果有强迫症的话，可以在BotSharp.WebHost右键引入BotSharp.Platform.Rasa和BotSharp.Platform.Articulate这两个项目，或者在BotSharp.WebHost/Settings/app.json中删除下图中圈中部分

![移除红色项目](https://images.gitee.com/uploads/images/2019/0909/192213_87385c4d_130171.jpeg "3.JPG")

接下来我们通过第二张图片上运行之后的地址打开查看Swagger的接口文档是否正常显示。

![运行成功](https://images.gitee.com/uploads/images/2019/0909/192612_6aceb74e_130171.jpeg "4.JPG")

如图所示，我们运行成功了。


接下来开始通过上面的接口文档在线导入和训练并测试代理。

先导入训练代理所需要的压缩包，里面包含有语料库

![接口操作](https://images.gitee.com/uploads/images/2019/0909/193255_897b251c_130171.jpeg "5.JPG")

![接口操作](https://images.gitee.com/uploads/images/2019/0909/193349_38366ca2_130171.jpeg "6.JPG")

![接口操作](https://images.gitee.com/uploads/images/2019/0909/194412_edf0f024_130171.jpeg "7.JPG")

上图中圈中的部分要记录下来，接下来会有用。其中id是刚导入文档生成的代理ID，clientAccessToken是授权ID，训练代理模型是需要这个授权ID的。

![输入图片说明](https://images.gitee.com/uploads/images/2019/0909/195938_0b68c3c4_130171.jpeg "8.JPG")

![输入图片说明](https://images.gitee.com/uploads/images/2019/0909/195950_18b84a44_130171.jpeg "9.JPG")

![输入图片说明](https://images.gitee.com/uploads/images/2019/0909/200000_34596964_130171.jpeg "10.JPG")

上面几张图片的操作就是保存当前接口所需要授权信息的方法，尤其是要注意圈中部分，另Bearer后面跟一个空格再跟上clientAccessToken的值，且训练不同的代理这个clientAccessToken的值也不一样。

在下图中填入前面生成的代理ID并点击Execute执行即可

![输入图片说明](https://images.gitee.com/uploads/images/2019/0909/200258_7ea18c61_130171.jpeg "11.JPG")

然后按下面图片操作输入测试数据执行输出

![输入图片说明](https://images.gitee.com/uploads/images/2019/0909/204756_99e489c3_130171.jpeg "12.JPG")

![输入图片说明](https://images.gitee.com/uploads/images/2019/0909/204939_6b75f8cd_130171.png "屏幕截图.png")

上图中圈中部分即为执行得到的结果