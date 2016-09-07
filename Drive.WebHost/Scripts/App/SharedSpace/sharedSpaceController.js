﻿(function () {
    "use strict";

    angular
        .module("driveApp")
        .controller("SharedSpaceController", SharedSpaceController);

    SharedSpaceController.$inject = ['SharedSpaceService', 'FolderService', 'FileService', '$uibModal', 'localStorageService', '$routeParams', '$location'];

    function SharedSpaceController(sharedSpaceService, folderService, fileService, $uibModal, localStorageService, $routeParams, $location) {
        var vm = this;

        vm.folderList = [];
        vm.addElem = addElem;
        vm.deleteElems = deleteElems;

        vm.space = {
            name: 'Shared Space',
            rootFolderId: null,
            folderId: null,
            folders: [],
            files: []
        }

        vm.getFolderContent = getFolderContent;
        vm.getFile = getFile;
        vm.getSharedData = getSharedData;
        vm.getSpaceByButton = getSpaceByButton;

        vm.search = search;
        vm.cancelSearch = cancelSearch;
        vm.searchText = '';

        vm.orderByColumn = orderByColumn;

        vm.paginate = {
            currentPage: 1,
            pageSize: 2,
            numberOfItems: 0,
            getContent: null
        }

        vm.pageChanged = function (pageNumber) {
            vm.paginate.currentPage = pageNumber;
            vm.paginate.getContent();
        }
        vm.chooseIcon = chooseIcon;

        activate();

        // TODO change method 
        function activate() {
            vm.space.rootFolderId = null;
            vm.space.folderId = null;

            vm.view = "fa fa-th";
            vm.showTable = true;
            vm.showGrid = false;
            vm.changeView = changeView;
            vm.sortByDate = null;
            vm.reverse = false;
            vm.iconHeight = 30;
            vm.columnForOrder = 'name';

            sharedSpaceService.getSharedData(vm.paginate.currentPage, vm.paginate.pageSize, vm.sortByDate, vm.space.folderId, vm.space.rootFolderId, function (data) {
                vm.space.files = data.files;
                //vm.spaceId = data.id;

                if (localStorageService.get('spaceId') !== vm.spaceId) {
                    localStorageService.set('spaceId', vm.spaceId);
                    localStorageService.set('current', null);
                    localStorageService.set('list', null)
                }

                if (localStorageService.get('list') != null)
                    vm.folderList = localStorageService.get('list');

                if (localStorageService.get('current') != null) {
                    vm.parentId = localStorageService.get('current');
                    getFolderContent(vm.parentId);
                } else {
                    getSharedData();
                }

            });
        }

        function getSharedData() {
            vm.searchText = '';
            vm.parentId = null;
            vm.paginate.currentPage = 1;
            getSharedContent();
            getSharedDataTotal();
            vm.paginate.getContent = getSharedContent;
        }

        function getSharedContent() {
            sharedSpaceService.getSharedData(vm.paginate.currentPage, vm.paginate.pageSize, vm.sortByDate, vm.space.folderId, vm.space.rootFolderId, function (data) {
                vm.space.files = data.files;
                vm.space.folders = data.folders;
            });
        }

        function getSpaceByButton() {
            vm.space.rootFolderId = null;
            vm.space.folderId = null;
            getSharedData();
            localStorageService.set('list', []);
            vm.folderList = localStorageService.get('list');
            localStorageService.set('current', null);
        }

        function getSharedDataTotal() {
            sharedSpaceService.getSharedDataTotal(vm.space.folderId, vm.space.rootFolderId, function (data) {
                vm.paginate.numberOfItems = data;
            });
        }

        function changeView(view) {
            if (view == "fa fa-th") {
                activateGridView();
            } else {
                activateTableView();
            }
        }

        function activateTableView() {
            vm.view = "fa fa-th";
            vm.showTable = true;
            vm.showGrid = false;
        }

        function activateGridView() {
            vm.view = "fa fa-list";
            vm.showTable = false;
            vm.showGrid = true;
        }

        vm.folderMenuOptions = [
            ['Delete', function ($itemScope) {
                sharedSpaceService.deleteSharedContent($itemScope.folder.id, function () {
                    if (vm.space.folders.lenght = 1 && vm.paginate.currentPage != 1) {
                        vm.paginate.currentPage--;
                        vm.paginate.numberOfItems--;
                        vm.paginate.getContent();
                    }
                    else {
                        vm.paginate.numberOfItems--;
                        vm.paginate.getContent();
                    }
                });
            }]
        ];

        vm.fileMenuOptions = [
           ['Delete', function ($itemScope) {
               sharedSpaceService.deleteSharedContent($itemScope.file.id, function () {
                   if (vm.space.files.lenght = 1 && vm.paginate.currentPage != 1) {
                       vm.paginate.currentPage--;
                       vm.paginate.numberOfItems--;
                       vm.paginate.getContent();
                   }
                   else {
                       vm.paginate.numberOfItems--;
                       vm.paginate.getContent();
                   }
               });
           }]
        ];

        function getFolderContent(id) {
            if (vm.space.rootFolderId == null) {
                vm.space.rootFolderId = id;
            }
            vm.space.folderId = id;
            getSharedData();
            localStorageService.set('current', id);
        }

        function getFile(id) {
            fileService.getFile(id, function (file) {
                vm.file = file;
            });
        }

        function addElem(folder) {
            vm.folderList.push(folder);
            localStorageService.set('list', vm.folderList);
        }

        function deleteElems(folder) {
            for (var i = vm.folderList.length - 1; i > -1; i--) {
                if (vm.folderList[i] === folder) {
                    break;
                }
                vm.folderList.splice(i, 1);
            }

            localStorageService.set('list', vm.folderList);
        }

        function search() {
            vm.paginate.currentPage = 1;
            vm.paginate.getContent = getResultSearchFoldersAndFiles;
            getResultSearchFoldersAndFiles();
            getNumberOfResultSearch();
        }

        function cancelSearch() {
            vm.searchText = '';
            vm.paginate.currentPage = 1;
            vm.paginate.getContent = getResultSearchFoldersAndFiles;
            getResultSearchFoldersAndFiles();
            getNumberOfResultSearch();
        }

        //TODO rename method
        // deleted param: vm.spaceId, vm.parentId

        function getResultSearchFoldersAndFiles() {
            sharedSpaceService.search(vm.searchText, vm.paginate.currentPage,vm.paginate.pageSize, function (data) {
                vm.space.folders = data.folders;
                vm.space.files = data.files;
            });
        }

        //TODO rename method
        // deleted param: vm.spaceId, vm.parentId

        function getNumberOfResultSearch(){
            sharedSpaceService.searchTotal(vm.searchText, function (data) {
                vm.paginate.numberOfItems = data;
            });
        }

        function openDocument(url) {
            window.open(url, '_blank');
        }

        function orderByColumn(column) {
            vm.columnForOrder = fileService.orderByColumn(column, vm.columnForOrder);
        }

        function chooseIcon(type) {
            vm.iconSrc = fileService.chooseIcon(type);
            return vm.iconSrc;
        }
        

    }
}());