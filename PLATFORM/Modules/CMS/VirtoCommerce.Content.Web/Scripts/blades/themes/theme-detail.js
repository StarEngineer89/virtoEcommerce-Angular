﻿angular.module('virtoCommerce.contentModule')
.controller('virtoCommerce.contentModule.themeDetailController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.contentModule.contentApi', 'virtoCommerce.contentModule.themes', function ($scope, bladeNavigationService, contentApi, themes) {
    var blade = $scope.blade;

    blade.refresh = function (parentRefresh) {
        if (blade.isNew) {
            initializeBlade({});
        } else {
            initializeBlade(blade.data);
            if (parentRefresh) {
                blade.parentBlade.initialize();
            }
        }
    };

    function initializeBlade(data) {
        if (blade.isNew) {
            blade.title = 'content.blades.theme-detail.title-new';
            blade.subtitle = 'content.blades.theme-detail.subtitle-new';
        } else {
            blade.title = data.name;
            blade.subtitle = 'content.blades.theme-detail.subtitle';
        }
        blade.currentEntity = angular.copy(data);
        blade.origEntity = data;
        blade.isLoading = false;
    }

    function isDirty() {
        return !angular.equals(blade.currentEntity, blade.origEntity) && blade.hasUpdatePermission();
    }

    function canSave() {
        return isDirty() && $scope.formScope && $scope.formScope.$valid;
    }

    $scope.saveChanges = function () {
        blade.isLoading = true;

        if (blade.isNew) {
            if (blade.currentEntity.defaultTheme) { // create from default
                themes.copyDefaultTheme({
                    storeId: blade.storeId,
                    srcPath: blade.currentEntity.defaultTheme,
                    destPath: blade.currentEntity.name
                }, refreshParentAndClose,
                function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
            } else { // create empty
                contentApi.createFolder({
                    contentType: 'themes',
                    storeId: blade.storeId
                }, blade.currentEntity, refreshParentAndClose,
                function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
            }
        } else {
            var newUrl = blade.origEntity.url.substring(0, blade.origEntity.url.length - blade.origEntity.name.length) + blade.currentEntity.name;
            contentApi.move({
                contentType: 'themes',
                storeId: blade.storeId,
                oldUrl: blade.origEntity.url,
                newUrl: newUrl
            }, function (data) {
                blade.currentEntity.url = newUrl;
                blade.data = blade.currentEntity;
                blade.refresh(true);
            }, function (error) {
                bladeNavigationService.setError('Error ' + error.status, blade);
            });
        }
    };

    function refreshParentAndClose() {
        blade.parentBlade.initialize();
        if (blade.parentBlade.parentBlade)
            blade.parentBlade.parentBlade.refresh(blade.storeId, 'themes');
        angular.copy(blade.currentEntity, blade.origEntity);
        $scope.bladeClose();
    }

    $scope.setForm = function (form) { $scope.formScope = form; };

    blade.onClose = function (closeCallback) {
        bladeNavigationService.showConfirmationIfNeeded(isDirty(), canSave(), blade, $scope.saveChanges, closeCallback, "content.dialogs.theme-save.title", "content.dialogs.theme-save.message");
    };

    if (!blade.isNew) {
        blade.toolbarCommands = [
            {
                name: "platform.commands.save",
                icon: 'fa fa-save',
                executeMethod: $scope.saveChanges,
                canExecuteMethod: canSave,
                permission: blade.updatePermission
            },
            {
                name: "platform.commands.reset",
                icon: 'fa fa-undo',
                executeMethod: function () {
                    angular.copy(blade.origEntity, blade.currentEntity);
                },
                canExecuteMethod: isDirty,
                permission: blade.updatePermission
            }
        ];
    }

    blade.refresh(false);
}]);