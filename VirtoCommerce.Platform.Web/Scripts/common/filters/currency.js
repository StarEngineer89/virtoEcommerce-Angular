﻿angular.module('ng')
.config(['$provide', function ($provide) {
    $provide.decorator('currencyFilter', ['$delegate', function ($delegate) {
        var filter = function (currency, symbol, fractionSize) {
            var result = $delegate.apply(this, [currency, "", fractionSize]);
            result += " " + symbol;
            return result;
        };
        return filter;
    }]);
}]);