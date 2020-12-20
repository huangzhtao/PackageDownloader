<h1 align="center">Package Downloader</h1>
<div align="center">
A toolkit for downloading packages and their dependencies for an isolated development environment.
</div>


## 🔨 Feature

Deploy the system on an Internet-connected server, select the type of package to download, configure the parameters through the interface to download the package and its dependency. After the server downloads, the zip package link is provided for download.Deploy the system on an Internet-connected server, select the type of package to download, configure the parameters through the interface to download the package and its dependency. After the server downloads, the zip package link is provided for download.

- 🎁 NuGet Package Download
- 💎 Npm Package Download
- 🏗️ Container Image Download

## 📌 Deployment

Docker deployment is supported.

1. Docker pull
`docker push huangzhtao/package-downloader:latest`

2. Docker Run, docker in docker configuration required。
`docker run -d -p 5000:80 -v /var/run/docker.sock:/var/run/docker.sock --restart=always --name PackageDownloader huangzhtao/package-downloader:latest`

## 👉 Usage

- NuGet download parameters

![image](https://raw.githubusercontent.com/huangzhtao/PackageDownloader/main/assets/NuGet.png)

1. Package: Enter the name of the package to be downloaded. After entering more than three characters, it will automatically connect to the repository to search and prompt.
2. Version: Users can select the version returned by the repository, the latest version by default.
3. Pre-released: Include pre-release version, "no" by default.
4. Dependency: Include dependency, "no" by default.
5. Repository: Repository url, default is the official address ( https://api.nuget.org/v3/index.json ).
6. Target：Target framework of the dependency, "ALL" or enter the desired version of the target framework.

- Npm download parameters

![image](https://raw.githubusercontent.com/huangzhtao/PackageDownloader/main/assets/Npm.PNG)

1. Package: Enter the name of the package to be downloaded. After entering more than three characters, it will automatically connect to the registry to search and prompt.
2. Version: Users can select the version returned by the registry, the latest version by default.
3. Pre-released: Include pre-release version, "no" by default.
4. Dependency: Include dependency, "no" by default.
5. Registry: Registry url, default is the official address ( https://registry.npmjs.org ).
6. Dev-Dependency: Include development dependency, "no" by default.
7. Dependency Depth: The default setting "-1" is not downloading any development dependencies. It is recommended not to select too deep to avoid too long download time.

- Container image download parameters

![image](https://raw.githubusercontent.com/huangzhtao/PackageDownloader/main/assets/Container.PNG)

1. NAME[:TAG|@DIGEST]: Only one parameter, which is the desired image to be pulled.

- Download window

![image](https://raw.githubusercontent.com/huangzhtao/PackageDownloader/main/assets/Download.PNG)

1. The download window will show the download process and status.
2. After the download is completed, the download URL will be displayed. Click to download the completed compressed package.

## 🔗 Technology

- [.Net 5](https://dotnet.microsoft.com/download/dotnet/5.0)
- [Blazorise](https://github.com/stsrki/Blazorise)
- [SemVer](https://github.com/adamreeve/semver.net)
