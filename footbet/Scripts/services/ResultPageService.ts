﻿module Services {
    "use strict";

    export class ResultPageService {
        static $inject = [
            "$http"
        ];

        constructor(private $http) { }

        public loadResult(): ng.IPromise<IBetViewModel> {
            var response = this.$http({
                    url: "../ResultPage/GetResults",
                    method: "POST"
                }).then(response => {
                    return response.data;
                })
                .catch(error => { return error.status });
            return response;
        }

        public getLeaderboardForLeague(leagueId: number): ng.IPromise<ILeaderboard> {
            var leaderboard = this.$http({
                method: 'POST',
                url: "../Leaderboard/GetLeaderboardByLeagueId",
                data: { leagueId: leagueId }
            }).then(response => response.data);
            return leaderboard;
        };
    }
}