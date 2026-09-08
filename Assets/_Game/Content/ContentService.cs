using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LoveGame.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace LoveGame.Content
{
    [Serializable]
    public class ContentPackInfo
    {
        public string id;
        public string name;
        public string description;
        public string version = "1.0.0";
        public long sizeBytes;
        public string[] regionIds;
        public bool required;
        public string remoteUrl;
    }

    [Serializable]
    public class ContentManifestFile
    {
        public int schemaVersion;
        public string remoteContentBaseUrl;
        public ContentPackInfo[] packs;
    }

    public enum PackState { NotDownloaded, Downloading, Downloaded, UpdateRequired, Error }

    /// <summary>Storage abstraction so local-first blockouts and remote CDN packs share one API.</summary>
    public interface IContentProvider
    {
        string Name { get; }
        PackState GetState(string packId);
        /// <summary>Begins an async download; reports progress via events. Returns a handle.</summary>
        ContentHandle Download(string packId);
        void Delete(string packId);
        long CacheSizeBytes();
    }

    public sealed class ContentHandle
    {
        public string PackId;
        public bool IsRunning;
        public bool CancelRequested;
    }

    /// <summary>
    /// Content service: manifest-driven pack management (local core + optional remote packs),
    /// download manager backend with progress/retry/cancel/cache accounting and Wi-Fi-only policy.
    /// Cloudflare R2/CDN base URL is configuration, never a hardcoded credential.
    /// </summary>
    public sealed class ContentService : IGameService
    {
        public string ServiceName => "Content";

        public readonly List<ContentPackInfo> Packs = new List<ContentPackInfo>();
        public string RemoteContentBaseUrl { get; private set; } = "";
        IContentProvider _provider;
        readonly Dictionary<string, ContentHandle> _handles = new Dictionary<string, ContentHandle>();
        string _cacheRoot;

        public event System.Action<ContentPackInfo> PackStateChanged;

        public void Initialize()
        {
            _cacheRoot = Path.Combine(Application.persistentDataPath, "ContentCache");
            Directory.CreateDirectory(_cacheRoot);
            LoadManifest();
            _provider = new RemoteContentProvider(_cacheRoot, this);
            // core pack is always local
            foreach (var pack in Packs)
            {
                if (pack.required) MarkDownloaded(pack.id, pack.version);
                else if (SaveSystem.Current.content.downloadedPacks.Contains(pack.id)) MarkDownloaded(pack.id, FindVersion(pack.id));
            }
            Log.Info("Content", $"{Packs.Count} packs, remote base URL = '{RemoteContentBaseUrl}'");
        }

        void LoadManifest()
        {
            var text = Resources.Load<TextAsset>("Data/content_manifest");
            if (text == null) { Log.Warn("Content", "content_manifest.json missing - all content local"); return; }
            try
            {
                var file = JsonUtility.FromJson<ContentManifestFile>(text.text);
                if (file == null || file.packs == null) return;
                RemoteContentBaseUrl = string.IsNullOrEmpty(file.remoteContentBaseUrl) ? "" : file.remoteContentBaseUrl;
                Packs.Clear();
                foreach (var p in file.packs)
                    if (!string.IsNullOrEmpty(p.id)) Packs.Add(p);
            }
            catch (Exception e) { Log.Error("Content", $"manifest parse failed: {e.Message}"); }
        }

        string FindVersion(string packId)
        {
            var idx = SaveSystem.Current.content.downloadedPacks.IndexOf(packId);
            return idx >= 0 && idx < SaveSystem.Current.content.packVersions.Count ? SaveSystem.Current.content.packVersions[idx] : "1.0.0";
        }

        void MarkDownloaded(string packId, string version)
        {
            var list = SaveSystem.Current.content.downloadedPacks;
            if (!list.Contains(packId))
            {
                list.Add(packId);
                SaveSystem.Current.content.packVersions.Add(version);
            }
            else
            {
                var idx = list.IndexOf(packId);
                if (idx >= 0 && idx < SaveSystem.Current.content.packVersions.Count) SaveSystem.Current.content.packVersions[idx] = version;
            }
        }

        public void Tick(float delta) { }

        public ContentPackInfo GetPack(string packId)
        {
            foreach (var p in Packs) if (p.id == packId) return p;
            return null;
        }

        public PackState GetState(string packId)
        {
            if (_handles.TryGetValue(packId, out var handle) && handle.IsRunning) return PackState.Downloading;
            return _provider.GetState(packId);
        }

        public bool IsRegionContentReady(string regionId)
        {
            foreach (var pack in Packs)
            {
                if (pack.regionIds == null) continue;
                foreach (var r in pack.regionIds)
                {
                    if (r != regionId) continue;
                    var state = GetState(pack.id);
                    if (pack.required) return true;
                    if (state != PackState.Downloaded) return false;
                }
            }
            return true; // no pack maps to this region: it is core content
        }

        /// <summary>Starts a download. Enforces the Wi-Fi-only preference when on cellular.</summary>
        public bool StartDownload(string packId)
        {
            var pack = GetPack(packId);
            if (pack == null || GetState(packId) == PackState.Downloading) return false;
            if (GameConfig.Settings.wifiOnlyDownloads && Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
            {
                GameEvents.Publish(new NotificationEvent { Title = "Wi-Fi only", Body = "Enable downloads on mobile data in settings first.", Duration = 4f });
                return false;
            }
            if (StorageFreeBytes() < pack.sizeBytes * 2)
            {
                GameEvents.Publish(new NotificationEvent { Title = "Not enough storage", Body = "Free up space before downloading this pack.", Duration = 4f });
                return false;
            }
            var handle = _provider.Download(packId);
            if (handle != null) _handles[packId] = handle;
            return handle != null;
        }

        public void CancelDownload(string packId)
        {
            if (_handles.TryGetValue(packId, out var handle)) handle.CancelRequested = true;
        }

        public void DeletePack(string packId)
        {
            _provider.Delete(packId);
            SaveSystem.Current.content.downloadedPacks.Remove(packId);
            SaveSystem.Save();
            PackStateChanged?.Invoke(GetPack(packId));
        }

        public long CacheSizeBytes() => _provider.CacheSizeBytes();

        public static long StorageFreeBytes()
        {
            try
            {
                // persistentDataPath drive free space (approximate, platform-dependent)
                var drive = new DirectoryInfo(Application.persistentDataPath).Root.FullName;
                var driveInfo = new DriveInfo(drive);
                return driveInfo.IsReady ? driveInfo.TotalFreeSpace : long.MaxValue;
            }
            catch { return long.MaxValue; }
        }

        internal void ReportProgress(string packId, float progress)
        {
            GameEvents.Publish(new ContentDownloadProgressEvent { PackId = packId, Progress = progress });
        }

        internal void ReportFinished(string packId, bool success)
        {
            if (_handles.TryGetValue(packId, out var handle)) handle.IsRunning = false;
            if (success)
            {
                var pack = GetPack(packId);
                MarkDownloaded(packId, pack != null ? pack.version : "1.0.0");
                SaveSystem.Save();
            }
            GameEvents.Publish(new ContentDownloadFinishedEvent { PackId = packId, Success = success });
            PackStateChanged?.Invoke(GetPack(packId));
        }

        internal string CacheRoot => _cacheRoot;

        public void Shutdown() { }
    }

    /// <summary>
    /// Remote provider: file-based cache + UnityWebRequest downloads with retry.
    /// When RemoteContentBaseUrl is empty, everything resolves as local (blockout mode) -
    /// the same service seamlessly switches to CDN mode once a base URL is configured.
    /// </summary>
    public sealed class RemoteContentProvider : IContentProvider
    {
        readonly string _cacheRoot;
        readonly ContentService _owner;

        public string Name => "Remote";

        public RemoteContentProvider(string cacheRoot, ContentService owner)
        {
            _cacheRoot = cacheRoot;
            _owner = owner;
        }

        string PackPath(string packId) => Path.Combine(_cacheRoot, packId + ".pack");

        public PackState GetState(string packId)
        {
            if (File.Exists(PackPath(packId)))
            {
                var pack = _owner.GetPack(packId);
                if (pack != null && File.ReadAllText(PackPath(packId)).Contains(pack.version)) return PackState.Downloaded;
                return PackState.UpdateRequired;
            }
            return _owner.GetPack(packId) is { required: true } ? PackState.Downloaded : PackState.NotDownloaded;
        }

        public ContentHandle Download(string packId)
        {
            var pack = _owner.GetPack(packId);
            if (pack == null) return null;
            if (string.IsNullOrEmpty(_owner.RemoteContentBaseUrl) || string.IsNullOrEmpty(pack.remoteUrl))
            {
                // no CDN configured: mark local content as instantly available
                File.WriteAllText(PackPath(packId), pack.version + " local");
                _owner.ReportFinished(packId, true);
                return new ContentHandle { PackId = packId, IsRunning = false };
            }
            var handle = new ContentHandle { PackId = packId, IsRunning = true };
            Services.Host.Run(DownloadRoutine(pack, handle));
            return handle;
        }

        IEnumerator DownloadRoutine(ContentPackInfo pack, ContentHandle handle)
        {
            var url = $"{_owner.RemoteContentBaseUrl.TrimEnd('/')}/{pack.remoteUrl}";
            int attempts = 0;
            while (attempts < 3 && !handle.CancelRequested)
            {
                attempts++;
                using (var request = UnityWebRequest.Get(url))
                {
                    var op = request.SendWebRequest();
                    while (!op.isDone && !handle.CancelRequested)
                    {
                        _owner.ReportProgress(pack.id, request.downloadProgress);
                        yield return null;
                    }
                    if (handle.CancelRequested)
                    {
                        request.Abort();
                        _owner.ReportFinished(pack.id, false);
                        yield break;
                    }
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // atomic write
                        var tmp = PackPath(pack.id) + ".tmp";
                        File.WriteAllBytes(tmp, request.downloadHandler.data);
                        if (File.Exists(PackPath(pack.id))) File.Delete(PackPath(pack.id));
                        File.Move(tmp, PackPath(pack.id));
                        _owner.ReportProgress(pack.id, 1f);
                        _owner.ReportFinished(pack.id, true);
                        yield break;
                    }
                    Log.Warn("Content", $"download attempt {attempts} failed for {pack.id}: {request.error}");
                }
                yield return new WaitForSeconds(2f * attempts);
            }
            _owner.ReportFinished(pack.id, false);
        }

        public void Delete(string packId)
        {
            if (File.Exists(PackPath(packId))) File.Delete(PackPath(packId));
        }

        public long CacheSizeBytes()
        {
            long total = 0;
            try
            {
                foreach (var file in Directory.GetFiles(_cacheRoot))
                    total += new FileInfo(file).Length;
            }
            catch { /* cache unreadable - treat as empty */ }
            return total;
        }
    }
}
