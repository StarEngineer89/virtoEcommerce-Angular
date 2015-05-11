﻿angular.module('virtoCommerce.helpdeskModule')
    .factory('zendesk_res_authlink', [
        '$resource', function($resource) {
            return $resource('api/help/authorize', {
                update: { method: 'PUT' }
            });
        }
    ])


    .factory('virtoCommerce.helpdeskModule.user_tickets', [
        '$resource', function ($resource) {
            return $resource('api/help/tickets/:status/:email', { status: '@Status', email: '@Email' }, {
                update: { method: 'PUT' }
            });
        }
    ]);