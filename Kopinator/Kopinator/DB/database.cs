using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using Kopinator.Misc;

namespace Kopinator.DB
{
    public enum RecordTypeEnum { Income, Pay }

    public class BudgetRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public double Amount { get; set; }
        public RecordTypeEnum RecordType { get; set; }
        public bool RegularRecord { get; set; }

        public string Represent() => $"Id: {Id}, name: {Description}, date: {Date}, type {RecordType}";

        public static BudgetRecord GetNewRecord(double amount, RecordTypeEnum recordType, bool regularRecord = false, string description = "")
        {
            var budgetRecord = new BudgetRecord()
            {
                Date = DateTime.Now,
                IsActive = true,
                Amount = amount,
                RecordType = recordType,
                Description = description
            };
            return budgetRecord;
        }
    }

    public class Index
    {
        public int Id { get; set; }
        public DateTime Date;
        public double Total { get; set; }
        public RecordTypeEnum RecordType { get; set; }
    }

    public class RegularPayment
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public bool AsPercentageOfIncome { get; set; }
        public RecordTypeEnum RecordType { get; set; }
    }

    static class DataBase
    {
        private static readonly LiteDatabase Db;
        private static LiteCollection<BudgetRecord> BudgetCollection => Db.GetCollection<BudgetRecord>("Budget");
        private static LiteCollection<Index> IndexCollection => Db.GetCollection<Index>("Index");
        private static LiteCollection<RegularPayment> RegularPaymentCollection => Db.GetCollection<RegularPayment>("RegularPayments");

        static DataBase()
        {
            var personalPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            Db = new LiteDatabase(Path.Combine(personalPath, @"kopinator.db"));
        }

        private static bool UpdateIndex(double ammount, DateTime date)
        {
            date = date.AbsoluteStart();
            do
            {
                var date1 = date;
                var currentDateIndex = IndexCollection.Find(t => t.Date == date1).First();
                currentDateIndex.Total += ammount;
                IndexCollection.Insert(currentDateIndex.Id, currentDateIndex);
                date = date.AddDays(1);
            } while (date < DateTime.Now);
            OnStateChanged();
            return true;
        }
        public static double GetIndexOnDate(DateTime date)
        {
            date = date.AbsoluteStart();
            var indexRecord = IndexCollection.FindOne(t => t.Date == date);
            if (indexRecord != null) return indexRecord.Total;

            indexRecord = IndexCollection.Find(t => t.Date < date).OrderByDescending(t => t.Date).FirstOrDefault();
            return indexRecord?.Total ?? 0;
        }

        public static double BudgetSum(DateTime startDate, DateTime endDate, RecordTypeEnum recordType)
        {
            var budgetSum = BudgetCollection.Find(t => t.Date >= startDate &&
                                                             t.Date <= endDate &&
                                                             t.RecordType == recordType).Sum(t => t.Amount);
            return budgetSum;
        }

        public static double GetIncomeOfDay(DateTime date)
        {
            var startDate = date.AbsoluteStart();
            var endDate = date.AbsoluteEnd();
            return BudgetSum(startDate, endDate, RecordTypeEnum.Income);
        }

        public static double GetPayOfDay(DateTime date)
        {
            var startDate = date.AbsoluteStart();
            var endDate = date.AbsoluteEnd();
            return BudgetSum(startDate, endDate, RecordTypeEnum.Pay);
        }

        private static bool AddBudgetRecord(BudgetRecord budgetRecord)
        {
            BudgetCollection.Insert(budgetRecord);
            UpdateIndex(budgetRecord.RecordType == RecordTypeEnum.Income ? budgetRecord.Amount : -budgetRecord.Amount,
                            budgetRecord.Date);
            return true;
        }

        public static bool AddPay(double ammount, string description = "")
        {
            var newRecord = BudgetRecord.GetNewRecord(ammount, RecordTypeEnum.Pay, false, description);
            return AddBudgetRecord(newRecord);
        }

        public static bool AddIncome(double ammount, string description = "")
        {
            var newRecord = BudgetRecord.GetNewRecord(ammount, RecordTypeEnum.Income, false, description);
            return AddBudgetRecord(newRecord);
        }

        public delegate void StateChangedDelegate();

        public static event StateChangedDelegate StateChanged;

        private static void OnStateChanged()
        {
            StateChanged?.Invoke();
        }
    }

}