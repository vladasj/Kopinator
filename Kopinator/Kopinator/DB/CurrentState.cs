using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util;
using Kopinator.Misc;

namespace Kopinator.DB
{
    static class CurrentState
    {
        private static List<string> _stateChangedList = new List<string>();

        private static double _previousPeriodTotal;
        public static double PreviousPeriodTotal
        {
            get
            {
                if (ReloadNeed(nameof(PreviousPeriodTotal)))
                {
                    _previousPeriodTotal = DataBase.GetIndexOnDate(DateTime.Now);
                    Reloaded(nameof(PreviousPeriodTotal));
                }
                return _previousPeriodTotal;
            }
        }
        
        private static double _thisMounthTotalIncome;
        public static double ThisMounthTotalIncome { get
            {
                if (ReloadNeed(nameof(ThisMounthTotalIncome)))
                {
                    _thisMounthTotalIncome = DataBase.BudgetSum(DateTime.Now.StartOfMonth(), DateTime.Now.EndOfMonth(),
                        RecordTypeEnum.Income);
                    Reloaded(nameof(ThisMounthTotalIncome));
                }
                return _thisMounthTotalIncome;
            }
        }
        public static double ThisMounthTotalRegularPay { get; private set; }
        public static double ThisMounthTotalPay { get; private set; }
        public static double ThisMounthPerDayAmmount => (PreviousPeriodTotal + ThisMounthTotalIncome - ThisMounthTotalRegularPay) / DateTime.Now.DaysInMonth();
        public static double ThisDayPay { get; private set; }
        public static double ThisDayAmmountLeft => ThisMounthPerDayAmmount - ThisDayPay;

        static CurrentState()
        {
            DataBase.StateChanged += DataBase_StateChanged;
        }

        private static void DataBase_StateChanged()
        {
            _stateChangedList.Clear();
        }

        private static bool ReloadNeed(string propertyName)
        {
            return _stateChangedList.Any(t => t == propertyName);
        }

        private static void Reloaded(string propertyName)
        {
            _stateChangedList.Add(propertyName);
        }
    }
}