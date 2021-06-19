using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanValidation1
{
    /// <summary>
    /// This class contains and makes atom timelines. Each atom timeline contains actions which affect the atom. Positive action means it supports it; negative it deletes it.  
    /// Warning: Action indexes are calculated from 1 not 0. This is because 0 is for initial state. So action 0 in the real timeline here has index 1.
    /// </summary>
    class AtomTlnMaker
    {
        readonly Dictionary<String, List<int>> AtomTimelines;

        public AtomTlnMaker()
        {
            AtomTimelines = new Dictionary<string, List<int>>();
        }

        /// <summary>
        /// Returns key in form of a string from a Term. This is necsssary as two terms with same value would be considered different, because they are different objects. 
        /// </summary>
        /// <returns></returns>
        private string GetKey(Term t)
        {
            String s = t.Name;
            foreach (Constant v in t.Variables)
            {
                s = s + " " + v.Name;
            }
            return s;
        }

        /// <summary>
        /// Adds int i to the timeline of atom t. If i is positive it means this action supports the atom. If i is negative it deletes the atom. 
        /// If atom is not in the list yet it will add it. 
        /// </summary>
        /// <param name="t">Atom </param>
        /// <param name="i">index of supportive action</param>
        private void AddSupportIndex(Term t, int i)
        {
            String key = GetKey(t);
            if (AtomTimelines.ContainsKey(key))
            {
                AtomTimelines[key].Add(i);
            }
            else
            {
                AtomTimelines.Add(key, new List<int> { i });
            }
        }

        /// <summary>
        /// Adds int i to the timeline of all terms. If i is positive it means this action supports the atom. If i is negative it deletes the atom. 
        /// If atom is not in the list yet it will add it. 
        /// </summary>
        /// <param name="terms"></param>
        /// <param name="i"></param>
        public void AddSupportIndex(List<Term> terms, int i)
        {
            foreach (Term t in terms)
            {
                AddSupportIndex(t, i);
            }
        }

        /// <summary>
        /// Returns dictionary of terms and timelines for atoms needed in the given list of conditions. 
        /// Retruns false if isPositive is true and there is a condition which references an atom that does not appear in the atomtimeline. 
        /// For negative preconditions this is always okay as atom not appearing in the atom timeline just means the negative condition is valid.
        /// </summary>
        /// <param name="conditions"></param>
        /// <param name="check"></param>
        /// <param name="isPositive">Determines whether the conditions are positive. If so this is used to give an error if they don't appear in the atom timeline. </param>
        /// <returns></returns>
        private bool GetCheckTimelines(List<Term> conditions, out Dictionary<Term, List<int>> check, bool isPositive)
        {
            check = new Dictionary<Term, List<int>>();
            foreach (Term t in conditions)
            {
                string key = GetKey(t);
                if (AtomTimelines.ContainsKey(key))
                {
                    if (isPositive)
                    {
                        check.Add(t, AtomTimelines[key]);
                    }
                    else
                    {
                        check.Add(t, GetNegativeTln(AtomTimelines[key]));
                    }
                }
                else
                {
                    if (isPositive)
                    {
                        //Console.WriteLine("This positive condition {0} does not exist in atom timeline yet. So no one supports it. This action/task is invalid and must be deleted.", t.ToString());
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Returns timeline of a specific atom.
        /// If atom is negative returns its invert. 
        /// </summary>
        /// <param name="atom"></param>
        /// <param name="positive">whether atom is positive or negative</param>
        /// <returns></returns>
        public List<int> GetAtomTimeline(Term atom, bool positive)
        {
            // if it doesn|t exist it return null 
            String key = GetKey(atom);
            if (AtomTimelines.ContainsKey(key))
            {
                return positive ? AtomTimelines[key] : GetNegativeTln(AtomTimelines[key]);
            }
            else
            {
                //Console.WriteLine("Warning: There is no timeline for atom {0}", atom);
            }
            return null;
        }

        /// <summary>
        /// Inverts the atom timeline and adds 0 if 0 is not in the timeline. 
        /// </summary>
        /// <param name="atomTimeline"></param>
        /// <returns></returns>
        private List<int> GetNegativeTln(List<int> atomTimeline)
        {
            List<int> newTimeline = new List<int>();
            if (!atomTimeline.Contains(0)) newTimeline.Add(0);
            //if atom timeline contains 0 then this negative support will not have 0 in it and just like with positive conditions if it doesnt have 0 then nothing supports it. 
            foreach (int i in atomTimeline)
            {
                if (i != 0)
                {
                    newTimeline.Add(-i);
                }
            }
            return newTimeline;
        }

        /// <summary>
        /// Creates supports and delete action vector. It will automatically add them to action a.         /// 
        /// Returns true if action is valid. 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="i">Index of given action</param>
        public bool CreateSupportsAndDeleteActions(ref Action a, int i)
        {
            Dictionary<Term, List<int>> check = new Dictionary<Term, List<int>>();
            List<Term> removeTerms = new List<Term>();
            if (!GetCheckTimelines(a.PosPreConditions, out check, true))
            {
                //TODO delete action maybe??
            }
            Dictionary<Term, List<int>> check2 = new Dictionary<Term, List<int>>();
            GetCheckTimelines(a.NegPreConditions, out check2, false);
            for (int j = i - 1; j > 0; j--)
            {
                foreach (KeyValuePair<Term, List<int>> atomTimeline in check)
                {
                    if (atomTimeline.Value.Contains(-j))
                    {
                        if (!a.DeleteActions.Contains(j)) a.DeleteActions.Add(j);
                    }
                    else if (atomTimeline.Value.Contains(j))
                    {
                        Tuple<Term, List<int>, int> support = new Tuple<Term, List<int>, int>(atomTimeline.Key, atomTimeline.Value, j);
                        a.Supports.Add(support);
                        removeTerms.Add(atomTimeline.Key);
                    }
                }
                foreach (Term t in removeTerms)
                {
                    check.Remove(t);
                }
                removeTerms = new List<Term>();
            }
            foreach (KeyValuePair<Term, List<int>> tuple in check)
            {
                if (!tuple.Value.Contains(0))
                {
                    //Console.WriteLine("This action {0}has precondition {1} that no one supports. This action is invalid and must be deleted.", a.ActionInstance,tuple.Key);
                    return false;
                }
            }
            return true;
        }
    }
}
