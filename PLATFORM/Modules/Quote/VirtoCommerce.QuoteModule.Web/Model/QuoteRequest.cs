﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;

namespace VirtoCommerce.QuoteModule.Web.Model
{
	public class QuoteRequest : AuditableEntity
	{
		public string Number { get; set; }
		public string StoreId { get; set; }
		public string ChannelId { get; set; }
		public bool IsAnonymous { get; set; }
		public string CustomerId { get; set; }
		public string CustomerName { get; set; }
		public string OrganizationName { get; set; }
		public string OrganizationId { get; set; }

		public string EmployeeId { get; set; }
		public string EmployeeName { get; set; }

		public DateTime? ExpirationDate { get; set; }
		public DateTime? ReminderDate { get; set; }

		public bool EnableNotification { get; set; } 
		public bool IsLocked { get; set; }
		public string Status { get; set; }

		public string Comment { get; set; }
		public string InnerComment { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public CurrencyCodes Currency { get; set; }

        public QuoteRequestTotals Totals { get; set; }
		public string Coupon { get; set; }

		public ShipmentMethod ShipmentMethod { get; set; }
		public ICollection<Address> Addresses { get; set; }
		public ICollection<QuoteItem> Items { get; set; }
		public ICollection<QuoteAttachment> Attachments { get; set; }

		public string LanguageCode { get; set; }

		public ICollection<TaxDetail> TaxDetails { get; set; }
	
		public bool IsCancelled { get; set; }
		public DateTime? CancelledDate { get; set; }
		public string CancelReason { get; set; }
	
		public string ObjectType { get; set; }
		public ICollection<DynamicObjectProperty> DynamicProperties { get; set; }
	
	}
}
