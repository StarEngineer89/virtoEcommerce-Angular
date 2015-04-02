﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VirtoCommerce.Domain.Marketing.Model;
using VirtoCommerce.Foundation.Frameworks;

namespace VirtoCommerce.MarketingModule.Web.Model.TypeExpressions
{
	public interface IConditionExpression
	{
		System.Linq.Expressions.Expression<Func<IPromotionEvaluationContext, bool>> GetConditionExpression();
	
	}
}