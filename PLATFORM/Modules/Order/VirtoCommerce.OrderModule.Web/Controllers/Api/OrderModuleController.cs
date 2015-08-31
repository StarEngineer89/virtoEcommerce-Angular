﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Platform.Core.Security;
using coreModel = VirtoCommerce.Domain.Order.Model;
using webModel = VirtoCommerce.OrderModule.Web.Model;
using VirtoCommerce.OrderModule.Web.Converters;
using VirtoCommerce.Domain.Cart.Services;
using System.Web.Http.ModelBinding;
using VirtoCommerce.OrderModule.Web.Binders;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Domain.Payment.Model;
using Omu.ValueInjecter;
using VirtoCommerce.Platform.Core.Caching;
using Hangfire;
using VirtoCommerce.Domain.Common;
using VirtoCommerce.OrderModule.Web.BackgroundJobs;
using VirtoCommerce.OrderModule.Data.Repositories;

namespace VirtoCommerce.OrderModule.Web.Controllers.Api
{
    [RoutePrefix("api/order/customerOrders")]
    [CheckPermission(Permission = PredefinedPermissions.Query)]
    public class OrderModuleController : ApiController
    {
        private readonly ICustomerOrderService _customerOrderService;
        private readonly ICustomerOrderSearchService _searchService;
        private readonly IUniqueNumberGenerator _uniqueNumberGenerator;
        private readonly IStoreService _storeService;
		private readonly CacheManager _cacheManager;
		private readonly Func<IOrderRepository> _repositoryFactory;

		public OrderModuleController(ICustomerOrderService customerOrderService, ICustomerOrderSearchService searchService, IStoreService storeService, IUniqueNumberGenerator numberGenerator, CacheManager cacheManager, Func<IOrderRepository> repositoryFactory)
        {
            _customerOrderService = customerOrderService;
            _searchService = searchService;
            _uniqueNumberGenerator = numberGenerator;
            _storeService = storeService;
			_cacheManager = cacheManager;
			_repositoryFactory = repositoryFactory;
        }

		/// <summary>
		/// Search customer orders by given criteria
		/// </summary>
		/// <param name="criteria">criteria</param>
        [HttpGet]
        [ResponseType(typeof(webModel.SearchResult))]
        [Route("")]
        public IHttpActionResult Search([ModelBinder(typeof(SearchCriteriaBinder))] coreModel.SearchCriteria criteria)
        {
            var retVal = _searchService.Search(criteria);
            return Ok(retVal.ToWebModel());
        }

		/// <summary>
		/// Find customer order by id
		/// </summary>
		/// <remarks>Return a single customer order with all nested documents</remarks>
		/// <param name="id">customer order id</param>
        [HttpGet]
        [ResponseType(typeof(webModel.CustomerOrder))]
        [Route("{id}")]
        public IHttpActionResult GetById(string id)
        {
            var retVal = _customerOrderService.GetById(id, coreModel.CustomerOrderResponseGroup.Full);
            if (retVal == null)
            {
                return NotFound();
            }
            return Ok(retVal.ToWebModel());
        }

		/// <summary>
		/// Create new customer order based on shopping cart.
		/// </summary>
		/// <param name="id">shopping cart id</param>
        [HttpPost]
        [ResponseType(typeof(webModel.CustomerOrder))]
        [Route("{id}")]
		public IHttpActionResult CreateOrderFromCart(string id)
        {
			var retVal = _customerOrderService.CreateByShoppingCart(id);
            return Ok(retVal.ToWebModel());
        }

		/// <summary>
		/// Registration customer order payment in external payment system
		/// </summary>
		/// <remarks>Used in front-end checkout or manual order payment registration</remarks>
		/// <param name="bankCardInfo">banking card information</param>
		/// <param name="orderId">customer order id</param>
		/// <param name="paymentId">payment id</param>
        [HttpPost]
        [ResponseType(typeof(webModel.CustomerOrder))]
        [Route("{orderId}/processPayment/{paymentId}")]
        public IHttpActionResult ProcessOrderPayments([FromBody]BankCardInfo bankCardInfo, string orderId, string paymentId)
        {
            var order = _customerOrderService.GetById(orderId, coreModel.CustomerOrderResponseGroup.Full);
            if (order == null)
            {
                throw new NullReferenceException("order");
            }
            var payment = order.InPayments.FirstOrDefault(x => x.Id == paymentId);
            if (payment == null)
            {
                throw new NullReferenceException("payment");
            }
            var store = _storeService.GetById(order.StoreId);
            var paymentMethod = store.PaymentMethods.FirstOrDefault(x => x.Code == payment.GatewayCode);
            if (payment == null)
            {
                throw new NullReferenceException("appropriate paymentMethod not found");
            }

            var context = new ProcessPaymentEvaluationContext
            {
                Order = order,
                Payment = payment,
                Store = store,
				BankCardInfo = bankCardInfo
            };

            var result = paymentMethod.ProcessPayment(context);

            _customerOrderService.Update(new coreModel.CustomerOrder[] { order });

            var retVal = new webModel.ProcessPaymentResult();
            retVal.InjectFrom(result);
            retVal.PaymentMethodType = paymentMethod.PaymentMethodType;

            return Ok(retVal);
        }

		/// <summary>
		/// Add new customer order to system
		/// </summary>
		/// <param name="customerOrder">customer order</param>
		[HttpPost]
        [ResponseType(typeof(webModel.CustomerOrder))]
        [Route("")]
        public IHttpActionResult CreateOrder(webModel.CustomerOrder customerOrder)
        {
            var retVal = _customerOrderService.Create(customerOrder.ToCoreModel());
            return Ok(retVal.ToWebModel());
        }

		/// <summary>
		///  Update a existing customer order 
		/// </summary>
		/// <param name="customerOrder">customer order</param>
        [HttpPut]
        [ResponseType(typeof(void))]
        [Route("")]
        [CheckPermission(Permission = PredefinedPermissions.Manage)]
		public IHttpActionResult Update(webModel.CustomerOrder customerOrder)
        {
			var coreOrder = customerOrder.ToCoreModel();
            _customerOrderService.Update(new coreModel.CustomerOrder[] { coreOrder });
            return StatusCode(HttpStatusCode.NoContent);
        }

		/// <summary>
		/// Get new shipment for specified customer order
		/// </summary>
		/// <remarks>Return new shipment document with populates all required properties.</remarks>
		/// <param name="id">customer order id </param>
        [HttpGet]
        [ResponseType(typeof(webModel.Shipment))]
        [Route("{id}/shipments/new")]
        public IHttpActionResult GetNewShipment(string id)
        {
            coreModel.Shipment retVal = null;
            var order = _customerOrderService.GetById(id, coreModel.CustomerOrderResponseGroup.Full);
            if (order != null)
            {
                retVal = new coreModel.Shipment
                {
                    Currency = order.Currency
                };
                retVal.Number = _uniqueNumberGenerator.GenerateNumber("SH{0:yyMMdd}-{1:D5}");

                //Detect not whole shipped items
                //TODO: LineItem partial shipping
                var shippedLineItemIds = order.Shipments.SelectMany(x => x.Items).Select(x=>x.LineItemId);

                //TODO Add check for digital products (don't add to shipment)
				retVal.Items = order.Items.Where(x => !shippedLineItemIds.Contains(x.Id))
							  .Select(x => new coreModel.ShipmentItem(x)).ToList();
                return Ok(retVal.ToWebModel());
            }

            return NotFound();
        }

		/// <summary>
		/// Get new payment for specified customer order
		/// </summary>
		/// <remarks>Return new payment  document with populates all required properties.</remarks>
		/// <param name="id">customer order id </param>
        [HttpGet]
        [ResponseType(typeof(webModel.PaymentIn))]
        [Route("{id}/payments/new")]
        public IHttpActionResult GetNewPayment(string id)
        {
            coreModel.PaymentIn retVal = null;
            var order = _customerOrderService.GetById(id, coreModel.CustomerOrderResponseGroup.Full);
            if (order != null)
            {
                retVal = new coreModel.PaymentIn
                {
                    Id = Guid.NewGuid().ToString(),
                    Currency = order.Currency,
                    CustomerId = order.CustomerId
                };
                retVal.Number = _uniqueNumberGenerator.GenerateNumber("PI{0:yyMMdd}-{1:D5}");
                return Ok(retVal.ToWebModel());
            }

            return NotFound();
        }

		/// <summary>
		///  Delete a whole customer orders
		/// </summary>
		/// <param name="ids">customer order ids for delete</param>
        [HttpDelete]
        [ResponseType(typeof(void))]
        [Route("")]
        [CheckPermission(Permission = PredefinedPermissions.Manage)]
        public IHttpActionResult DeleteOrdersByIds([FromUri] string[] ids)
        {
            _customerOrderService.Delete(ids);
            return StatusCode(HttpStatusCode.NoContent);
        }

		/// <summary>
		///  Delete a concrete customer order operation (document) 
		/// </summary>
		/// <param name="id">customer order id</param>
		/// <param name="operationId">operation id</param>
        [HttpDelete]
        [ResponseType(typeof(void))]
        [Route("~/api/order/customerOrders/{id}/operations/{operationId}")]
        public IHttpActionResult Delete(string id, string operationId)
        {
            var order = _customerOrderService.GetById(id, coreModel.CustomerOrderResponseGroup.Full);
            if (order != null)
            {
                var operation = order.GetFlatObjectsListWithInterface<coreModel.IOperation>().FirstOrDefault(x => ((Entity)x).Id == operationId);
                if (operation != null)
                {
                    var shipment = operation as coreModel.Shipment;
                    var payment = operation as coreModel.PaymentIn;
                    if (shipment != null)
                    {
                        order.Shipments.Remove(shipment);
                    }
                    else if (payment != null)
                    {
                        //If payment not belong to order need remove payment in shipment
                        if (!order.InPayments.Remove(payment))
                        {
                            var paymentContainsShipment = order.Shipments.FirstOrDefault(x => x.InPayments.Contains(payment));
                            paymentContainsShipment.InPayments.Remove(payment);
                        }
                    }
                }
                _customerOrderService.Update(new coreModel.CustomerOrder[] { order });
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

		/// <summary>
		///  Get a some order statistic information for Commerce manager dashboard
		/// </summary>
		/// <param name="start">start interval date</param>
		/// <param name="end">end interval date</param>
		[HttpGet]
		[ResponseType(typeof(webModel.DashboardStatisticsResult))]
		[Route("~/api/order/dashboardStatistics")]
        [OverrideAuthorization]
		public IHttpActionResult GetDashboardStatistics([FromUri]DateTime? start = null, [FromUri]DateTime? end = null)
		{
			start = start ?? DateTime.UtcNow.AddYears(-1);
			end = end ?? DateTime.UtcNow;
			var cacheKey = CacheKey.Create("Statistic", start.Value.ToString("yyyy-MM-dd"), end.Value.ToString("yyyy-MM-dd"));
			var retVal = _cacheManager.Get(cacheKey, () =>
			{

				var collectStaticJob = new CollectOrderStatisticJob(_repositoryFactory, _cacheManager);
				return collectStaticJob.CollectStatistics(start.Value, end.Value);

			});
			return Ok(retVal);
		}

    }
}
