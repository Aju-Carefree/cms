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
    public partial class LayerFileController
    {
        [RequestSizeLimit(long.MaxValue)]
        [HttpPost, Route(RouteUpload)]
        public async Task<ActionResult<UploadResult>> Upload([FromQuery] SiteRequest request, [FromForm] IFormFile file)
        {
            var site = await _siteRepository.GetAsync(request.SiteId);

            if (file == null)
            {
                return this.Error(Constants.ErrorUpload);
            }

            var fileName = Path.GetFileName(file.FileName);

            if (!_pathManager.IsFileExtensionAllowed(site, PathUtils.GetExtension(fileName)))
            {
                return this.Error(Constants.ErrorFileExtensionAllowed);
            }
            if (!_pathManager.IsFileSizeAllowed(site, file.Length))
            {
                return this.Error(Constants.ErrorFileSizeAllowed);
            }

            var localDirectoryPath = await _pathManager.GetUploadDirectoryPathAsync(site, UploadType.File);
            var filePath = PathUtils.Combine(localDirectoryPath, _pathManager.GetUploadFileName(site, fileName));

            await _pathManager.UploadAsync(file, filePath);

            var fileUrl = await _pathManager.GetSiteUrlByPhysicalPathAsync(site, filePath, true);
            var isAutoSync = await _storageManager.IsAutoSyncAsync(request.SiteId, SyncType.Files);
            if (isAutoSync)
            {
                var (success, url) = await _storageManager.SyncAsync(request.SiteId, filePath);
                if (success)
                {
                    fileUrl = url;
                }
            }

            return new UploadResult
            {
                Name = fileName,
                Path = filePath,
                Url = fileUrl
            };
        }
    }
}
