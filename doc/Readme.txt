ClownFish.Mvc 是什么？
====================================
ClownFish.Mvc 的前身是 MyMVC项目，您可以查看我的博客链接简单的了解MyMVC的初始功能：
http://www.cnblogs.com/fish-li/archive/2012/02/12/2348395.html
http://www.cnblogs.com/fish-li/archive/2012/02/21/2361982.html

ClownFish.Mvc 是一个运行在ASP.NET平台下的MVC框架，可以让您开发出完全符合MVC规范的应用程序，
它与ASP.NET MVC & WEB API框架有着类似的功能，但更简单，更容易学习，可以满足绝大多数的WEB开发需求，
下面是它支持的部分特性列表：
-支持Contrller, View, Model分离
-支持ASP.NET Routing
-支持BigPipe
-支持Action自匹配（配合jQuery.form开发AJAX非常简单）
-支持Service开发（XML, JSON请求/响应）
-支持HttpClient（简单且强大，支持同步/异步）
-支持Controller同名（适合于大项目开发）
-支持自定义权限验证
-支持OutputCache
-支持自定义Form数据的转换器
-支持跨域请求
-支持Razor
-支持await/async异步
-支持灰度发布（内建反向代理）
-支持强大的扩展特性（类型拦截，实例事件扩展订阅）
-支持404错误诊断
-封装了ASP.NET的文件监视，使用更简单


ClownFish.Mvc也有一些独有设计，可以简化开发任务，甚至实现一些微软框架没有的功能。
在持续优化的过程中，ClownFish.Mvc更关注应用程序的扩展性，例如，
-支持类型拦截
-支持实例事件扩展订阅

除此之外，ClownFish.Mvc还包含了我多年ASP.NET的开发经验，
已将我认为最有价值的功能，特性，开发模式融入其中，
留给大家的是一个简单，易学习，功能强大的WEB开发框架。




ClownFish.Mvc的版权问题
====================================
ClownFish.Mvc是一款免费的软件，可用于任何项目，没有任何版权限制。
由于ClownFish.Mvc的BUG造成的任何产品问题，也请自行承担。




如何在IIS中运行示例
====================================
请参考网址：
http://www.cnblogs.com/fish-li/archive/2012/02/26/2368989.html
或者参考：《配置 MyMVC 演示程序.docx》



DEMO对SQLSERVER的支持
====================================

DemoWebSite1网站现在可使用三种数据访问方式：
1. 早期的XML文件（读写全是一个文件）。
2. 用存储过程的方式访问SQLSERVER。
3. 用XmlCommand的方式访问SQLSERVER。


切换方式：
打开DemoWebSite1\web.config,找到appSettings配置节，参考里面的注释。



SQLSERVER数据库文件：
示例所需的MyNorthwind数据库文件的下载地址：http://files.cnblogs.com/fish-li/MyNorthwind.7z
关于数据库的配置可参考：http://www.cnblogs.com/fish-li/archive/2012/02/26/2368989.html


说明：
后面二种数据访问方式采用了ClownFish，它是一个通用的数据访问层，可支持多种数据库，
更多的ClownFish介绍可参考：http://www.cnblogs.com/fish-li/archive/2012/07/17/ClownFish.html


