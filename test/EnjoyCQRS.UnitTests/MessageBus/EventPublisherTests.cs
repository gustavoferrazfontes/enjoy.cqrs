﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnjoyCQRS.Events;
using EnjoyCQRS.MessageBus;
using EnjoyCQRS.MessageBus.InProcess;
using EnjoyCQRS.UnitTests.MessageBus.Stubs;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnjoyCQRS.UnitTests.MessageBus
{
    public class EventPublisherTests
    {
        private const string CategoryName = "Unit";
        private const string CategoryValue = "Event publisher";

        [Then]
        [Trait(CategoryName, CategoryValue)]
        public async void When_a_single_event_is_published_to_the_bus_containing_a_single_EventHandler()
        {
            var testEvent = new TestEvent(Guid.NewGuid());

            var handler = new FirstTestEventHandler();

            var handlers = new List<Action<TestEvent>>
            {
                evt => handler.ExecuteAsync(evt)
            };

            var eventRouter = CreateEventRouter(handlers);

            EventPublisher eventPublisher = new EventPublisher(eventRouter);
            
            await eventPublisher.PublishAsync<IDomainEvent>(new[] { testEvent });
            await eventPublisher.CommitAsync();

            handler.Ids.First().Should().Be(testEvent.AggregateId);
        }

        [Then]
        [Trait(CategoryName, CategoryValue)]
        public async void When_a_single_event_is_published_to_the_bus_containing_multiple_EventHandlers()
        {
            var handler1 = new FirstTestEventHandler();
            var handler2 = new SecondTestEventHandler();

            var handlers = new List<Action<TestEvent>>
            {
                evt => handler1.ExecuteAsync(evt),
                evt => handler2.ExecuteAsync(evt)
            };

            var eventRouter = CreateEventRouter(handlers);

            EventPublisher eventPublisher = new EventPublisher(eventRouter);

            var testEvent = new TestEvent(Guid.NewGuid());
            
            await eventPublisher.PublishAsync<IDomainEvent>(new[] { testEvent });
            await eventPublisher.CommitAsync();

            handler1.Ids.First().Should().Be(testEvent.AggregateId);
            handler2.Ids.First().Should().Be(testEvent.AggregateId);
        }

        [Then]
        [Trait(CategoryName, CategoryValue)]
        public async void Events_should_be_published_on_correct_order()
        {
            var ids = new List<Tuple<Guid, int>>();

            var handler1 = new OrderTestEventHandler(ids);

            var handlers = new List<Action<OrderedTestEvent>>
            {
                evt => handler1.ExecuteAsync(evt)
            };

            var eventRouter = CreateEventRouter(handlers);

            EventPublisher eventPublisher = new EventPublisher(eventRouter);

            var orderTestEvent = new OrderedTestEvent(Guid.NewGuid(), 1);
            var orderTestEvent2 = new OrderedTestEvent(Guid.NewGuid(), 2);
            var orderTestEvent3 = new OrderedTestEvent(Guid.NewGuid(), 3);

            await eventPublisher.PublishAsync<IDomainEvent>(new[]
            {
                orderTestEvent,
                orderTestEvent2,
                orderTestEvent3
            });

            await eventPublisher.CommitAsync();

            ids[0].Item2.Should().Be(orderTestEvent.Order);

            ids[1].Item2.Should().Be(orderTestEvent2.Order);

            ids[2].Item2.Should().Be(orderTestEvent3.Order);
        }

        private IEventRouter CreateEventRouter<TEvent>(List<Action<TEvent>> handlers)
            where TEvent : IDomainEvent
        {
            Mock<IEventRouter> eventRouterMock = new Mock<IEventRouter>();
            eventRouterMock.Setup(e => e.RouteAsync(It.IsAny<TEvent>())).Callback((IDomainEvent @event) =>
            {
                handlers.ForEach((action =>
                {
                    action((TEvent)@event);
                }));

            }).Returns(Task.CompletedTask);

            return eventRouterMock.Object;
        }
    }
}