﻿(function() {
    "use strict";

    angular
        .module("driveApp")
        .controller("ModalInstanceCtrl", ModalInstanceCtrl);

    ModalInstanceCtrl.$inject = ['FolderService', '$uibModalInstance', 'items'];

    function ModalInstanceCtrl(folderService, $uibModalInstance, items) {
        var vm = this;
        vm.save = save;
        vm.cancel = cancel;
        vm.submitted = false;
        vm.folder = {};

        //vm.title = 'Update Folder';

        activate();

        function activate() {
            vm.folder = items;
            //if (vm.folder == undefined) {
            //    vm.title = 'Create Folder';
            //}
        }

        function save() {
            vm.submitted = true;
            if (vm.folder.name !== undefined) {
                if (vm.folder.id === undefined) {
                    folderService.create(vm.folder,
                        function (response) {
                            if (response)
                                $uibModalInstance.close(response);
                        });
                } else {
                    folderService.updateFolder(vm.folder,
                        function (response) {
                            if (response)
                                $uibModalInstance.close(response);
                        });
                }
            }
        }

        function cancel() {
            $uibModalInstance.dismiss('cancel');
        };
    }
}());