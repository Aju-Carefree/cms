﻿using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SSCMS.Configuration;
using SSCMS.Dto;
using SSCMS.Enums;
using SSCMS.Utils;

namespace SSCMS.Web.Controllers.Admin.Common.Editor
{
    public partial class LayerVideoController
    {
        [RequestSizeLimit(long.MaxValue)]
        [HttpPost, Route(RouteUploadVideo)]
        public async Task<ActionResult<UploadResult>> UploadVideo([FromQuery] SiteRequest request, [FromForm] IFormFile file)
        {
            var site = await _siteRepository.GetAsync(request.SiteId);

            if (file == null)
            {
                return this.Error(Constants.ErrorUpload);
            }

            var fileName = Path.GetFileName(file.FileName);

            if (!_pathManager.IsVideoExtensionAllowed(site, PathUtils.GetExtension(fileName)))
            {
                return this.Error(Constants.ErrorVideoExtensionAllowed);
            }
            if (!_pathManager.IsVideoSizeAllowed(site, file.Length))
            {
                return this.Error(Constants.ErrorVideoSizeAllowed);
            }

            var localDirectoryPath = await _pathManager.GetUploadDirectoryPathAsync(site, UploadType.Video);
            var filePath = PathUtils.Combine(localDirectoryPath, _pathManager.GetUploadFileName(site, fileName));

            await _pathManager.UploadAsync(file, filePath);

            var videoUrl = await _pathManager.GetSiteUrlByPhysicalPathAsync(site, filePath, true);
            var coverUrl = string.Empty;

            var isVod = await _vodManager.IsEnabledAsync(request.SiteId);
            if (isVod)
            {
                var vodPlay = await _vodManager.UploadAsync(filePath);
                if (vodPlay.Success)
                {
                    videoUrl = vodPlay.PlayUrl;
                    coverUrl = vodPlay.CoverUrl;
                }
            }
            else
            {
                var isAutoSync = await _storageManager.IsAutoSyncAsync(request.SiteId, SyncType.Videos);
                if (isAutoSync)
                {
                    var (success, url) = await _storageManager.SyncAsync(request.SiteId, filePath);
                    if (success)
                    {
                        videoUrl = url;
                    }
                }
            }

            return new UploadResult
            {
                Name = fileName,
                Path = filePath,
                Url = videoUrl,
                CoverUrl = coverUrl
            };
        }
    }
}
