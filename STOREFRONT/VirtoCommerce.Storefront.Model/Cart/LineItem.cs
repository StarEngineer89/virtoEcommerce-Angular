﻿using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Storefront.Model.Common;

namespace VirtoCommerce.Storefront.Model.Cart
{
    public class LineItem : Entity
    {
        public LineItem()
        {
            Discounts = new List<Discount>();
            TaxDetails = new List<TaxDetail>();
        }

        /// <summary>
        /// Gets or sets the value of product id
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets the value of catalog id
        /// </summary>
        public string CatalogId { get; set; }

        /// <summary>
        /// Gets or sets the value of category id
        /// </summary>
        public string CategoryId { get; set; }

        /// <summary>
        /// Gets or sets the value of product SKU
        /// </summary>
        public string Sku { get; set; }

        /// <summary>
        /// Gets or sets the value of line item name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of line item quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the value of line item currency
        /// </summary>
        /// <value>
        /// Currency code in ISO 4217 format
        /// </value>
        public Currency Currency { get; set; }

        /// <summary>
        /// Gets or sets the value of line item warehouse location
        /// </summary>
        public string WarehouseLocation { get; set; }

        /// <summary>
        /// Gets or sets the value of line item shipping method code
        /// </summary>
        public string ShipmentMethodCode { get; set; }

        /// <summary>
        /// Gets or sets the requirement for line item shipping
        /// </summary>
        public bool RequiredShipping { get; set; }

        /// <summary>
        /// Gets or sets the value of line item thumbnail image absolute URL
        /// </summary>
        public string ThumbnailImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the value of line item image absolute URL
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the flag of line item is a gift
        /// </summary>
        public bool IsGift { get; set; }

        /// <summary>
        /// Gets or sets the collection of line item discounts
        /// </summary>
        /// <value>
        /// Collection od Discount objects
        /// </value>
        public ICollection<Discount> Discounts { get; set; }

        /// <summary>
        /// Gets or sets the value of language code
        /// </summary>
        /// <value>
        /// Culture name in ISO 3166-1 alpha-3 format
        /// </value>
        public string LanguageCode { get; set; }

        /// <summary>
        /// Gets or sets the value of line item comment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets the flag of line item is recurring
        /// </summary>
        public bool IsReccuring { get; set; }

        /// <summary>
        /// Gets or sets flag of line item has tax
        /// </summary>
        public bool TaxIncluded { get; set; }

        /// <summary>
        /// Gets or sets the value of line item volumetric weight
        /// </summary>
        public decimal? VolumetricWeight { get; set; }

        /// <summary>
        /// Gets or sets the value of line item weight unit
        /// </summary>
        public string WeightUnit { get; set; }

        /// <summary>
        /// Gets or sets the value of line item weight
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// Gets or sets the value of line item measurement unit
        /// </summary>
        public string MeasureUnit { get; set; }

        /// <summary>
        /// Gets or sets the value of line item height
        /// </summary>
        public decimal? Height { get; set; }

        /// <summary>
        /// Gets or sets the value of line item length
        /// </summary>
        public decimal? Length { get; set; }

        /// <summary>
        /// Gets or sets the value of line item width
        /// </summary>
        public decimal? Width { get; set; }

        /// <summary>
        /// Gets or sets the value of line item original price
        /// </summary>
        public Money ListPrice { get; set; }

        /// <summary>
        /// Gets or sets the value of line item sale price (include static discount)
        /// </summary>
        public Money SalePrice { get; set; }

        /// <summary>
        /// Gets the value of line item actual price (include all types of discounts)
        /// </summary>
        public Money PlacedPrice
        {
            get
            {
                decimal placedPrice = (SalePrice.Amount > 0 ? SalePrice.Amount : ListPrice.Amount) - DiscountTotal.Amount;

                return new Money(placedPrice, Currency.Code);
            }
        }

        /// <summary>
        /// Gets the value of line item subtotal price (actual price * line item quantity)
        /// </summary>
        public Money ExtendedPrice
        {
            get
            {
                decimal extendedPrice = PlacedPrice.Amount * Quantity;

                return new Money(extendedPrice, Currency.Code);
            }
        }

        /// <summary>
        /// Gets the value of line item total discount amount
        /// </summary>
        public Money DiscountTotal
        {
            get
            {
                decimal discountsAmount = Discounts.Sum(d => d.DiscountAmount.Amount);

                return new Money(discountsAmount, Currency.Code);
            }
        }

        /// <summary>
        /// Gets the value of line item total tax amount
        /// </summary>
        public Money TaxTotal
        {
            get
            {
                decimal taxesAmount = TaxDetails.Sum(td => td.Amount.Amount);

                return new Money(taxesAmount, Currency.Code);
            }
        }

        /// <summary>
        /// Gets or sets the value of line item tax type
        /// </summary>
        public string TaxType { get; set; }

        /// <summary>
        /// Gets or sets the collection of line item tax detalization lines
        /// </summary>
        /// <value>
        /// Collection of TaxDetail objects
        /// </value>
        public ICollection<TaxDetail> TaxDetails { get; set; }
    }
}