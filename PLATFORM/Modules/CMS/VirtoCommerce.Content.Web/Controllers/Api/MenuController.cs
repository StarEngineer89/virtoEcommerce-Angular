﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.Content.Data.Services;
using VirtoCommerce.Content.Web.Converters;
using VirtoCommerce.Content.Web.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.Content.Web.Controllers.Api
{
	[RoutePrefix("api/cms/{storeId}")]
    [CheckPermission(Permission = PredefinedPermissions.Query)]
	public class MenuController : ApiController
	{
		private readonly IMenuService _menuService;

		public MenuController(IMenuService menuService)
		{
			if (menuService == null)
				throw new ArgumentNullException("menuService");

			_menuService = menuService;
		}

		/// <summary>
		/// Get menu link lists
		/// </summary>
		/// <remarks>Get all menu link lists by store. Returns array of store menu link lists</remarks>
        /// <param name="storeId">Store Id</param>
        /// <response code="500">Internal Server Error</response>
        /// <response code="200">Menu link lists returned OK</response>
		[HttpGet]
		[ResponseType(typeof(IEnumerable<MenuLinkList>))]
		[ClientCache(Duration = 30)]
		[Route("menu")]
		public IHttpActionResult GetLists(string storeId)
		{
		    var lists = _menuService.GetListsByStoreId(storeId);
		    if (lists.Any())
		    {
		        return this.Ok(lists.Select(s => s.ToWebModel()));
		    }
			return Ok();
		}

		/// <summary>
		/// Get menu link list by id
		/// </summary>
		/// <remarks>Get menu link list by list id. Returns menu link list</remarks>
		/// <param name="listId">List Id</param>
		/// <response code="500">Internal Server Error</response>
		/// <response code="200">Menu link list returned OK</response>
		[HttpGet]
		[ResponseType(typeof(MenuLinkList))]
		[Route("menu/{listId}")]
		public IHttpActionResult GetList(string listId)
		{
			var item = _menuService.GetListById(listId).ToWebModel();
			return Ok(item);
		}

		/// <summary>
		/// Checking name of menu link list
		/// </summary>
		/// <remarks>Checking pair of name+language of menu link list for unique. Return result of checking</remarks>
		/// <param name="storeId">Store Id</param>
		/// <param name="name">Name of menu link list</param>
		/// <param name="language">Language of menu link list</param>
		/// <param name="id">Menu link list id</param>
		/// <response code="500">Internal Server Error</response>
		/// <response code="200">Checking name returns OK</response>
		[HttpGet]
		[ResponseType(typeof(CheckNameResult))]
		[Route("menu/checkname")]
		public IHttpActionResult CheckName(string storeId, string name, string language, string id)
		{
			var retVal = _menuService.CheckList(storeId, name, language, id);
			var response = new CheckNameResult { Result = retVal };
			return Ok(response);
		}

		[HttpPost]
		[ResponseType(typeof(void))]
		[Route("menu")]
        [CheckPermission(Permission = PredefinedPermissions.Manage)]
		public IHttpActionResult Update(string storeId, MenuLinkList list)
		{
			_menuService.Update(list.ToCoreModel());
			return Ok();
		}

		[HttpDelete]
		[ResponseType(typeof(void))]
		[Route("menu")]
        [CheckPermission(Permission = PredefinedPermissions.Manage)]
		public IHttpActionResult Delete(string listId)
		{
			_menuService.DeleteList(listId);
			return Ok();
		}

	}
}
