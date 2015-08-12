﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Data.Asset;
using VirtoCommerce.Platform.Core.Packaging;
using VirtoCommerce.Platform.Web.Converters.Packaging;
using webModel = VirtoCommerce.Platform.Web.Model.Packaging;

namespace VirtoCommerce.Platform.Web.Controllers.Api
{
    [RoutePrefix("api/platform/modules")]
    [CheckPermission(Permission = PredefinedPermissions.ModuleQuery)]
    public class ModulesController : ApiController
    {
        private readonly string _packagesPath;
        private readonly IPackageService _packageService;
        private static readonly ConcurrentQueue<webModel.ModuleWorkerJob> _scheduledJobs = new ConcurrentQueue<webModel.ModuleWorkerJob>();
        private static readonly ConcurrentBag<webModel.ModuleWorkerJob> _jobList = new ConcurrentBag<webModel.ModuleWorkerJob>();
        private static Task _runningTask;
        private static readonly object _lockObject = new object();

        public ModulesController(IPackageService packageService, string packagesPath)
        {
            _packageService = packageService;
            _packagesPath = packagesPath;
        }

        /// <summary>
        /// Get installed modules
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(webModel.ModuleDescriptor[]))]
        public IHttpActionResult GetModules()
        {
            var retVal = _packageService.GetModules().Select(x => x.ToWebModel()).ToArray();
            return Ok(retVal);
        }

        /// <summary>
        /// Get module details
        /// </summary>
        /// <param name="id">Module ID.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        [ResponseType(typeof(webModel.ModuleDescriptor))]
        public IHttpActionResult GetModuleById(string id)
        {
            var retVal = _packageService.GetModules().FirstOrDefault(x => x.Id == id);
            if (retVal != null)
            {
                return Ok(retVal.ToWebModel());
            }
            return NotFound();
        }

        /// <summary>
        /// Upload module package for installation or update
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("")]
        [ResponseType(typeof(webModel.ModuleDescriptor))]
        [CheckPermission(Permission = PredefinedPermissions.ModuleManage)]
        public async Task<IHttpActionResult> Upload()
        {
            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            if (!Directory.Exists(_packagesPath))
            {
                Directory.CreateDirectory(_packagesPath);
            }

            var streamProvider = new CustomMultipartFormDataStreamProvider(_packagesPath);
            await Request.Content.ReadAsMultipartAsync(streamProvider);

            var file = streamProvider.FileData.FirstOrDefault();
            if (file != null)
            {
                var descriptor = _packageService.OpenPackage(Path.Combine(_packagesPath, file.LocalFileName));
                if (descriptor != null)
                {
                    var retVal = descriptor.ToWebModel();
                    retVal.FileName = file.LocalFileName;

                    var dependencyErrors = _packageService.GetDependencyErrors(descriptor);
                    retVal.ValidationErrors.AddRange(dependencyErrors);

                    return Ok(retVal);
                }
            }

            return NotFound();
        }

        /// <summary>
        /// Install module from uploaded file
        /// </summary>
        /// <param name="fileName">Module package file name.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("install")]
        [ResponseType(typeof(webModel.ModuleWorkerJob))]
        [CheckPermission(Permission = PredefinedPermissions.ModuleManage)]
        public IHttpActionResult InstallModule(string fileName)
        {
            var package = _packageService.OpenPackage(Path.Combine(_packagesPath, fileName));

            if (package != null)
            {
                var result = ScheduleJob(package.ToWebModel(), webModel.ModuleAction.Install);
                return Ok(result);
            }

            return InternalServerError();
        }

        /// <summary>
        /// Update module from uploaded file
        /// </summary>
        /// <param name="id">Module ID.</param>
        /// <param name="fileName">Module package file name.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}/update")]
        [ResponseType(typeof(webModel.ModuleWorkerJob))]
        [CheckPermission(Permission = PredefinedPermissions.ModuleManage)]
        public IHttpActionResult UpdateModule(string id, string fileName)
        {
            var module = _packageService.GetModules().FirstOrDefault(m => m.Id == id);

            if (module != null)
            {
                var package = _packageService.OpenPackage(Path.Combine(_packagesPath, fileName));

                if (package != null && package.Id == module.Id)
                {
                    var result = ScheduleJob(package.ToWebModel(), webModel.ModuleAction.Update);
                    return Ok(result);
                }
            }

            return InternalServerError();
        }

        /// <summary>
        /// Uninstall module
        /// </summary>
        /// <param name="id">Module ID.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}/uninstall")]
        [ResponseType(typeof(webModel.ModuleWorkerJob))]
        [CheckPermission(Permission = PredefinedPermissions.ModuleManage)]
        public IHttpActionResult UninstallModule(string id)
        {
            var module = _packageService.GetModules().FirstOrDefault(m => m.Id == id);
            if (module != null)
            {
                var result = ScheduleJob(module.ToWebModel(), webModel.ModuleAction.Uninstall);
                return Ok(result);
            }
            return InternalServerError();
        }

        /// <summary>
        /// Get installation or update details
        /// </summary>
        /// <param name="id">Job ID.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("jobs/{id}")]
        [ResponseType(typeof(webModel.ModuleWorkerJob))]
        public IHttpActionResult GetJob(string id)
        {
            var job = _jobList.FirstOrDefault(x => x.Id == id);
            if (job != null)
            {
                return Ok(job);
            }
            return NotFound();
        }

        /// <summary>
        /// Restart web application
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("restart")]
        [ResponseType(typeof(void))]
        [CheckPermission(Permission = PredefinedPermissions.ModuleManage)]
        public IHttpActionResult Restart()
        {
            HttpRuntime.UnloadAppDomain();
            return StatusCode(HttpStatusCode.NoContent);
        }


        private webModel.ModuleWorkerJob ScheduleJob(webModel.ModuleDescriptor descriptor, webModel.ModuleAction action)
        {
            var retVal = new webModel.ModuleWorkerJob(_packageService, descriptor, action);

            _scheduledJobs.Enqueue(retVal);

            if (_runningTask == null || _runningTask.IsCompleted)
            {
                lock (_lockObject)
                {
                    if (_runningTask == null || _runningTask.IsCompleted)
                    {
                        _runningTask = Task.Run(() => { DoWork(); }, retVal.CancellationToken);
                    }
                }
            }

            return retVal;
        }

        private static void DoWork()
        {
            while (_scheduledJobs.Any())
            {
                webModel.ModuleWorkerJob job;

                if (_scheduledJobs.TryDequeue(out job))
                {
                    try
                    {
                        _jobList.Add(job);
                        job.Started = DateTime.UtcNow;
                        var reportProgress = new Progress<ProgressMessage>(m => { job.ProgressLog.Add(m.ToWebModel()); });

                        if (job.Action == webModel.ModuleAction.Install)
                        {
                            job.PackageService.Install(job.ModuleDescriptor.Id, job.ModuleDescriptor.Version, reportProgress);
                        }
                        else if (job.Action == webModel.ModuleAction.Update)
                        {
                            job.PackageService.Update(job.ModuleDescriptor.Id, job.ModuleDescriptor.Version, reportProgress);
                        }
                        else if (job.Action == webModel.ModuleAction.Uninstall)
                        {
                            job.PackageService.Uninstall(job.ModuleDescriptor.Id, reportProgress);
                        }
                    }
                    catch (Exception ex)
                    {
                        job.ProgressLog.Add(new webModel.ProgressMessage { Message = ex.ToString(), Level = ProgressMessageLevel.Error.ToString() });
                    }

                    job.Completed = DateTime.UtcNow;
                }
            }
        }
    }
}
