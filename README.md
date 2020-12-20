<h1 align="center">Package Downloader</h1>
<div align="center">
一款用于网络隔离的开发环境情况下方便进行包及其依赖项下载的软件。
</div>


## 🔨 系统功能

使用时在互联网部署一套系统，选择需要下载的包类型，通过界面配置参数即可完成包及其依赖性下载，服务器下载完成后提供zip包链接下载。
目前支持以下三类下载。

- 🎁 NuGet包下载
- 💎 Npm包下载
- 🏗️ 容器镜像下载

## 📌 安装部署

支持Docker直接部署

1. Docker pull
`docker push huangzhtao/package-downloader:latest`

2. Docker Run。由于需要在docker内使用docker命令，需要支持docker in docker。
`docker run -d -p 5000:80 -v /var/run/docker.sock:/var/run/docker.sock --restart=always --name PackageDownloader huangzhtao/package-downloader:latest`

## 👉 使用说明

- NuGet包下载参数

![image](https://raw.githubusercontent.com/huangzhtao/PackageDownloader/main/assets/NuGet.png)

1. Package： 输入要下载的包名，输入三个字符以上后自动连接Repostory进行搜索并提示。
2. Version： 根据Repostory返回的版本提供用户进行选择，默认选择最新版本。
3. Include Pre-released：是否包含预发布版本，默认选择“否”。
4. Include Dependency：是否包含依赖，默认选择“是”。
5. Repostory：服务器地址，默认为官方地址（https://api.nuget.org/v3/index.json ）。
6. Target：依赖的目标框架版本，ALL或者输入需要的目标框架版本。

- Npm包下载参数

![image](https://raw.githubusercontent.com/huangzhtao/PackageDownloader/main/assets/Npm.PNG)

1. Package： 输入要下载的包名，输入三个字符以上后自动连接Registry进行搜索并提示。
2. Version：根据Registry返回的版本提供用户进行选择，默认选择最新版本。
3. Pre-released：是否包含预发布版本，默认选择“否”。
4. Dependency：是否包含依赖，默认选择“是”。
5. Registry：服务器地址，默认为官方地址（https://registry.npmjs.org ）。
6. Dev-Dependency：是否包含开发依赖，默认选择“否”。
7. Dependency Depth：考虑到开发依赖数量庞大，因此设置下载深度，-1为不下载，建议不要选择太深，以免下载时间过长。

- 容器镜像下载参数

![image](https://raw.githubusercontent.com/huangzhtao/PackageDownloader/main/assets/Container.PNG)

1. NAME[:TAG|@DIGEST]：仅一个参数，即需要拉的镜像的信息。

- 下载窗口

![image](https://raw.githubusercontent.com/huangzhtao/PackageDownloader/main/assets/Download.PNG)

1. 下载窗口将会展示下载的过程及状态。
2. 下载完成后将在Download url部分展示下载的信息，点击即可下载完成的压缩包。

## 🔗 使用技术

- [.Net 5](https://dotnet.microsoft.com/download/dotnet/5.0)
- [Blazorise](https://github.com/stsrki/Blazorise)
- [SemVer](https://github.com/adamreeve/semver.net)
