﻿using Newtonsoft.Json;
using NLog;
using Refit;
using SyncTrayzor.SyncThing.Api;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingApiClient
    {
        void SetConnectionDetails(Uri baseAddress, string apiKey);

        Task ShutdownAsync();
        Task<List<Event>> FetchEventsAsync(int since, int? limit = null);
        Task<Config> FetchConfigAsync();
        Task ScanAsync(string folderId, string subPath);
        Task<SystemInfoResponse> FetchSystemInfoAsync();
        Task<ConnectionsResponse> FetchConnectionsAsync();
        Task<SyncthingVersionResponse> FetchVersionAsync();
        Task<IgnoresResponse> FetchIgnoresAsync(string folderId);
        Task RestartAsync();
        Task<FolderModelResponse> FetchFolderModelAsync(string folderId);
    }

    public class SyncThingApiClient : ISyncThingApiClient
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private ISyncThingApi api;

        public void SetConnectionDetails(Uri baseAddress, string apiKey)
        {
            var httpClient = new HttpClient(new AuthenticatedHttpClientHandler(apiKey))
            {
                BaseAddress = baseAddress.NormalizeZeroHost(),
                Timeout = TimeSpan.FromSeconds(70),
            };
            this.api = RestService.For<ISyncThingApi>(httpClient, new RefitSettings()
            {
                JsonSerializerSettings = new JsonSerializerSettings()
                {
                    Converters = { new EventConverter() }
                },
            });
        }

        public Task ShutdownAsync()
        {
            logger.Info("Requesting API shutdown");
            this.EnsureSetup();
            return this.api.ShutdownAsync();
        }

        public Task<List<Event>> FetchEventsAsync(int since, int? limit)
        {
            this.EnsureSetup();
            if (limit == null)
                return this.api.FetchEventsAsync(since);
            else
                return this.api.FetchEventsLimitAsync(since, limit.Value);
        }

        public async Task<Config> FetchConfigAsync()
        {
            this.EnsureSetup();
            var config = await this.api.FetchConfigAsync();
            logger.Debug("Fetched configuration: {0}", config);
            return config;
        }

        public Task ScanAsync(string folderId, string subPath)
        {
            logger.Debug("Scanning folder: {0} subPath: {1}", folderId, subPath);
            this.EnsureSetup();
            return this.api.ScanAsync(folderId, subPath);
        }

        public async Task<SystemInfoResponse> FetchSystemInfoAsync()
        {
            this.EnsureSetup();
            var systemInfo = await this.api.FetchSystemInfoAsync();
            logger.Debug("Fetched system info: {0}", systemInfo);
            return systemInfo;
        }

        public Task<ConnectionsResponse> FetchConnectionsAsync()
        {
            this.EnsureSetup();
            return this.api.FetchConnectionsAsync();
        }

        public async Task<SyncthingVersionResponse> FetchVersionAsync()
        {
            this.EnsureSetup();
            var version = await this.api.FetchVersionAsync();
            logger.Debug("Fetched version: {0}", version);
            return version;
        }

        public async Task<IgnoresResponse> FetchIgnoresAsync(string folderId)
        {
            this.EnsureSetup();
            var ignores = await this.api.FetchIgnoresAsync(folderId);
            logger.Debug("Fetched ignores for folderid {0}: {1}", folderId, ignores);
            return ignores;
        }

        public Task RestartAsync()
        {
            this.EnsureSetup();
            logger.Debug("Restarting Syncthing");
            return this.api.RestartAsync();
        }

        public async Task<FolderModelResponse> FetchFolderModelAsync(string folderId)
        {
            this.EnsureSetup();
            var folderModel = await this.api.FetchFolderModelAsync(folderId);
            logger.Debug("Fethed folder model for {0}: {1}", folderId, folderModel);
            return folderModel;
        }

        private void EnsureSetup()
        {
            if (this.api == null)
                throw new InvalidOperationException("SetConnectionDetails not called");
        }

        private class AuthenticatedHttpClientHandler : WebRequestHandler
        {
            private readonly string apiKey;

            public AuthenticatedHttpClientHandler(string apiKey)
            {
                this.apiKey = apiKey;
                // We expect Syncthing to return invalid certs
                this.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                request.Headers.Add("X-API-Key", this.apiKey);
                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}
