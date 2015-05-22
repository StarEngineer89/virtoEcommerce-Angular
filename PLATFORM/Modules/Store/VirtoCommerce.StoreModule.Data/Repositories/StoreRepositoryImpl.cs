﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;
using VirtoCommerce.StoreModule.Data.Model;

namespace VirtoCommerce.StoreModule.Data.Repositories
{
	public class StoreRepositoryImpl : EFRepositoryBase, IStoreRepository
	{
		public StoreRepositoryImpl()
		{
		}

		public StoreRepositoryImpl(string nameOrConnectionString)
			: this(nameOrConnectionString, null)
		{
		}
		public StoreRepositoryImpl(string nameOrConnectionString, params IInterceptor[] interceptors)
			: base(nameOrConnectionString, null, interceptors)
		{

		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			MapEntity<Store>(modelBuilder, toTable: "Store");
			MapEntity<StoreCurrency>(modelBuilder, toTable: "StoreCurrency");
			MapEntity<StoreLanguage>(modelBuilder, toTable: "StoreLanguage");
			MapEntity<StorePaymentGateway>(modelBuilder, toTable: "StorePaymentGateway");
			MapEntity<StoreShippingMethod>(modelBuilder, toTable: "StoreShippingMethod");
		

			base.OnModelCreating(modelBuilder);
		}

		#region IStoreRepository Members

		public Store GetStoreById(string id)
		{
			var retVal = Stores.Where(x => x.Id == id).Include(x => x.Languages)
														 .Include(x => x.Currencies)
														 .Include(x => x.PaymentGateways)
														 .Include(x => x.ShippingMethods);
			return retVal.FirstOrDefault();
		}

		public IQueryable<Store> Stores
		{
			get { return GetAsQueryable<Store>(); }
		}

		#endregion

		
	}

}
