﻿using System;
using System.Collections.Generic;
using System.Text;
using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Baskets.Domain.Repositories
{
    public interface IBasketsUnitOfWork : IUnitOfWork
    {
        IBasketsRepository Baskets { get; }

        IInventoryItemsRepository InventoryItems { get; }
    }
}
