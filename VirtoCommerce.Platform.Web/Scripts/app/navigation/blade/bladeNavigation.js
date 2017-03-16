angular.module('platformWebApp')
.factory('platformWebApp.toolbarService', function () {
    var toolbarCommandsMap = [];
    return {
        register: function (toolbarItem, toolbarController) {
            var map = toolbarCommandsMap;
            if (!map[toolbarController]) {
                map[toolbarController] = [];
            }

            map[toolbarController].push(toolbarItem);
            map[toolbarController].sort(function (a, b) {
                return a.index - b.index;
            });
        },
        resolve: function (bladeCommands, toolbarController) {
            var externalCommands = toolbarCommandsMap[toolbarController];
            if (externalCommands) {
                bladeCommands = angular.copy(bladeCommands || []);

                _.each(externalCommands, function (newCommand) {
                    bladeCommands.splice(newCommand.index, 0, newCommand);
                });
            }

            return bladeCommands;
        }
    };
})
.directive('vaBladeContainer', ['platformWebApp.bladeNavigationService', function (bladeNavigationService) {
    return {
        restrict: 'E',
        replace: true,
        templateUrl: '$(Platform)/Scripts/app/navigation/blade/bladeContainer.tpl.html',
        link: function (scope) {
            scope.blades = bladeNavigationService.stateBlades();
        }
    }
}])
.directive('vaBlade', ['$compile', 'platformWebApp.bladeNavigationService', 'platformWebApp.toolbarService', '$timeout', '$document', function ($compile, bladeNavigationService, toolbarService, $timeout, $document) {
    return {
        terminal: true,
        priority: 100,
        link: function (scope, element) {
            element.attr('ng-controller', scope.blade.controller);
            element.attr('id', scope.blade.id);
            element.attr('ng-model', "blade");
            element.removeAttr("va-blade");
            $compile(element)(scope);
            scope.blade.$scope = scope;

            var mainContent = $('.cnt');
            var blade = $('.blade:last', mainContent);
            var offset = parseInt(blade.offset().left);

            if (!scope.blade.disableOpenAnimation && !scope.blade.state) {
                blade.css('margin-left', '-' + blade.width() + 'px').addClass('__animate');

                setTimeout(function () {
                    blade.animate({ 'margin-left': 0 }, 250, function () {
                        blade.removeAttr('style').removeClass('__animate');
                    });
                }, 0);
            }

            var recalculateWidth = function () {
                if (scope.blade.state) {
                    var currentBlade = $(element);
                    var mainMenu = $('.nav-bar');
                    var parentBlade = scope.blade.parentBlade ? $('#' + scope.blade.parentBlade.id) : undefined;
                    var contentWidth = '420px';
                    var innerWidth = '380px';
                    if (scope.blade.state == 'expanded') {
                        var widthOffset = mainMenu.width() + (parentBlade ? parentBlade.width() + 8 : 0) + 8;
                        contentWidth = 'calc(100vw - ' + parseInt(widthOffset) + 'px)';
                        innerWidth = 'calc(100vw - ' + parseInt(widthOffset + 40) + 'px)';
                    }
                    var contentblock = currentBlade.find(".blade-content");
                    $(contentblock).width(contentWidth);
                    $(contentblock).find('.inner-block').width(innerWidth);

                    setVisibleToolsLimit();
                }
            }
            
            scope.$on('$includeContentLoaded', function () {
                recalculateWidth();
            });

            scope.$watch('blade.state', function() {
                recalculateWidth();
            });

            $timeout(function () {
                var scrollAdjustment = (offset >= mainContent.scrollLeft()) ? blade.width() : mainContent.scrollLeft();
                mainContent.animate({ scrollLeft: offset + scrollAdjustment + 'px' }, 0); //500);
            }, 0, false);

            scope.bladeClose = function (onAfterClose) {
                bladeNavigationService.closeBlade(scope.blade, onAfterClose, function (callback) {
                    blade.addClass('__animate').animate({ 'margin-left': '-' + blade.width() + 'px' }, 125, function () {
                        blade.remove();
                        callback();
                    });
                });
            };

            scope.$watch('blade.toolbarCommands', function (toolbarCommands) {
                scope.resolvedToolbarCommands = toolbarService.resolve(toolbarCommands, scope.blade.controller);

                setVisibleToolsLimit();
            }, true);

            var toolbar = blade.find(".blade-toolbar .menu.__inline");

            function setVisibleToolsLimit() {
                scope.toolsPerLineCount = scope.resolvedToolbarCommands ? scope.resolvedToolbarCommands.length : 1;

                $timeout(function () {
                    if (toolbar.height() > 55 && scope.toolsPerLineCount > 1) {
                        var maxToolbarWidth = toolbar.width() - 46; // the 'more' button is 46px wide
                        //console.log(toolbar.width() + 'maxToolbarWidth: ' + maxToolbarWidth);
                        var toolsWidth = 0;
                        var lis = toolbar.find("li");
                        var i = 0;
                        while (maxToolbarWidth > toolsWidth && lis.length > i) {
                            toolsWidth += lis[i].clientWidth;
                            i++;
                        }
                        scope.toolsPerLineCount = i - 1;
                    }
                }, 220);
            }

            function handleClickEvent() {
                setVisibleToolsLimit();
                $document.unbind('click', handleClickEvent);
            }

            scope.showMoreTools = function (event) {
                scope.toolsPerLineCount = scope.resolvedToolbarCommands.length;

                event.stopPropagation();
                $document.bind('click', handleClickEvent);
            };
        }
    };
}])
.factory('platformWebApp.bladeNavigationService', ['platformWebApp.authService', '$timeout', '$state', 'platformWebApp.dialogService', function (authService, $timeout, $state, dialogService) {

    function showConfirmationIfNeeded(showConfirmation, canSave, blade, saveChangesCallback, closeCallback, saveTitle, saveMessage) {
        if (showConfirmation) {
            var dialog = { id: "confirmCurrentBladeClose" };

            if (canSave) {
                dialog.title = saveTitle;
                dialog.message = saveMessage;
            } else {
                dialog.title = "Warning";
                dialog.message = "Validation failed for this object. Will you continue editing and save later?";
            }

            dialog.callback = function (userChoseYes) {
                if (canSave) {
                    if (userChoseYes) {
                        saveChangesCallback();
                    }
                    closeCallback();
                } else if (!userChoseYes) {
                    closeCallback();
                }
            };

            dialogService.showConfirmationDialog(dialog);
        }
        else {
            closeCallback();
        }
    }

    var service = {
        blades: [],
        currentBlade: undefined,
        showConfirmationIfNeeded: showConfirmationIfNeeded,
        closeBlade: function (blade, callback, onBeforeClosing) {
            //Need in case a copy was passed
            blade = service.findBlade(blade.id) || blade;

            // close all children
            service.closeChildrenBlades(blade, function () {
                var doCloseBlade = function () {
                    if (angular.isFunction(onBeforeClosing)) {
                        onBeforeClosing(doCloseBladeFinal);
                    } else {
                        doCloseBladeFinal();
                    }
                }

                var doCloseBladeFinal = function () {
                    var idx = service.stateBlades().indexOf(blade);
                    if (idx >= 0) service.stateBlades().splice(idx, 1);

                    //remove blade from children collection
                    if (angular.isDefined(blade.parentBlade)) {
                        var childIdx = blade.parentBlade.childrenBlades.indexOf(blade);
                        if (childIdx >= 0) {
                            blade.parentBlade.childrenBlades.splice(childIdx, 1);
                        }
                    }
                    if (angular.isFunction(callback)) {
                        $timeout(callback, 60);
                    };
                };

                if (angular.isFunction(blade.onClose)) {
                    blade.onClose(doCloseBlade);
                }
                else {
                    doCloseBlade();
                }
            });

            if (blade.parentBlade && blade.parentBlade.state == 'collapsed') {
                blade.parentBlade.state = 'expanded';
            }
        },
        closeChildrenBlades: function (blade, callback) {
            if (blade && _.any(blade.childrenBlades)) {
                angular.forEach(blade.childrenBlades.slice(), function (child) {
                    service.closeBlade(child, function () {
                        // show only when all children were closed
                        if (blade.childrenBlades.length == 0 && angular.isFunction(callback)) {
                            callback();
                        }
                    });
                });
            } else if (angular.isFunction(callback)) {
                callback();
            }
        },
        stateBlades: function (stateName) {
            if (angular.isUndefined(stateName)) {
                stateName = $state.current.name;
            }

            if (angular.isUndefined(service.blades[stateName])) {
                service.blades[stateName] = [];
            }

            return service.blades[stateName];
        },
        findBlade: function (id) {
            var found;
            angular.forEach(service.stateBlades(), function (blade) {
                if (blade.id == id) {
                    found = blade;
                }
            });

            return found;
        },
        showBlade: function (blade, parentBlade) {
            blade.isLoading = true;
            blade.parentBlade = parentBlade;
            blade.childrenBlades = [];
            if (parentBlade) {
                blade.headIcon = parentBlade.headIcon;
                blade.updatePermission = parentBlade.updatePermission;
            }
            //copy securityscopes from parent blade
            if (parentBlade != null && parentBlade.securityScopes) {
                //need merge scopes
                if (angular.isArray(blade.securityScopes) && angular.isArray(parentBlade.securityScopes)) {
                    blade.securityScopes = parentBlade.securityScopes.concat(blade.securityScopes);
                }
                else {
                    blade.securityScopes = parentBlade.securityScopes;
                }
            }

            var existingBlade = service.findBlade(blade.id);

            //Show blade in previous location
            if (existingBlade != undefined) {
                //store prev blade x-index
                blade.xindex = existingBlade.xindex;
            } else if (!angular.isDefined(blade.xindex)) {
                //Show blade as last one by default
                blade.xindex = service.stateBlades().length;
            }

            var showBlade = function () {
                if (angular.isDefined(parentBlade)) {
                    blade.xindex = service.stateBlades().indexOf(parentBlade) + 1;
                    parentBlade.childrenBlades.push(blade);
                }
                //show blade in same place where it was
                service.stateBlades().splice(Math.min(blade.xindex, service.stateBlades().length), 0, blade);
                service.currentBlade = blade;
            };

            if (angular.isDefined(parentBlade) && parentBlade.childrenBlades.length > 0) {
                service.closeChildrenBlades(parentBlade, showBlade);
            }
            else if (angular.isDefined(existingBlade)) {
                service.closeBlade(existingBlade, showBlade);
            }
            else {
                showBlade();
            }

            if (parentBlade && parentBlade.state == 'expanded') {
                parentBlade.state = 'collapsed';
            }

            blade.hasUpdatePermission = function () {
                return authService.checkPermission(blade.updatePermission, blade.securityScopes);
            };
        },
        checkPermission: authService.checkPermission,
        setError: function (msg, blade) {
            if (blade) {
                blade.isLoading = false;
                blade.error = msg;
            }
        }
    };

    return service;
}]);