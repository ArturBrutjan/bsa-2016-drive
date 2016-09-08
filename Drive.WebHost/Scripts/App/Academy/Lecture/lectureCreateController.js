﻿(function () {
    angular.module('driveApp.academyPro')
        .controller('LectureCreateController', LectureCreateController);

    LectureCreateController.$inject = [
        'LectureService',
        'toastr',
        '$location',
        '$routeParams',
         'AcademyService'
    ];

    function LectureCreateController(lectureService, toastr, $location, $routeParams, academyService) {
        var vm = this;
        vm.currentAcademyId = $routeParams.id;
        vm.addNewLecture = addNewLecture;
        vm.create = create;
        vm.cancel = cancel;
        vm.submitVideo = submitVideo;
        vm.submitSlide = submitSlide;
        vm.submitRepository = submitRepository;
        vm.submitSample = submitSample;
        vm.submitUseful = submitUseful;
        vm.getAcademy = getAcademy;
        vm.getCourseList = getCourseList;

        activate();

        function activate() {
            vm.lecture = {
                courseId: vm.currentAcademyId,
                videoLinks: [],
                slidesLinks: [],
                sampleLinks: [],
                usefulLinks: [],
                repositoryLinks: [],
                codeSamples: []
            };

            vm.calendar = {
                isOpen: false,
                openCalendar: openCalendar,
                timepickerOptions: {
                    showMeridian: false
                }
            };

            vm.calendarHomeTask = {
                isOpen: false,
                openCalendarHomeTask: openCalendarHomeTask,
                timepickerOptions: {
                    showMeridian: false
                }
            }
        }

        function addNewLecture() {
            return lectureService.pushData(vm.lecture);
        };

        function getAcademy(id) {
            return academyService.getAcademy(id)
                .then(function (data) {
                    vm.academy = data;
                    return vm.academy;
                });
        }

        function create() {
            if (vm.lecture.name) {
                vm.addNewLecture()
                    .then(function() {
                        $location.url('/apps/academy/' + vm.currentAcademyId);
                    });
                toastr.success(
                'New lecture was added successfully!', 'Academy Pro',
                {
                    closeButton: true, timeOut: 6000
                });
            }
        }

        function cancel() {

        }

        function openCalendar(e) {
            e.preventDefault();
            e.stopPropagation();

            vm.calendar.isOpen = true;
        };

        function openCalendarHomeTask(e) {
            e.preventDefault();
            e.stopPropagation();

            vm.calendarHomeTask.isOpen = true;
        };

        function submitVideo() {
            if (vm.currentVideo.name && vm.currentVideo.link) {
                vm.lecture.videoLinks.push(vm.currentVideo);
                vm.currentVideo = {};
            }
        }

        function submitSlide() {
            if (vm.currentSlide.name && vm.currentSlide.link) {
                vm.lecture.slidesLinks.push(vm.currentSlide);
                vm.currentSlide = {};
            }
        }

        function submitRepository() {
            if (vm.currentRepository.name && vm.currentRepository.link) {
                vm.lecture.repositoryLinks.push(vm.currentRepository);
                vm.currentRepository = {};
            }
        }

        function submitSample() {
            if (vm.currentSample.name && vm.currentSample.link) {
                vm.lecture.sampleLinks.push(vm.currentSample);
                vm.currentSample = {};
            }
        }

        function submitUseful() {
            if (vm.currentuseful.name && vm.currentuseful.link) {
                vm.lecture.usefulLinks.push(vm.currentuseful);
                vm.currentuseful = {};
            }
        }

        function getCourseList() {
            $location.url('/apps/academy/');
        }
    }
})();