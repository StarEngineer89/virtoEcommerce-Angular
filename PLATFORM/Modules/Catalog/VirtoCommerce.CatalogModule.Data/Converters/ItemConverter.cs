﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using foundation = VirtoCommerce.CatalogModule.Data.Model;
using module = VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Platform.Data.Common;
using Omu.ValueInjecter;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.CatalogModule.Data.Converters
{
	public static class ItemConverter
	{
		/// <summary>
		/// Converting to model type
		/// </summary>
		/// <returns></returns>
		public static module.CatalogProduct ToModuleModel(this foundation.Item dbItem, module.Catalog catalog,
														  module.Category category, module.Property[] properties,
														  foundation.Item[] variations,
														  foundation.SeoUrlKeyword[] seoInfos,
														  string mainProductId,
														  module.CatalogProduct[] associatedProducts)
		{
			var retVal = new module.CatalogProduct();
			retVal.InjectFrom(dbItem);
			retVal.Catalog = catalog;
			retVal.CatalogId = catalog.Id;
		
			if (category != null)
			{
				retVal.Category = category;
				retVal.CategoryId = category.Id;
			}

			retVal.MainProductId = mainProductId;

			#region Links
			retVal.Links = dbItem.CategoryItemRelations.Select(x => x.ToModuleModel()).ToList();
			#endregion

			#region Variations
			if (variations != null)
			{
				retVal.Variations = new List<module.CatalogProduct>();
				foreach (var variation in variations)
				{
					var productVaraition = variation.ToModuleModel(catalog, category, properties,
																   variations: null,
																   seoInfos: null,
																   mainProductId: retVal.Id, associatedProducts: null);
					productVaraition.MainProduct = retVal;
					productVaraition.MainProductId = retVal.Id;
					retVal.Variations.Add(productVaraition);
				}
			}
			#endregion

			#region Assets
			if (dbItem.ItemAssets != null)
			{
				retVal.Assets = dbItem.ItemAssets.OrderBy(x => x.SortOrder).Select(x => x.ToModuleModel()).ToList();
			}
			#endregion

			#region Property values
			if (dbItem.ItemPropertyValues != null)
			{
				retVal.PropertyValues = dbItem.ItemPropertyValues.Select(x => x.ToModuleModel(properties)).ToList();
			}
			#endregion

			#region SeoInfo
			if (seoInfos != null)
			{
				retVal.SeoInfos = seoInfos.Select(x => x.ToModuleModel()).ToList();
			}
			#endregion

			#region EditorialReviews
			if (dbItem.EditorialReviews != null)
			{
				retVal.Reviews = dbItem.EditorialReviews.Select(x => x.ToModuleModel()).ToList();
			}
			#endregion

			#region Associations
			if (dbItem.AssociationGroups != null && associatedProducts != null)
			{
				retVal.Associations = new List<module.ProductAssociation>();
				foreach (var association in dbItem.AssociationGroups.SelectMany(x => x.Associations))
				{
					var associatedProduct = associatedProducts.FirstOrDefault(x => x.Id == association.ItemId);
					if (associatedProduct != null)
					{
						var productAssociation = association.ToModuleModel(associatedProduct);
						retVal.Associations.Add(productAssociation);
					}
				}
			}
			#endregion
			return retVal;
		}

		/// <summary>
		/// Converting to foundation type
		/// </summary>
		/// <param name="catalog"></param>
		/// <returns></returns>
		public static foundation.Item ToFoundation(this module.CatalogProduct product)
		{
			var retVal = new foundation.Product();
			retVal.InjectFrom(product);
			//Constant fields
			//Only for main product
			retVal.AvailabilityRule = (int)module.AvailabilityRule.Always;
			retVal.MinQuantity = 1;
			retVal.MaxQuantity = 0;
			//If it variation need make active false (workaround)
			// retVal.IsActive = product.MainProductId == null;
  
			//Changed fields
			retVal.CatalogId = product.CatalogId;

			#region ItemPropertyValues
			retVal.ItemPropertyValues = new NullCollection<foundation.ItemPropertyValue>();
			if (product.PropertyValues != null)
			{
				retVal.ItemPropertyValues = new ObservableCollection<foundation.ItemPropertyValue>();
				foreach (var propValue in product.PropertyValues)
				{
					var dbPropValue = propValue.ToFoundation<foundation.ItemPropertyValue>() as foundation.ItemPropertyValue;
					retVal.ItemPropertyValues.Add(dbPropValue);
				}
			}
			#endregion

			#region ItemAssets
			retVal.ItemAssets = new NullCollection<foundation.ItemAsset>();
			if (product.Assets != null)
			{
				var assets = product.Assets.ToArray();
				retVal.ItemAssets = new ObservableCollection<foundation.ItemAsset>();
				for (int order = 0; order < assets.Length; order++)
				{
					var asset = assets[order];
					var dbAsset = asset.ToFoundation();
					dbAsset.SortOrder = order;
					retVal.ItemAssets.Add(dbAsset);
				}
			}
			#endregion

			#region CategoryItemRelations
			retVal.CategoryItemRelations = new NullCollection<foundation.CategoryItemRelation>();
			if (product.Links != null)
			{
				retVal.CategoryItemRelations = new ObservableCollection<foundation.CategoryItemRelation>();
				retVal.CategoryItemRelations.AddRange(product.Links.Select(x => x.ToFoundation(product)));
			}
			#endregion

			#region EditorialReview
			retVal.EditorialReviews = new NullCollection<foundation.EditorialReview>();
			if (product.Reviews != null)
			{
				retVal.EditorialReviews = new ObservableCollection<foundation.EditorialReview>();
				retVal.EditorialReviews.AddRange(product.Reviews.Select(x => x.ToFoundation(product)));
			}
			#endregion

			#region Associations
			retVal.AssociationGroups = new NullCollection<foundation.AssociationGroup>();
			if (product.Associations != null)
			{
				retVal.AssociationGroups = new ObservableCollection<foundation.AssociationGroup>();
				var associations = product.Associations.ToArray();
				for (int order = 0; order < associations.Count(); order++)
				{
					var association = associations[order];
					var associationGroup = retVal.AssociationGroups.FirstOrDefault(x => x.Name == association.Name);
					if (associationGroup == null)
					{
						associationGroup = new foundation.AssociationGroup
						{
							Name = association.Name,
							Description = association.Description,
							Priority = 1,
						};
						retVal.AssociationGroups.Add(associationGroup);
					}
					var foundationAssociation = association.ToFoundation();
					foundationAssociation.Priority = order;
					associationGroup.Associations.Add(foundationAssociation);
				}
			}
			#endregion

			return retVal;
		}


		/// <summary>
		/// Patch changes
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		public static void Patch(this foundation.Item source, foundation.Item target)
		{
			if (target == null)
				throw new ArgumentNullException("target");

			var patchInjectionPolicy = new PatchInjection<foundation.Item>(x => x.Name, x => x.Code, x => x.IsBuyable, x=> x.IsActive, x=>x.TrackInventory);
			target.InjectFrom(patchInjectionPolicy, source);

			#region ItemAssets
			if (!source.ItemAssets.IsNullCollection())
			{
				source.ItemAssets.Patch(target.ItemAssets, (sourceAsset, targetAsset) => sourceAsset.Patch(targetAsset));
			}
			#endregion

			#region ItemPropertyValues
			if (!source.ItemPropertyValues.IsNullCollection())
			{
				source.ItemPropertyValues.Patch(target.ItemPropertyValues, (sourcePropValue, targetPropValue) => sourcePropValue.Patch(targetPropValue));
			}

			#endregion

			#region CategoryItemRelations
			if (!source.CategoryItemRelations.IsNullCollection())
			{
				source.CategoryItemRelations.Patch(target.CategoryItemRelations, new CategoryItemRelationComparer(),
										 (sourcePropValue, targetPropValue) => sourcePropValue.Patch(targetPropValue));
			}
			#endregion


			#region EditorialReviews
			if (!source.EditorialReviews.IsNullCollection())
			{
				source.EditorialReviews.Patch(target.EditorialReviews, (sourcePropValue, targetPropValue) => sourcePropValue.Patch(targetPropValue));
			}
			#endregion

			#region Association
			if (!source.AssociationGroups.IsNullCollection())
			{
				var associationComparer = AnonymousComparer.Create((foundation.AssociationGroup x) => x.Name);
				source.AssociationGroups.Patch(target.AssociationGroups, associationComparer,
										 (sourceGroup, targetGroup) => sourceGroup.Patch(targetGroup));
			}
			#endregion
		}

	}
}
