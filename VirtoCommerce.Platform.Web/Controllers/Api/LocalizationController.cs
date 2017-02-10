﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Description;
using Newtonsoft.Json.Linq;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using WebGrease.Extensions;

namespace VirtoCommerce.Platform.Web.Controllers.Api
{
    [RoutePrefix("")]
    public class LocalizationController : ApiController
    {
        private const string LanguageSetting = "VirtoCommerce.Core.General.Language";

        private readonly IModuleCatalog _moduleCatalog;
        private readonly ISecurityService _securityService;

        public LocalizationController(IModuleCatalog moduleCatalog, ISecurityService securityService)
        {
            _moduleCatalog = moduleCatalog;
            _securityService = securityService;
        }

        /// <summary>
        /// Return localization resource
        /// </summary>
        /// <param name="lang">Language of localization resource (en by default)</param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/platform/localization")]
        [ResponseType(typeof(object))] // Produces invalid response type in generated client
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public JObject GetLocalization(string lang = "en")
        {
            var searchPattern = string.Format("{0}.*.json", lang);
            var files = GetAllLocalizationFiles(searchPattern);

            var result = new JObject();
            foreach (var file in files)
            {
                var part = JObject.Parse(File.ReadAllText(file));
                result.Merge(part, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Merge });
            }
            return result;
        }

        /// <summary>
        /// Return all available locales
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/platform/localization/locales")]
        [ResponseType(typeof(string[]))]
        public IHttpActionResult GetLocales()
        {
            var files = GetAllLocalizationFiles("*.json");
            var locales = files
                .Select(Path.GetFileName)
                .Select(x => x.Substring(0, x.IndexOf('.'))).Distinct().ToArray();

            return Ok(locales);
        }

        [HttpGet]
        [Route("api/platform/security/users/{id}/locale")]
        [ResponseType(typeof(string))]
        public async Task<IHttpActionResult> GetLocale(string id)
        {
            var user = await _securityService.FindByIdAsync(id, UserDetails.Undefined);
            var locale = user.Settings.GetSettingValue(LanguageSetting, string.Empty);
            return Ok(locale);
        }

        [HttpPut]
        [Route("api/platform/security/users/{id}/locale")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> SetLocale(string id)
        {
            var user = await _securityService.FindByIdAsync(id, UserDetails.Undefined);
            user.Settings.SetSettingValue(LanguageSetting, await Request.Content.ReadAsStringAsync());
            await _securityService.UpdateAsync(user);
            return StatusCode(HttpStatusCode.NoContent);
        }

        private void CreateUserLocaleSetting()
        {
            
        }

        private string[] GetAllLocalizationFiles(string searchPattern)
        {
            var files = new List<string>();

            // Get platform localization files
            var platformPath = HostingEnvironment.MapPath(Startup.VirtualRoot).EnsureEndSeparator();
            var platformFileNames = GetLocalizationFilesByPath(platformPath, searchPattern);
            files.AddRange(platformFileNames);

            // Get modules localization files
            foreach (var module in _moduleCatalog.Modules.OfType<ManifestModuleInfo>())
            {
                  var moduleFileNames = GetLocalizationFilesByPath(module.FullPhysicalPath, searchPattern);
                files.AddRange(moduleFileNames);
            }

            // Get user defined localization files from App_Data/Localizations folder
            var userLocalizationPath = HostingEnvironment.MapPath(Startup.VirtualRoot + "/App_Data").EnsureEndSeparator();
            var userFileNames = GetLocalizationFilesByPath(userLocalizationPath, searchPattern);
            files.AddRange(userFileNames);
            return files.ToArray();
        }

        private string[] GetLocalizationFilesByPath(string path, string searchPattern, string localizationSubfolder = "Localizations")
        {
            var sourceDirectoryPath = Path.Combine(path, localizationSubfolder).EnsureEndSeparator();

            return Directory.Exists(sourceDirectoryPath)
                ? Directory.EnumerateFiles(sourceDirectoryPath, searchPattern, SearchOption.AllDirectories).ToArray()
                : new string[0];
        }
    }
}
