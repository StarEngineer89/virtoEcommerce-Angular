﻿angular.module('platformWebApp')
.controller('platformWebApp.licenseDetailController', ['$scope', '$window', 'FileUploader', '$http', 'platformWebApp.dialogService', function ($scope, $window, FileUploader, $http, dialogService) {
    var blade = $scope.blade;
    $scope.license = $scope.license || { "type": "Community", "customerName": "N/A", "expirationDate": "N/A" };

    $scope.activate = function (activationCode, isActivationByCode) {
        blade.isLoading = true;
        $scope.activationError = null;

        if (isActivationByCode) {
            $http.post('api/platform/licensing/activateByCode', JSON.stringify(activationCode)).then(function (response) {
                activationCallback(response.data, isActivationByCode);
            }, function (error) {
                $scope.activationError = error.data.message;
            });
        } else {
            $scope.filename = null;
            uploader.uploadAll();
        }
    };

    function activationCallback(result, isActivationByCode) {
        blade.isLoading = false;

        if (result) {
            if (result.expirationDate && new Date(result.expirationDate) < new Date()) {
                $scope.activationError = 'Activation failed. This license has expired.';
            } else {
                var dialog = {
                    id: "confirm",
                    license: result,
                    callback: function () {
                        // confirmed. Activate the license
                        blade.isLoading = true;
                        $http.post('api/platform/licensing/activateLicense', result).then(function () {
                            $window.location.reload();
                        });
                    }
                };
                dialogService.showDialog(dialog, '$(Platform)/Scripts/app/licensing/dialogs/licenseConfirmation-dialog.tpl.html', 'platformWebApp.confirmDialogController');
            }
        } else {
            $scope.activationError = isActivationByCode ? 'Activation failed. Check the activation code.' : 'Activation failed. Check the license file.';
        }
    }

    // create the uploader
    var uploader = $scope.uploader = new FileUploader({
        scope: $scope,
        headers: { Accept: 'application/json' },
        url: 'api/platform/licensing/activateByFile',
        method: 'POST',
        autoUpload: false,
        removeAfterUpload: true
    });

    // ADDING FILTERS
    // lic only
    uploader.filters.push({
        name: 'licFilter',
        fn: function (i /*{File|FileLikeObject}*/, options) {
            return i.name.toLowerCase().endsWith('.lic');
        }
    });

    uploader.onAfterAddingFile = function (fileItem) {
        $scope.filename = fileItem.file.name;
        $scope.activationError = null;
    };

    uploader.onSuccessItem = function (item, response, status, headers) {
        activationCallback(response, false);
    };

    uploader.onErrorItem = function (item, response, status) {
        blade.isLoading = false;
        $scope.activationError = response.message ? response.message : status;
    };

    blade.title = 'platform.blades.license.title';
    blade.headIcon = 'fa-info-circle';
    blade.isLoading = false;
}])

.config(['$stateProvider', function ($stateProvider) {
    $stateProvider
        .state('workspace.appLicense', {
            url: '/appLicense',
            templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
            controller: ['platformWebApp.bladeNavigationService', function (bladeNavigationService) {
                var blade = {
                    id: 'appLicense',
                    controller: 'platformWebApp.licenseDetailController',
                    template: '$(Platform)/Scripts/app/licensing/license-detail.tpl.html'
                };
                bladeNavigationService.showBlade(blade);
            }]
        });
}]);