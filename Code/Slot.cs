﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Represents one moment "in time". There can be exactly one action in each slot. 
    /// Slot has a list of positive conditions that are true in that moment. Negative conditions are simply conditions that are not present. 
    /// </summary>
    class Slot
    {
        /// <summary>
        /// List of conditions that are true in this slot. 
        /// </summary>
        public List<Term> Conditions { get; }

        public Slot()
        {
            Conditions = new List<Term>();
        }

        public Slot(List<Term> conds)
        {
            Conditions = conds;
        }

        /// <summary>
        /// Adds new conditions if they are not already in.
        /// </summary>
        /// <param name="conditions"></param>
        public void AddConditions(List<Term> conditions)
        {
            if (conditions?.Any() == true)
            {
                foreach (Term condition in conditions)
                {
                    if (!Conditions.Contains(condition)) Conditions.Add(condition);
                }
            }
        }

        /// <summary>
        /// Removes condition if the slot had them. 
        /// </summary>
        /// <param name="conditions"></param>
        public void RemoveConditions(List<Term> conditions)
        {
            if (conditions?.Any() == true)
            {
                foreach (Term condition in conditions)
                {
                    if (Conditions.Contains(condition)) Conditions.Remove(condition);
                }
            }
        }

        /// <summary>
        /// Adds conditions left in c1 after removing all conditions in c2 from them.
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        public void AddLeftOverConditions(List<Term> c1, List<Term> c2)
        {
            List<Term> c3;
            if (c1 != null) c3 = new List<Term>(c1);
            else c3 = new List<Term>();
            foreach (Term condition in c2)
            {
                if (c3.Contains(condition))
                {
                    c3.Remove(condition); //Thsi works. 
                }
            }
            AddConditions(c3);
        }

        /// <summary>
        /// returns true if this slot shares conditions with given list. 
        /// </summary>
        /// <param name="conditions"></param>
        /// <returns></returns>
        public bool SharesItems(List<Term> conditions)
        {
            foreach (Term condition in conditions)
            {
                if (Conditions.Contains(condition)) return true;
            }
            return false;
        }
        //Returns true if this slot contains all conditions.
        public bool SharesAllItems(List<Term> conditions)
        {
            foreach (Term condition in conditions)
            {
                if (!Conditions.Contains(condition))
                {
                    Console.WriteLine(" This conditions {0} is not present.", condition);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Describes this slot as a string. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = string.Join(",", Conditions.Select(x => x.Name));
            String s = "Slot: " + " conditions: " + text;
            return s;
        }
    }
}
