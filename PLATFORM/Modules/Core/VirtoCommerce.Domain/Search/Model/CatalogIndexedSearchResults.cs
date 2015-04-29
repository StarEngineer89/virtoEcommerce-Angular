﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VirtoCommerce.Foundation.Search;
using System.Runtime.Serialization;
using VirtoCommerce.Foundation.Catalogs.Model;
using VirtoCommerce.Foundation.Search.Facets;

namespace VirtoCommerce.Domain.Search
{
    public class CatalogItemSearchResults
    {

		public CatalogItemSearchResults()
		{
		}

		public CatalogItemSearchResults(ISearchCriteria criteria, Dictionary<string, Dictionary<string, object>> items, SearchResults results)
		{
			_Items = items;
			_TotalCount = results.TotalCount;
			_Count = results.DocCount;
			_FacetGroups = results.FacetGroups;
			_SearchCriteria = criteria;
		}

        ISearchCriteria _SearchCriteria;

        public virtual ISearchCriteria SearchCriteria
        {
            get { return _SearchCriteria; }
        }

        Dictionary<string, Dictionary<string, object>> _Items;

        public virtual Dictionary<string, Dictionary<string,object>> Items
        { 
            get
            {
                return _Items;
            }
        }

        int _TotalCount = 0;

        public virtual int TotalCount
        {
            get
            {
                return _TotalCount;
            }
        }

        int _Count = 0;
        public virtual int Count
        {
            get
            {
                return _Count;
            }
        }

        FacetGroup[] _FacetGroups = null;
        public virtual FacetGroup[] FacetGroups
        {
            get
            {
                return _FacetGroups;
            }
        }

    
    }
}
