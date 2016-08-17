﻿(function () {
    'use strict';

    angular
        .module('driveApp')
        .factory('SpaceService', SpaceService);

    SpaceService.$inject = ['$http'];

    function SpaceService($http) {
        var service = {
            getSpace: getSpace,
            getAllSpaces: getAllSpaces,
            searchFoldersAndFiles,
            getNumberOfResultSearchFoldersAndFiles
        };

        function getSpace(id, callback) {
            $http.get('/api/spaces/' + id)
                .then(function (response) {
                    if (callback) {
                        callback(response.data);
                    }
                }, function () {
                    console.log('Error while getting space!');
                });
        }

        function getAllSpaces(callback) {
            $http.get('/api/spaces')
            .then(function (response) {
                if (callback) {
                    callback(response.data);
                }
            }, function () {
                console.log('Error while getting all spaces!');
            });
        }

        function searchFoldersAndFiles(spaceId, folderId, text, currentPage, pageSize, callback) {
            $http.get('/api/spaces/' + spaceId + '/search', {
                params: {
                    folderId: folderId,
                    text: text,
                    page: currentPage,
                    count: pageSize
                }
            })
            .then(function (response) {
                if (callback) {
                    callback(response.data);
                }
            }, function () {
                console.log('Error in searchFoldersAndFiles Method!');
            });
        }

        function getNumberOfResultSearchFoldersAndFiles(spaceId, folderId, text, callback) {
            $http.get('/api/spaces/' + spaceId + '/total', {
                params: {
                    folderId: folderId,
                    text: text
                }
            })
            .then(function (response) {
                if (callback) {
                    callback(response.data);
                }
            }, function () {
                console.log('Error in getNumberOfResultSearchFoldersAndFiles Method!');
            });
        }

        return service;
    }

    var app = angular.module('driveApp');

    app.filter('typeOfFile', function () {
        return function (input, uppercase) {
            switch (input) {
                case 0:
                    return 'Undefined';
                case 1:
                    return 'Document';
                case 2:
                    return 'Sheets';
                case 3:
                    return 'Slides';
                case 4:
                    return 'Trello';
                case 5:
                    return 'Link';
                case 6:
                    return 'Physical file';
                default:
                    return '';
            }
        }
    });
})();