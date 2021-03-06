﻿module Controllers {
    "use strict";

    export class ResultPageController {
        message: string;

        static $inject = [
            "$scope",
            "$rootScope",
            "betBaseController",
            "resultPageService"
        ];

        constructor(private $scope: ng.IScope,
            private $rootScope: ng.IRootScopeService,
            private betBaseController: Services.BetBaseController,
            private resultPageService: Services.ResultPageService) {
            $scope.$on('modelLoaded', ()=> {
                this.resultPageControllerInit();
            });
            this.loadResult();

        }
        private loadResult() {
            this.resultPageService.loadResult().then((betViewModel) => {
                this.betBaseController.groups = betViewModel.groups;
                this.betBaseController.initializeGroupsForBet();
                this.betBaseController.initializePlayoffGamesForBet(betViewModel.playoffGames);
                this.$rootScope.$broadcast('modelLoaded', true);
            });
        }

        private initializeGroupsAndPlayoffGames() {
            angular.forEach(this.betBaseController.groups, (group)=> {
                this.betBaseController.setWinnerAndRunnerUpInGroup(group);
                this.betBaseController.setPlayoffGameTeams(group, true);
            });
        }

        private resultPageControllerInit () {
            this.initializeGroupsAndPlayoffGames();
        }
    }
}

angular
    .module("footballCompApp")
    .controller("ResultPageController", Controllers.ResultPageController);