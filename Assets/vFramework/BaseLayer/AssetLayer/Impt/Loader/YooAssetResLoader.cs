using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace vFramework.BaseLayer.AssetLayer
{
    /// <summary>
    /// YooAsset 加载，location 为 Collector 配置地址；可选前缀 yoo://。
    /// 需先完成 YooAssets.Initialize 与 Package 初始化。
    /// </summary>
    public sealed class YooAssetResLoader : IResLoader
    {
        public const string Prefix = "yoo://";

        public bool CanLoad(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return false;
            }

            try
            {
                var normalized = NormalizeLocation(location);
                return !string.IsNullOrEmpty(normalized) && YooAssets.CheckLocationValid(normalized);
            }
            catch
            {
                return false;
            }
        }

        public async Task<ILoaderHandle> LoadAsync<T>(string location, CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            var normalized = NormalizeLocation(location);
            if (!YooAssets.CheckLocationValid(normalized))
            {
                throw new InvalidOperationException($"YooAsset location invalid: {normalized}");
            }

            var handle = YooAssets.LoadAssetAsync<T>(normalized);
            await handle.Task;

            if (handle.Status != EOperationStatus.Succeed || handle.AssetObject == null)
            {
                handle.Release();
                throw new InvalidOperationException(
                    $"YooAsset load failed: {normalized}, error: {handle.LastError}");
            }

            return new YooAssetLoaderHandle(handle);
        }

        public static string NormalizeLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return string.Empty;
            }

            return location.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                ? location.Substring(Prefix.Length)
                : location;
        }

        private sealed class YooAssetLoaderHandle : ILoaderHandle
        {
            private AssetOperationHandle _handle;

            public UnityEngine.Object Asset => _handle?.AssetObject;

            public YooAssetLoaderHandle(AssetOperationHandle handle)
            {
                _handle = handle;
            }

            public void ReleaseBackend()
            {
                _handle?.Release();
                _handle = null;
            }
        }
    }
}
