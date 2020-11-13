﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using PackageDownloader.NuGet;
using PackageDownloader.Service.Interface;
using PackageDownloader.Shared.NuGet;
using PackageDownloader.Shared.Response;
using PackageDownloader.Shared.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PackageDownloader.Server.Hubs
{
    public class DownloadPackageHub : Hub<IDownloadPackageHubClient>
    {
        private IMemoryCache _cache;
        private IPackageService _packageService;

        public DownloadPackageHub(IMemoryCache memoryCache, IPackageService packageService)
        {
            _cache = memoryCache;
            _packageService = packageService;
        }

        public override Task OnConnectedAsync()
        {
            var clientIP = Context.GetHttpContext().Request.HttpContext.Connection.RemoteIpAddress;
            ConnectionInfo connectionInfo = new ConnectionInfo
            {
                connectionID = Context.ConnectionId,
                userIP = clientIP.ToString(),
                connectionTime = DateTime.Now
            };

            _cache.Set(connectionInfo.connectionID, connectionInfo);

            Console.WriteLine($"{connectionInfo.connectionTime}: {connectionInfo.connectionID} connected, {connectionInfo.userIP}");
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception e)
        {
            _cache.Remove(Context.ConnectionId);

            Console.WriteLine($"Disconnected {e?.Message} {Context.ConnectionId}");
            await base.OnDisconnectedAsync(e);
        }

        public async Task RequestToDownload(RequestDownloadNuGetInfo info)
        {
            ServerResponse response = new ServerResponse()
            {
                payload = new Dictionary<string, string>()
            };
            response.payload.Add("Status", "request received.");
            await SendResponse(response);

            string resourceName = await _packageService.DownloadPackageAsync(Context.ConnectionId, info);
            string downloadUrl = $"./download/file?name={resourceName}";

            response.payload.Clear();
            response.payload.Add("Status", "download completed.");
            response.payload.Add("DownloadUrl", downloadUrl);
            await SendResponse(response);
        }

        public async Task SendMessage(string message)
        {
            await Clients.Caller.Message(message);
        }

        public async Task SendResponse(ServerResponse response)
        {
            await Clients.Caller.Response(response);
        }
    }
}
