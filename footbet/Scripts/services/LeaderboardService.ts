﻿module Services {
    "use strict";

    export class LeaderboardService {
        static $inject = [
            "$http"
        ];

        constructor(private $http) {}

        public getLeaderboard(): ng.IPromise<ILeaderboard> {
            var leaderboard = this.$http({
                method: 'POST',
                url: "../Leaderboard/GetOverallLeaderboardBySportsEventId",
            }).then(response => response.data);
            return leaderboard;
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