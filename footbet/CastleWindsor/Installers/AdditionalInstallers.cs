﻿using System.Web.Script.Serialization;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Footbet.Caching;
using Footbet.ScoreCalculations;

namespace Footbet.CastleWindsor.Installers
{
    public class AdditionalInstallers : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container
                .Register(Component.For<JavaScriptSerializer>().ImplementedBy<JavaScriptSerializer>())
                .Register(Component.For<IGameScoreCalculator>().ImplementedBy<GameScoreCalculator>())
                .Register(Component.For<IBonusScoreCalculator>().ImplementedBy<BonusScoreCalculator>())
                .Register(Component.For<ITopScorerScoreCalculator>().ImplementedBy<TopScorerScoreCalculator>())
                .Register(Component.For<ICacheService>().ImplementedBy<InMemoryCache>());
        }
    }
}