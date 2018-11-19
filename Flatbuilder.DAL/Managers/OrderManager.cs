﻿using Flatbuilder.DAL.Context;
using Flatbuilder.DAL.Entities;
using Flatbuilder.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Flatbuilder.DAL.Managers
{
    public class OrderManager : IOrderManager
    {
        private readonly FlatbuilderContext _context;

        public OrderManager(FlatbuilderContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.Costumer)
                .Include(o => o.OrderRooms)
                .ThenInclude(or => or.Room)
                .AsNoTracking()
                .ToListAsync();
            return orders;

            //var kitchens = _context.Rooms.OfType<Kitchen>().ToList();
            //var zuhayn = _context.Rooms.OfType<Shower>().ToList();
        }

        public async Task<List<Order>> GetOrdersByNameAsync(string name)
        {
            if (await _context.Costumers.FirstOrDefaultAsync(c => c.Name.Equals(name)) == null)
                return null;
            var orders = await _context.Orders
                .Include(o => o.Costumer)
                .Include(o => o.OrderRooms)
                .ThenInclude(or => or.Room)
                .AsNoTracking()
                .Where(o => o.Costumer.Name.Equals(name))
                .ToListAsync();
            return orders;
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);

            return order;
        }

        public async Task DeleteOrderAsync(Order o)
        {
            _context.Remove(o);

            await SaveChangesAsync();
        }

        public async Task<Order> AddOrderAsync(Order order, List<Room> rooms)
        {
            if(order.EndDate <= order.StartDate)
                throw new Exception("Invalid time interval");

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var freerooms = await _context.Rooms
                .Include(r => r.OrderRooms)
                .Where(r => (!_context.OrderRooms.Select(or => or.RoomId).Contains(r.Id)) //meg nem foglalt szobak
                   || (!(_context.OrderRooms  //szobak amik nincsenek zavaro foglalasok szobai kozt
                            .Where(or => (_context.Orders
                                .Where(o => (o.StartDate < order.EndDate && o.EndDate > order.StartDate)
                                    || (o.EndDate > order.StartDate && o.StartDate < order.EndDate))
                                .Select(o => o.Id))
                            .Contains(or.OrderId))
                            .Select(or => or.RoomId)).Contains(r.Id)))
                .AsNoTracking()
                .ToListAsync();

                if (freerooms == null)
                    return null;

                List<OrderRoom> newors = new List<OrderRoom>();

                for (int i = 0; i < rooms.Count; i++)
                {
                    foreach (var fr in freerooms)
                        if (rooms[i].GetType().Equals(fr.GetType()))
                        {
                            newors.Add(new OrderRoom { RoomId = fr.Id, Note = "megrendeles" });
                            freerooms.Remove(fr);
                            break;
                        }
                    if (newors.Count != (i + 1))
                        return null;
                }

                int costmerid = await _context.Costumers
                    .Where(c => c.Name == order.Costumer.Name)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();

                Order newo = new Order
                {
                    CostumerId = costmerid,
                    StartDate = order.StartDate,
                    EndDate = order.EndDate,
                    OrderRooms = newors
                };

                await _context.AddAsync(newo);

                await SaveChangesAsync();

                scope.Complete();

                return newo;
            }
        }

        public async Task InsertAsync(/*Order order*/)
        {
            var room = new Kitchen { Price = 400 };
            _context.Rooms.Add(room);

            _context.Add(new Order
            {
                Costumer = new Costumer { Name = "nevem" },
                StartDate = DateTime.Now.AddDays(-5),
                EndDate = DateTime.Now.AddDays(1),
                OrderRooms = new List<OrderRoom>
                {
                    new OrderRoom { Room = room, Note = "megrendeles" }
                }
            });

            await _context.SaveChangesAsync();
            //var foglalasi_szobak = order.Rooms.Select(r => r.Id).ToList();

            //var marFoglalva = await _context.Orders.AnyAsync(o => o.Rooms.Any(r => foglalasi_szobak.Contains(r.Id)));
            //var roomId = 7;
            //var szabad = _context.Rooms.Where(r => r.Id == roomId && r.OrderRooms.Any(or => or.Order.EndDate < DateTime.Now ))
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException e)
            {
                throw new Exception("Concurrency error");
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
