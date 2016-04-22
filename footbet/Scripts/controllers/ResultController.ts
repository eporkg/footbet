﻿/// <reference path="../../typings/angularjs/angular.d.ts" />
/// <reference path="../../typings/angularjs/angular-resource.d.ts" />
module Controllers {
    "use strict";

    export class ResultController {
        message: string;

        static $inject = [
            "$scope",
            "betBaseController",
            "betService"
        ];

        constructor(private $scope: ng.IScope,
            private betBaseController: Services.BetBaseController,
            private betService: Services.BetService) {
            this.betBaseController.isRequired = false;
            $scope.$on('modelLoaded', () => {
                this.userBetControllerInit();
            });
        }

        private saveResultBets() {
            this.betService.saveBetResult(this.betBaseController.groups, this.betBaseController.playoffGames).then((response) => {
                this.message = response;
            });
        }

        private initializeGroupsAndPlayoffGames() {
            angular.forEach(this.betBaseController.groups, (group) => {
                this.betBaseController.setWinnerAndRunnerUpInGroup(group);
            });
        }

        private userBetControllerInit() {
            this.initializeGroupsAndPlayoffGames();
        }
    }
}