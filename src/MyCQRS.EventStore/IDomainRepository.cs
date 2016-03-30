﻿using System;

namespace MyCQRS.EventStore
{
    public interface IDomainRepository
    {
        TAggregate GetById<TAggregate>(Guid id) where TAggregate : class, IAggregate, new();
        void Add<TAggregate>(TAggregate aggregate) where TAggregate : class, IAggregate;
    }
}
