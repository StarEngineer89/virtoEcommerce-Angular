﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Data.Security.Identity;
using VirtoCommerce.Platform.Web.Model.Security;

namespace VirtoCommerce.Platform.Web.Controllers.Api
{
    /// <summary>
    /// </summary>
    [RoutePrefix("api/platform/security")]
    public class SecurityController : ApiController
    {
        private readonly Func<IAuthenticationManager> _authenticationManagerFactory;
        private readonly Func<ApplicationSignInManager> _signInManagerFactory;
        private readonly IPermissionService _permissionService;
        private readonly IRoleManagementService _roleService;
        private readonly ISecurityService _securityService;

        /// <summary>
        /// </summary>
        public SecurityController(Func<ApplicationSignInManager> signInManagerFactory, Func<IAuthenticationManager> authManagerFactory,
            IPermissionService permissionService, IRoleManagementService roleService, ISecurityService securityService)
        {
            _signInManagerFactory = signInManagerFactory;
            _authenticationManagerFactory = authManagerFactory;
            _permissionService = permissionService;
            _roleService = roleService;
            _securityService = securityService;
        }

        /// <summary>
        /// Sign in with user name and password
        /// </summary>
        /// <remarks>
        /// Verifies provided credentials and if succeeded returns full user details, otherwise returns 401 Unauthorized.
        /// </remarks>
        /// <param name="model">User credentials.</param>
        [HttpPost]
        [Route("login")]
        [ResponseType(typeof(ApplicationUserExtended))]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Login(UserLogin model)
        {
            if (await _signInManagerFactory().PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, true) == SignInStatus.Success)
            {
                return Ok(await _securityService.FindByNameAsync(model.UserName, UserDetails.Full));
            }

            return StatusCode(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Sign out
        /// </summary>
        [HttpPost]
        [Route("logout")]
        public IHttpActionResult Logout()
        {
            _authenticationManagerFactory().SignOut();
            return Ok(new { status = true });
        }

        /// <summary>
        /// Get current user details
        /// </summary>
        [HttpGet]
        [Route("currentuser")]
        [ResponseType(typeof(ApplicationUserExtended))]
        public async Task<IHttpActionResult> GetCurrentUser()
        {
            return Ok(await _securityService.FindByNameAsync(User.Identity.Name, UserDetails.Full));
        }

        /// <summary>
        /// Get all registered permissions
        /// </summary>
        [HttpGet]
        [Route("permissions")]
        [ResponseType(typeof(Permission[]))]
        [CheckPermission(Permission = PredefinedPermissions.SecurityQuery)]
        public IHttpActionResult GetPermissions()
        {
            var result = _permissionService.GetAllPermissions()
                .OrderBy(p => p.GroupName)
                .ThenBy(p => p.Name)
                .ToArray();

            return Ok(result);
        }

        /// <summary>
        /// Search roles by keyword
        /// </summary>
        /// <param name="request">Search parameters.</param>
        [HttpGet]
        [Route("roles")]
        [ResponseType(typeof(RoleSearchResponse))]
        [CheckPermission(Permission = PredefinedPermissions.SecurityQuery)]
        public IHttpActionResult SearchRoles([FromUri]RoleSearchRequest request)
        {
            var result = _roleService.SearchRoles(request);
            return Ok(result);
        }

        /// <summary>
        /// Get role by ID
        /// </summary>
        /// <param name="roleId"></param>
        [HttpGet]
        [Route("roles/{roleId}")]
        [ResponseType(typeof(Role))]
        [CheckPermission(Permission = PredefinedPermissions.SecurityQuery)]
        public IHttpActionResult GetRole(string roleId)
        {
            var result = _roleService.GetRole(roleId);
            return Ok(result);
        }

        /// <summary>
        /// Delete roles by ID
        /// </summary>
        /// <param name="roleIds">An array of role IDs.</param>
        [HttpDelete]
        [Route("roles")]
        [CheckPermission(Permission = PredefinedPermissions.SecurityManage)]
        public IHttpActionResult DeleteRoles([FromUri(Name = "ids")] string[] roleIds)
        {
            if (roleIds != null)
            {
                foreach (var roleId in roleIds)
                {
                    _roleService.DeleteRole(roleId);
                }
            }

            return Ok();
        }

        /// <summary>
        /// Add a new role or update an existing role
        /// </summary>
        /// <param name="role"></param>
        [HttpPut]
        [Route("roles")]
        [ResponseType(typeof(Role))]
        [CheckPermission(Permission = PredefinedPermissions.SecurityManage)]
        public IHttpActionResult UpdateRole(Role role)
        {
            var result = _roleService.AddOrUpdateRole(role);
            return Ok(result);
        }

        /// <summary>
        /// Generate new API key
        /// </summary>
        /// <remarks>
        /// Generates new key but does not save it.
        /// </remarks>
        /// <param name="type"></param>
        [HttpGet]
        [ResponseType(typeof(ApiAccount))]
        [Route("apiaccounts/new")]
        [CheckPermission(Permission = PredefinedPermissions.SecurityManage)]
        public IHttpActionResult GenerateNewApiAccount(ApiAccountType type)
        {
            var result = _securityService.GenerateNewApiAccount(type);
            result.IsActive = null;
            return Ok(result);
        }

        /// <summary>
        /// Search users by keyword
        /// </summary>
        /// <param name="request">Search parameters.</param>
        [HttpGet]
        [ResponseType(typeof(UserSearchResponse))]
        [Route("users")]
        [CheckPermission(Permission = PredefinedPermissions.SecurityQuery)]
        public async Task<IHttpActionResult> SearchUsersAsync([FromUri] UserSearchRequest request)
        {
            var result = await _securityService.SearchUsersAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Get user details by user name
        /// </summary>
        /// <param name="userName"></param>
        [HttpGet]
        [ResponseType(typeof(ApplicationUserExtended))]
        [Route("users/{userName}")]
        [CheckPermission(Permission = PredefinedPermissions.SecurityQuery)]
        public async Task<IHttpActionResult> GetUserByName(string userName)
        {
            var retVal = await _securityService.FindByNameAsync(userName, UserDetails.Full);
            return Ok(retVal);
        }

        /// <summary>
        /// Create new user
        /// </summary>
        /// <param name="user">User details.</param>
        [HttpPost]
        [Route("users/create")]
        [CheckPermission(Permission = PredefinedPermissions.SecurityManage)]
        public async Task<IHttpActionResult> CreateAsync(ApplicationUserExtended user)
        {
            var result = await _securityService.CreateAsync(user);
            return ProcessSecurityResult(result);
        }

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="changePassword">Old and new passwords.</param>
        [HttpPost]
        [ResponseType(typeof(IdentityResult))]
        [Route("users/{userName}/changepassword")]
        [CheckPermission(Permission = PredefinedPermissions.SecurityQuery)]
        public async Task<IHttpActionResult> ChangePassword(string userName, [FromBody] ChangePasswordInfo changePassword)
        {
            var result = await _securityService.ChangePasswordAsync(userName, changePassword.OldPassword, changePassword.NewPassword);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        /// <summary>
        /// Reset password
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="resetPassword">New password.</param>
        [HttpPost]
        [ResponseType(typeof(IdentityResult))]
        [Route("users/{userName}/resetpassword")]
        [CheckPermission(Permission = PredefinedPermissions.SecurityManage)]
        public async Task<IHttpActionResult> ResetPassword(string userName, [FromBody] ResetPasswordInfo resetPassword)
        {
            var result = await _securityService.ResetPasswordAsync(userName, resetPassword.NewPassword);
            return ProcessSecurityResult(result);
        }

        /// <summary>
        /// Update user details by user ID
        /// </summary>
        /// <param name="user">User details.</param>
        [HttpPut]
        [Route("users")]
        [CheckPermission(Permission = PredefinedPermissions.SecurityManage)]
        public async Task<IHttpActionResult> UpdateAsync(ApplicationUserExtended user)
        {
            var result = await _securityService.UpdateAsync(user);
            return ProcessSecurityResult(result);
        }

        /// <summary>
        /// Delete users by name
        /// </summary>
        /// <param name="names">An array of user names.</param>
        [HttpDelete]
        [Route("users")]
        [CheckPermission(Permission = PredefinedPermissions.SecurityManage)]
        public async Task<IHttpActionResult> DeleteAsync([FromUri] string[] names)
        {
            await _securityService.DeleteAsync(names);
            return Ok();
        }


        private IHttpActionResult ProcessSecurityResult(SecurityResult securityResult)
        {
            IHttpActionResult result;

            if (securityResult == null)
            {
                result = BadRequest();
            }
            else
            {
                if (!securityResult.Succeeded)
                    result = BadRequest(securityResult.Errors != null ? string.Join(" ", securityResult.Errors) : "Unknown error.");
                else
                    result = Ok();
            }

            return result;
        }
    }
}
