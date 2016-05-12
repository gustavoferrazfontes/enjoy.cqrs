﻿using System;
using System.Collections.Generic;
using System.Linq;
using EnjoyCQRS.Bus;
using EnjoyCQRS.Bus.InProcess;
using EnjoyCQRS.Events;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnjoyCQRS.UnitTests.MessageBus
{
    public class EventRouterTests
    {
        private const string CategoryName = "Unit";
        private const string CategoryValue = "Event router";

        [Fact]
        [Trait(CategoryName, CategoryValue)]
        public void When_a_single_event_is_published_to_the_bus_containing_a_single_EventHandler()
        {
            var handler = new FirstTestEventHandler();

            var handlers = new List<Action<TestEvent>>
            {
                evt => handler.Execute(evt)
            };

            Mock<IEventRouter> eventRouterMock = new Mock<IEventRouter>();
            eventRouterMock.Setup(e => e.Route(It.IsAny<TestEvent>())).Callback((IDomainEvent @event) =>
            {
                handlers.ForEach((action =>
                {
                    action((TestEvent)@event);
                }));
            });

            var testEvent = new TestEvent(Guid.NewGuid());
            
            EventBus directMessageBus = new EventBus(eventRouterMock.Object);

            directMessageBus.Publish<IDomainEvent>(new[] { testEvent });
            directMessageBus.Commit();

            handler.Ids.First().Should().Be(testEvent.AggregateId);
        }

        [Fact]
        [Trait(CategoryName, CategoryValue)]
        public void When_a_single_event_is_published_to_the_bus_containing_multiple_EventHandlers()
        {
            var handler1 = new FirstTestEventHandler();
            var handler2 = new SecondTestEventHandler();

            var handlers = new List<Action<TestEvent>>
            {
                evt => handler1.Execute(evt),
                evt => handler2.Execute(evt)
            };

            Mock<IEventRouter> eventRouterMock = new Mock<IEventRouter>();
            eventRouterMock.Setup(e => e.Route(It.IsAny<TestEvent>())).Callback((IDomainEvent @event) =>
            {
                handlers.ForEach((action =>
                {
                    action((TestEvent)@event);
                }));
            });

            EventBus directMessageBus = new EventBus(eventRouterMock.Object);

            var testEvent = new TestEvent(Guid.NewGuid());

            directMessageBus.Publish<IDomainEvent>(new[] { testEvent });
            directMessageBus.Commit();

            handler1.Ids.First().Should().Be(testEvent.AggregateId);
            handler2.Ids.First().Should().Be(testEvent.AggregateId);
        }

        public class FirstTestEventHandler : IEventHandler<TestEvent>
        {
            public List<Guid> Ids { get; } = new List<Guid>();

            public void Execute(TestEvent @event)
            {
                Ids.Add(@event.AggregateId);
            }
        }

        public class SecondTestEventHandler : IEventHandler<TestEvent>
        {
            public List<Guid> Ids { get; } = new List<Guid>();

            public void Execute(TestEvent @event)
            {
                Ids.Add(@event.AggregateId);
            }
        }

        public class TestEvent : DomainEvent
        {
            public TestEvent(Guid aggregateId) : base(aggregateId)
            {
            }
        }
    }
}