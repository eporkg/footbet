﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Footbet.Models.DomainModels;
using Footbet.Models.Enums;
using Footbet.Repositories.Contracts;
using Footbet.ScoreCalculations;
using Footbet.Services.Contracts;
using WebGrease.Css.Extensions;

namespace Footbet.Services
{
    public class UserScoreService : IUserScoreService
    {
        private readonly IUserScoreRepository _userScoreRepository;
        private readonly IGameRepository _gameRepository;
        private readonly IScoreBasisRepository _scoreBasisRepository;
        private readonly IUserBetRepository _userBetRepository;

        private readonly IGameScoreCalculator _gameScoreCalculator;
        private readonly ITopScorerScoreCalculator _topScorerScoreCalculator;
        private readonly IBonusScoreCalculator _bonusScoreCalculator;

        public UserScoreService(
            IUserScoreRepository userScoreRepository,
            IGameRepository gameRepository,
            IScoreBasisRepository scoreBasisRepository,
            IGameScoreCalculator gameScoreCalculator,
            IUserBetRepository userBetRepository,
            ITopScorerScoreCalculator topScorerScoreCalculator, IBonusScoreCalculator bonusScoreCalculator)
        {
            _userScoreRepository = userScoreRepository;
            _gameRepository = gameRepository;
            _scoreBasisRepository = scoreBasisRepository;
            _gameScoreCalculator = gameScoreCalculator;
            _userBetRepository = userBetRepository;
            _topScorerScoreCalculator = topScorerScoreCalculator;
            _bonusScoreCalculator = bonusScoreCalculator;
        }

        public string UpdateUserScores(UserBet referenceUserBet, int sportsEventId = 1)
        {
            var invalidUserBets = "";
            var gameIdsToUsersWithMaxScore = new Dictionary<int, List<string>>();
            var userBets = _userBetRepository.GetAllUserBetsBySportsEventIdWithoutResultBet(sportsEventId);

            var groupGames = _gameRepository.GetGroupGamesBySportsEventId(sportsEventId);

            var scoreBases = _scoreBasisRepository.GetScoreBasisesBySportsEventId(sportsEventId);
            var userScores = _userScoreRepository.GetAllUserScoresBySportsEventId(sportsEventId);
            var maxGroupGameScore = scoreBases.Where(x => x.GameType == (int) GameType.GroupGame).Max(x => x.Points);
            AddNewUserScoresBasedOnUserBets(sportsEventId, userBets, userScores);

            foreach (var userBet in userBets)
            {
                if (UserBetIsInvalid(userBet))
                {
                    invalidUserBets += "   " + userBet.UserId;
                    continue;
                }

                var referenceBets = referenceUserBet.Bets;
                var referencePlayoffBets = referenceUserBet.PlayoffBets;

                var currentUserScore = userScores.First(x => x.UserId == userBet.UserId);
                currentUserScore.Score = 0;

                foreach (var referenceBet in referenceBets)
                {
                    var currentBet = userBet.Bets.Single(x => x.GameId == referenceBet.GameId);
                    var currentGame = groupGames.First(x => x.Id == referenceBet.GameId);
                    var scoreForGame =
                        _gameScoreCalculator.GetScoreForUserOnGame(referenceBet, currentBet, currentGame, scoreBases);
                    if (scoreForGame == maxGroupGameScore)
                    {
                        if (gameIdsToUsersWithMaxScore.ContainsKey(currentGame.Id))
                        {
                            gameIdsToUsersWithMaxScore[currentGame.Id].Add(userBet.UserId);
                        }
                        else
                        {
                            gameIdsToUsersWithMaxScore.Add(currentGame.Id, new List<string> {userBet.UserId});
                        }
                    }


                    currentUserScore.Score += scoreForGame;
                }

                currentUserScore.TopScorerScore = _topScorerScoreCalculator.Calculate(referenceUserBet, userBet, scoreBases);
                currentUserScore.PlayoffScore = AddScoresForPlayoffGames(referencePlayoffBets, userBet, scoreBases);
                currentUserScore.BonusScore = 0;
            }

            AddBonusPoints(userScores, userBets, gameIdsToUsersWithMaxScore);
            _userScoreRepository.SaveOrUpdateUserScores(userScores);

            return invalidUserBets;
        }

        private void AddBonusPoints(List<UserScore> userScores, List<UserBet> userBets, Dictionary<int, List<string>> gameIdsToUsersWithMaxScore)
        {
            var bonusScoresPerUser = _bonusScoreCalculator.GetBonusScoresPerUser(userScores, userBets.Count, gameIdsToUsersWithMaxScore);
            foreach (var userScore in userScores)
            {
                var userBonusPoints = bonusScoresPerUser.FirstOrDefault(x => x.Key == userScore.UserId);
                userScore.BonusScore = userBonusPoints.Value;
            }
        }

        private bool UserBetIsInvalid(UserBet userBet)
        {
            return userBet.Bets.Count != 48 ||
                   userBet.PlayoffBets.Count != 16;
        }

        private static int AddScoresForPlayoffGames(ICollection<PlayoffBet> referencePlayoffBets, UserBet userBet, List<ScoreBasis> scoreBasis)
        {
            var playoffScore = 0;
            foreach (GameType gameType in (GameType[]) Enum.GetValues(typeof(GameType)))
            {
                if ((int) gameType == 1) continue;
                var scoreForGame = AddScoreForGameType(referencePlayoffBets, userBet, scoreBasis, (int) gameType);
                playoffScore += scoreForGame;
            }

            return playoffScore;
        }

        private static int AddScoreForGameType(IEnumerable<PlayoffBet> referencePlayoffBets, UserBet userBet,
            IEnumerable<ScoreBasis> scoreBasis, int gameType)
        {
            var playoffBetsByGameTypeReference = referencePlayoffBets.Where(x => x.GameType == gameType).ToList();
            var usersBetByGameType = userBet.PlayoffBets.Where(x => x.GameType == gameType).ToList();
            var scoreBasisByGameType = scoreBasis.Where(x => x.GameType == gameType).ToList();
            var playoffTeamsByGameTypeReference = CreateListOfPlayoffGamesByGameType(playoffBetsByGameTypeReference);
            var usersPlayoffTeamsByGameType = CreateListOfPlayoffGamesByGameType(usersBetByGameType);
            if (gameType >= 5)
            {
                var endingGameScore = AddScoreForEndingPlayoffGames(playoffBetsByGameTypeReference, usersBetByGameType,
                    scoreBasisByGameType, gameType);
                if (gameType == (int) GameType.Finals)
                {
                    var gameScore = playoffTeamsByGameTypeReference
                        .Where(usersPlayoffTeamsByGameType.Contains)
                        .Sum(playoffBetReference => 10);
                    endingGameScore += gameScore;
                }
                   
                return endingGameScore;
            }
            
            return AddScoreForNotEndingPlayoffGames(playoffTeamsByGameTypeReference, usersPlayoffTeamsByGameType,
                scoreBasisByGameType);
        }

        private static int AddScoreForNotEndingPlayoffGames(IEnumerable<int?> playoffTeamsByGameTypeReference,
            ICollection<int?> usersPlayoffTeamsByGameType, List<ScoreBasis> scoreBasisByGameType)
        {
            return playoffTeamsByGameTypeReference
                .Where(usersPlayoffTeamsByGameType.Contains)
                .Sum(playoffBetReference => scoreBasisByGameType.First().Points);
        }

        private static int AddScoreForEndingPlayoffGames(IEnumerable<PlayoffBet> playoffBetsByGameTypeReference,
            IEnumerable<PlayoffBet> usersBetByGameType, List<ScoreBasis> scoreBasisByGameType, int gameType)
        {
            var scoreForGameType = 0;
            var betsByGameTypeReference = playoffBetsByGameTypeReference as PlayoffBet[] ??
                                          playoffBetsByGameTypeReference.ToArray();
            if (!betsByGameTypeReference.Any())
                return 0;

            var playoffGameResult = betsByGameTypeReference.First();

            var userPlayoffGame = usersBetByGameType.First();

            var winningTeamResult = GetWinningTeamFromGame(playoffGameResult);
            var runnerUpTeamResult = GetRunnerUpTeamFromGame(playoffGameResult);


            var usersWinningTeam = GetWinningTeamFromGame(userPlayoffGame);
            var usersRunnerUpTeam = GetRunnerUpTeamFromGame(userPlayoffGame);

            if (winningTeamResult != null && winningTeamResult == usersWinningTeam)
            {
                var scoreBasisWinningTeam = scoreBasisByGameType.First(x => !x.IsRunnerUp);
                scoreForGameType += scoreBasisWinningTeam.Points;
            }

            if (runnerUpTeamResult != null && runnerUpTeamResult == usersRunnerUpTeam)
            {
                var scoreBasisRunnerUp = scoreBasisByGameType.First(x => x.IsRunnerUp);
                scoreForGameType += scoreBasisRunnerUp.Points;
            }

            return scoreForGameType;
        }

        private static int? GetRunnerUpTeamFromGame(PlayoffBet playoffGame)
        {
            if (playoffGame.Result == null || playoffGame.Result == 1) return null;
            return playoffGame.Result == 0 ? playoffGame.AwayTeam : playoffGame.HomeTeam;
        }

        private static int? GetWinningTeamFromGame(PlayoffBet playoffGame)
        {
            if (playoffGame.Result == null || playoffGame.Result == 1) return null;
            return playoffGame.Result == 0 ? playoffGame.HomeTeam : playoffGame.AwayTeam;
        }

        private static List<int?> CreateListOfPlayoffGamesByGameType(
            IEnumerable<PlayoffBet> playoffBetsByGameTypeReference)
        {
            var playoffTeamsByGameTypeReference = new List<int?>();

            foreach (var playoffBetReference in playoffBetsByGameTypeReference)
            {
                if(playoffBetReference.HomeTeam != null)
                    playoffTeamsByGameTypeReference.Add(playoffBetReference.HomeTeam);
                if (playoffBetReference.AwayTeam != null)
                    playoffTeamsByGameTypeReference.Add(playoffBetReference.AwayTeam);
            }

            return playoffTeamsByGameTypeReference;
        }


        private static void AddNewUserScoresBasedOnUserBets(int sportsEventId, IEnumerable<UserBet> userBets,
            ICollection<UserScore> userScores)
        {
            foreach (var userBet in userBets)
            {
                if (UserScoreExists(userScores, userBet)) continue;

                userScores.Add(new UserScore
                {
                    UserId = userBet.UserId,
                    SportsEventId = sportsEventId,
                    Score = 0
                });
            }
        }

        private static bool UserScoreExists(IEnumerable<UserScore> userScores, UserBet userBet)
        {
            return userScores.Any(x => x.UserId == userBet.UserId);
        }
    }
}