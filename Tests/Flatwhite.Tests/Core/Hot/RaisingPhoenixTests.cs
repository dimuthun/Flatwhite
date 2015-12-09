﻿using System;
using System.Threading;
using Flatwhite.Hot;
using NSubstitute;
using NUnit.Framework;

namespace Flatwhite.Tests.Core.Hot
{
    [TestFixture]
    public class RaisingPhoenixTests
    {
        [Test]
        public void Reborn_should_execute_the_task_once()
        {
            var hitCount = 2000;
            var wait = new AutoResetEvent(false);
            var count = 0;
            Func<IPhoenixState> action = () =>
            {
                count ++;
                wait.Set();
                return Substitute.For<IPhoenixState>();
            };

            var state = new RaisingPhoenix();
            for (var i = 0; i < hitCount; i++)
            {
                state.Reborn(action);
            }
            Assert.IsTrue(wait.WaitOne(1000));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void Reborn_should_return_same_instance_if_there_is_exception()
        {
            Func<IPhoenixState> action = () =>
            {
                throw new Exception();
            };
            var state = new RaisingPhoenix();
            for (var i = 0; i < 10000; i++)
            {
                Assert.AreSame(state, state.Reborn(action));
            }
        }

        [Test]
        public void Reborn_should_not_call_reborn_on_result_if_task_completed_and_just_return_the_returned_phoenix()
        {
            var reborn = Substitute.For<IPhoenixState>();
            Func<IPhoenixState> action = () => reborn;
            var state = new RaisingPhoenix();
            IPhoenixState rebornState;
            do
            {
                rebornState = state.Reborn(action);
            } while (object.ReferenceEquals(rebornState, state));

            reborn.Received(0).Reborn(Arg.Any<Func<IPhoenixState>>());

        }
    }
}
