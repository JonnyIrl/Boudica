﻿using Boudica.Enums;
using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class HistoryService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<HistoryRecord> _historyRecordCollection;
        public HistoryService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            string name = typeof(HistoryRecord).Name + "Test";
#else
            string name = typeof(HistoryRecord).Name;
#endif
            _historyRecordCollection = _mongoDBContext.GetCollection<HistoryRecord>(name);
        }

        public async Task<HistoryRecord> InsertHistoryRecord(HistoryRecord record)
        {
            await _historyRecordCollection.InsertOneAsync(record);
            return record;
        }

        public async Task<HistoryRecord> InsertHistoryRecord(ulong userId, ulong? targetId, HistoryType historyType, int? amount = null)
        {
            return await InsertHistoryRecord(CreateHistoryRecord(userId, targetId, historyType, amount));
        }

        public HistoryRecord CreateHistoryRecord(ulong userId, ulong? targetId, HistoryType historyType, int? amount = null)
        {
            return new HistoryRecord()
            {
                UserId = userId,
                TargetUserId = targetId,
                HistoryType = historyType,
                DateTimeInserted = DateTime.UtcNow,
                Amount = amount
            };
        }

        public async Task<long> GetAwardedCountAsync(ulong userId)
        {
            return await _historyRecordCollection.CountDocumentsAsync(x => x.TargetUserId == userId && x.HistoryType == HistoryType.Award);
        }

        public async Task<List<HistoryRecord>> GetAllHistoryRecordsAsync(DateTime from, DateTime to)
        {
            return await _historyRecordCollection.Find(x => x.DateTimeInserted > from && x.DateTimeInserted < to).ToListAsync();
        }
    }
}
