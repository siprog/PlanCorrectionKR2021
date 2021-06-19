using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Represents a partial step between creating a task from a rule.
    /// Has all main variables of rule and subtasks filled. Creates proper conditions (with actual variables not number references).
    /// </summary>
    class RuleInstance
    {
        /// <summary>
        /// Describes the main task of this ruleinstance (The task that this rule decomposes)
        /// </summary>
        public Task MainTask;
        /// <summary>
        /// List of all subtasks for this rule. 
        /// </summary>
        public List<Task> Subtasks;

        /// <summary>
        /// Term is the condition and the int says to which subtask is this related to. (Counted from 0)
        /// </summary>
        public List<Tuple<int, Term>> PosPreConditions { get; }
        public List<Tuple<int, Term>> NegPreConditions { get; }
        public List<Tuple<int, int, Term>> PosBetweenConditions { get; }
        public List<Tuple<int, int, Term>> NegBetweenConditions { get; }
        /// <summary>
        /// True if the creation if this rule instance is valid. All conditions were created properly. 
        /// </summary>
        readonly bool isValid;

        /// <summary>
        /// Indexes of actions that must be deleted in order for this task to be valid
        /// Warning: Action indexes are calculated from 1 not 0. Explanation in AtomTlnMaker. 
        /// </summary>
        public List<int> DeleteActions;

        /// <summary>
        /// This is a union of all supports of subtasks. Any support that is already supported by an action that this rule decomposes to is removed from the union. \
        /// We will not need to check that one as it is already valid.         /// 
        /// </summary>
        public List<Tuple<Term, List<int>, int>> CheckSupports;

        /// <summary>
        /// The index of the first subtasks that this rule belongs to. 
        /// </summary>
        public double StartIndex;

        public RuleInstance(Task mainTask, List<Task> subtasks, Rule rule, List<String> allVars, List<Constant> allconstants)
        {
            this.MainTask = mainTask;
            this.Subtasks = subtasks;
            StartIndex= FindMinIndex(subtasks);
            isValid = CheckOrdering(rule.OrderConditions);
            PosPreConditions = new List<Tuple<int, Term>>();
            NegPreConditions = new List<Tuple<int, Term>>();
            PosBetweenConditions = new List<Tuple<int, int, Term>>();
            NegBetweenConditions = new List<Tuple<int, int, Term>>();
            //These conditions below dont actually check vaidity, they just create the proper term conditions. 
            //These wont work for empty tasks when it comes to start index.
            if (isValid) isValid = CreateConditions(rule.PosBetweenConditions, PosBetweenConditions, allVars,rule.AllVarsTypes); //These go first as they are most likely to break the rule instance
            if (isValid) isValid = CreateConditions(rule.NegBetweenConditions, PosBetweenConditions, allVars,rule.AllVarsTypes); //TODO do between conditions with forall. 
            if (isValid) isValid = CreateConditions(rule.PosPreConditions, PosPreConditions, allVars, rule.AllVarsTypes, true, allconstants);
            if (isValid) isValid = CreateConditions(rule.NegPreConditions, NegPreConditions, allVars, rule.AllVarsTypes, false, allconstants);
            CreateDeleteActions(subtasks);
            //Must be done later after we know the action vector.            
            //CreateSupports(subtasks);

        }

        /// <summary>
        /// Finds the the starting position of this task. 
        /// </summary>
        /// <param name="subtasks"></param>
        /// <returns></returns>
        private double FindMinIndex(List<Task> subtasks)
        {
            double curMin = -1;
            foreach (Task t in subtasks)
            {
                double eI = t.GetStartIndex();
                if (eI < curMin || curMin == -1) curMin = eI;
            }
            return curMin;
        }

        /// <summary>
        /// This is an union of all supports of subtasks. Also a support that is already supported by an action that this task is decomposed to is removed from check supports as sit does not need ot be checked again. 
        /// </summary>
        /// <param name="subtasks"></param>
        public void CreateSupports(List<Task> subtasks,bool[] mainTaskVector)
        {
            CheckSupports = new List<Tuple<Term, List<int>, int>>();
            foreach(Task t in subtasks)
            {
                foreach(Tuple<Term, List<int>, int> supportTuple in t.Supports)
                {
                    //If this support timeline does not have action that this task decomposes to as it's action support, it must be aded to checksupports.
                    if (!mainTaskVector[supportTuple.Item3-1])
                    {
                        CheckSupports.Add(supportTuple);
                    }
                }
            }
        }

        /// <summary>
        /// If domain model is totally ordered, then the task (after action deletion) must be decomposed into a continuous sequence of actions. 
        /// So if there is any action between the tasks actiosn it must be removed.  
        /// </summary>
        internal void DeleteActionPerTO(bool[] mainTaskVector,double min, double max)
        {
            //We already know the index of first and last action of this action vector. 
            for(int i=(int)min;i<max;i++)
            {
                if (!mainTaskVector[i])
                {
                    this.DeleteActions.Add(i + 1); // Because delete action indexes start from 1 not 0. 
                } 
               
            }
        }

        /// <summary>
        /// Populates field delete actions of this rule instance. 
        /// Combines all delete actions of this rules subtasks. 
        /// </summary>
        /// <param name="subtasks"></param>
        public void CreateDeleteActions(List<Task> subtasks)
        {
            DeleteActions = new List<int>();
            foreach(Task t in subtasks)
            {
                DeleteActions.AddRange(t.DeleteActions);
            }
            DeleteActions=DeleteActions.Distinct().ToList();            
        }       

        public bool IsValid()
        {
            return isValid;
        }

        /// <summary>
        /// Checks whether subtasks are properly ordered. 
        /// </summary>
        /// <param name="orderConditions"></param>
        /// <returns></returns>
        private bool CheckOrdering(List<Tuple<int, int>> orderConditions)
        {
            if (orderConditions?.Any() != true) return true; //If there is no ordering than it's ordered properly.
            foreach (Tuple<int, int> combo in orderConditions)
            {
                Task subtask1 = Subtasks[combo.Item1];
                Task subtask2 = Subtasks[combo.Item2];
                if (!(subtask1.GetEndIndex() < subtask2.GetStartIndex())) return false;
            }
            return true;
        }

        private bool CreateConditions(List<Tuple<int, string, List<int>>> PostConditions1, List<Tuple<int, Term>> PostConditions2, List<String> allVars, List<ConstantType> allVarsType, bool pos, List<Constant> allconstants)
        {
            bool valid = true;
            bool containsForallCondition = false;
            foreach (Tuple<int, string, List<int>> conditionTuple in PostConditions1)
            {
                Constant[] newVars = new Constant[conditionTuple.Item3.Count];
                for (int i = 0; i < conditionTuple.Item3.Count; i++)
                {
                    int num = conditionTuple.Item3[i];
                    if (num < 0 || num >= allVars.Count) return false; //change to >=
                    newVars[i] = new Constant(allVars[num], allVarsType[num]);
                    if (allVars[num].StartsWith("!"))
                    {
                        //This is an forall variable. 
                        //So I must create many instances of this condition. With each constant of desired type. 
                        containsForallCondition = true;
                    }
                }
                Term condition = new Term(conditionTuple.Item2, newVars);
                if (conditionTuple.Item2.Contains("equal") || conditionTuple.Item2.Equals("=")) valid = CheckEquality(pos, condition);
                else
                { //We do not add equality conditions to normal conditions. 
                    if (containsForallCondition)
                    {
                        PostConditions2.AddRange(CreateForAllConditions(newVars, allconstants, allVarsType, (int)StartIndex, conditionTuple.Item2));
                    }
                    else
                    {
                        Tuple<int, Term> tuple = new Tuple<int, Term>((int)StartIndex, condition);
                        PostConditions2.Add(tuple);
                    }
                }
                containsForallCondition = false;
            }
            return true;
        }

        private List<Tuple<int, Term>> CreateForAllConditions(Constant[] newVars, List<Constant> allconstants, List<ConstantType> allVarsType, int subtaskNum, string name)
        {
            List<Tuple<int, Term>> solution = new List<Tuple<int, Term>>();
            for (int i = 0; i < newVars.Length; i++)
            {
                if (newVars[i].Name.StartsWith("!"))
                {
                    List<Constant> rightTypeConstants = allconstants.Where(x => newVars[i].Type.IsAncestorTo(x.Type)).ToList();
                    foreach (Constant c in rightTypeConstants)
                    {
                        c.Name = c.Name.Replace("!", "");
                        newVars[i] = c;
                        Term condition = new Term(name, newVars.ToArray());
                        Tuple<int, Term> tuple = new Tuple<int, Term>(subtaskNum, condition);
                        solution.Add(tuple);
                    }
                }
            }
            return solution;
        }

        //Handles logical equality conditions. 
        private bool CheckEquality(bool pos, Term condition)
        {
            int i = 0;
            foreach (Constant var in condition.Variables)
            {
                if (pos)
                {
                    foreach (Constant var2 in condition.Variables)
                    {
                        if (!var.Equals(var2)) return false;
                    }
                }
                else
                {
                    for (int j = 0; j < condition.Variables.Length; j++)
                    {
                        if (j != i)
                        {
                            if (var == condition.Variables[j]) return false;
                        }
                    }
                }
                i++;
            }
            return true;
        }

        private bool CreateConditions(List<Tuple<int, int, string, List<int>>> BetweenConditions1, List<Tuple<int, int, Term>> BetweenConditions2, List<String> allVars,List<ConstantType> allVarsType)
        {
            foreach (Tuple<int, int, string, List<int>> conditionTuple in BetweenConditions1)
            {
                Task task1 = Subtasks[conditionTuple.Item1];
                Task task2 = Subtasks[conditionTuple.Item2];
                Constant[] newVars = new Constant[conditionTuple.Item4.Count];
                for (int i = 0; i < conditionTuple.Item4.Count; i++)
                {
                    int num = conditionTuple.Item4[i];
                    if (num < 0 || num >= allVars.Count) return false;
                    newVars[i] = new Constant(allVars[num], allVarsType[num]);
                }
                Term condition = new Term(conditionTuple.Item3, newVars);
                Tuple<int, int, Term> tuple = new Tuple<int, int, Term>(conditionTuple.Item1, conditionTuple.Item2, condition);
                BetweenConditions2.Add(tuple);
            }
            return true;
        }

        public override string ToString()
        {
            string text = string.Join(",", Subtasks.Select(x => x.TaskInstance.Name));
            string text2 = string.Join(",", PosPreConditions.Select(x => x.Item2.Name));
            string text3 = string.Join(",", NegPreConditions.Select(x => x.Item2.Name));
            string text6 = string.Join(",", PosBetweenConditions.Select(x => x.Item3.Name));
            string text7 = string.Join(",", NegBetweenConditions.Select(x => x.Item3.Name));
            return "RuleInstance: " + this.MainTask.TaskInstance.Name + " subtasks " + text + " posPreCond" + text2 + "negPreCond " + text3 + "posPostCond " + "posBetweenCond " + text6 + "negBetweenCond" + text7;
        }
    }
}
