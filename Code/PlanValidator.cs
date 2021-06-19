using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Validates the plan. 
    /// </summary>
    class PlanValidator
    {       
        /// <summary>
        /// Retruns true/false depending on whether the plan is valid. Also outputs the longest valid plan on the console if plan is invalid. 
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="allTaskTypes"></param>
        /// <param name="initialConditions"></param>
        /// <param name="allConstants"></param>
        /// <param name="allConstantTypes"></param>
        /// <param name="emptyRules"></param>
        /// <returns></returns>
        public bool IsPlanValid(List<Action> plan, List<TaskType> allTaskTypes, List<Term> initialConditions, List<Constant> allConstants, List<Rule> emptyRules)
        {
            int iteration = 0;
            AtomTlnMaker atomTimelines = new AtomTlnMaker();
            atomTimelines.AddSupportIndex(initialConditions, 0); //Adds initial state to atom timelines. 
            Task longest = null;
            double longestLength = 0;
            for (int i = 1; i <= plan.Count; i++)
            {
                atomTimelines.AddSupportIndex(plan[i - 1].NegEffects, -i); //Adds that this action deletes given atoms in atom timelines. 
                atomTimelines.AddSupportIndex(plan[i - 1].PosEffects, i); //Adds that this action supports given atoms in atom timelines. 
            }
            List<Task> newTasks = new List<Task>();
            List<Task> allTasks = new List<Task>();

            ///Transforms action into tasks. 
            for (int i = 0; i < plan.Count; i++)
            {
                Action a = plan[i];
                TaskType taskType = FindTaskType(a, allTaskTypes);
                bool[] array = new bool[plan.Count];
                array[i] = true;
                bool valid = atomTimelines.CreateSupportsAndDeleteActions(ref a, i + 1);
                if (!valid)
                {
                    //Optional: Could be removed form plan entirely. 
                }
                Task t = new Task(a.ActionInstance, array, taskType, i, i, a.DeleteActions, a.Supports);
                t.Iteration = -1;
                t.TaskType.SetMinTaskLengthIfSmaller(1);
                t.TaskType.AddInstance(t);
                newTasks.Add(t);
            }

            //Main loop where we find applicable rules from newTasks and then use them to create new set of tasks.  
            while (newTasks?.Any() == true)
            {
                newTasks = newTasks.Distinct().ToList();
                List<Rule> applicableRules = GetApplicableRules(newTasks, iteration - 1);
                applicableRules = applicableRules.Distinct().ToList();
                applicableRules = applicableRules.Except(emptyRules).ToList(); //We have already created basic empty rules. We dont want to create them again. 
                allTasks.AddRange(newTasks);
                newTasks = new List<Task>();

                foreach (Rule r in applicableRules)
                {
                    List<RuleInstance> ruleInstances = r.GetRuleInstances(plan.Count, allConstants, iteration - 1, plan.Count + 1);
                    foreach (RuleInstance ruleInstance in ruleInstances)
                    {
                        List<Task> subtasks = new List<Task>();
                        Term mainTaskName = ruleInstance.MainTask.TaskInstance;
                        bool validNewTask = true;
                        double min=-1;
                        double max=-1;
                        validNewTask = CreateAndCheckActionVector(ruleInstance, plan.Count, out bool[] mainTaskVector,ref min, ref max);
                        if (validNewTask)
                        {                           
                            ruleInstance.CreateSupports(subtasks, mainTaskVector);
                            List<Tuple<Term, List<int>, int>> check = new List<Tuple<Term, List<int>, int>>();
                            check = ruleInstance.CheckSupports;
                            if (!CheckPreconditions(ruleInstance, mainTaskVector, atomTimelines, ref check)) validNewTask = false;
                            if (!CheckBetweenConditions(ruleInstance, mainTaskVector, atomTimelines, ref check)) validNewTask = false;
                            List<Tuple<Term, List<int>, int>> supports = CreateSupports(ruleInstance, mainTaskVector, ref check, plan.Count);
                            if (!CompareDeleteAndRequiredActions(ruleInstance, mainTaskVector)) validNewTask = false;
                            if (!CheckNullState(ref check)) validNewTask = false;
                            if (validNewTask && check.Count == 0)
                            {
                                Task t = new Task(mainTaskName, mainTaskVector, ruleInstance.MainTask.TaskType, min, max, ruleInstance.DeleteActions, supports);
                                t.Iteration = iteration;
                                List<Task> everytask = new List<Task>(allTasks);
                                everytask.AddRange(newTasks);
                                if (CheckNewness(everytask, t)) //at current iteration we should never do same task twice because of the way rulevariant are made.
                                {
                                    newTasks.Add(t);
                                    if ((longest == null || longestLength <= t.size()) && CheckSupportsRootTask(t))
                                    {
                                        longest = t;
                                        longestLength = t.size();
                                    }
                                    if (IsGoalTask(t))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                iteration++;
            }
            if (longest == null) Console.WriteLine("Plan is invalid. No task decomposes to even a single action.");
            else { Console.WriteLine("Plan is invalid. The task that decomposes to most actions is {0} and in order to be valid actions number: {1} must be deleted ", longest, longest.UnusedActions()); }
            return false;
        }

        /// <summary>
        /// In order for a task to be root task it cannot have any supports outside its list of actions. So each of these supports must be substituted 
        /// by either an action in actionvector that supports it or initial state. 
        /// Alg. 3:
        /// forall(tln,k)∈supdo
        ///    forj=k−1 downto 1 do
        ///        if j∈tln∧j∈idx then continue with next timeline.
        ///        if(−j)∈tln∧j∈idx then return False 
        ///    end for
        ///    if0/∈tln then return False
        /// end forall          
        /// return True
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private bool CheckSupportsRootTask(Task t)
        {
            bool solved = false;
            if (t.Supports == null) return true;
            //Alg3: forall(tln,k)∈supdo
            foreach ( Tuple<Term,List<int>,int> support in t.Supports)
            {
                //Alg3: forj=k−1downto1do
                int j = support.Item3-1;
                while(!solved && j>0)
                {
                    if (support.Item2.Contains(j) && t.GetActionVector()[j-1])
                    {
                        //This support is contained within my actions. This is okay. We can go to the next support.
                        solved = true;

                    } else if (support.Item2.Contains(-j) && t.GetActionVector()[j - 1])
                    {
                        return false;
                    }
                    j--;
                }
                if (!solved && !support.Item2.Contains(0))
                {
                    return false;
                }                
                solved = false;
            }
            return true;
        }

        /// <summary>
        /// Creates action vector for a task and checks that no two subtask decompose into the same action. 
        /// </summary>
        /// <param name="ruleInstance"></param>
        /// <param name="planLength"></param>
        /// <param name="mainTaskVector"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private bool CreateAndCheckActionVector(RuleInstance ruleInstance, int planLength,out bool[] mainTaskVector,ref double min, ref double max)
        {
            List<Task> subtasks = ruleInstance.Subtasks;
            min = ruleInstance.StartIndex; //Is -1 if subtasks are empty
            max = FindMaxIndex(subtasks); //Returns -1 if subtasks are empty
            mainTaskVector = new bool[planLength];

            for (int i = (int)Math.Round(min); i <= (int)Math.Round(max); i++)
            {
                int sum = 0;
                foreach (Task t in subtasks)
                {
                    //Empty tasks have null everywhere so they are fine. 
                    sum += Convert.ToInt32(t.GetActionVector()[i]);
                }
                if (sum > 1) return false;
                mainTaskVector[i] = (sum == 1);
            }
            return true;
        }

        //Checks this check <- { (tln,i) | (tln,i)∈check s.t. 0∈tln } % initial state does not provide condition 
        private bool CheckNullState(ref List<Tuple<Term, List<int>, int>> check)
        {
            List<Tuple<Term, List<int>, int>> removeTerms= new List<Tuple<Term, List<int>, int>>();
            foreach (Tuple<Term, List<int>, int> condition in check)
            {
                if (condition.Item2.Contains(0)) removeTerms.Add(condition);
            }
                check = check.Except(removeTerms).ToList();
            if (check.Count == 0) {
                //This means that remove Terms is the same as check and so it produces null. 
                check = new List<Tuple<Term, List<int>, int>>();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks that delete and required actions do not intersect. 
        /// </summary>
        /// <param name="ruleInstance"></param>
        /// <param name="mainTaskVector"></param>
        /// <returns></returns>
        private bool CompareDeleteAndRequiredActions(RuleInstance ruleInstance,bool[] mainTaskVector)
        {
            foreach(int i in ruleInstance.DeleteActions)
            {
                //Action that should be deleted is required for this task.
                if (mainTaskVector[i-1])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Creates support for a task. 
        /// This already makes sure that if a an action (needed as support) is deleted it will find a new support for that condition. 
        /// </summary>
        /// <param name="ruleInstance"></param>
        /// <param name="mainTaskVector"></param>
        /// <param name="check"></param>
        /// <param name="planLength"></param>
        /// <returns></returns>
        private List<Tuple<Term, List<int>, int>> CreateSupports(RuleInstance ruleInstance, bool[] mainTaskVector,ref List<Tuple<Term, List<int>, int>> check,  int planLength)
        {
            List<Tuple<Term, List<int>, int>> supports = new List<Tuple<Term, List<int>, int>>();
            for (int j=planLength;j>0;j--)
            {
                if (!ruleInstance.DeleteActions.Contains(j))
                {
                    List<Tuple<Term, List<int>, int>> removeTerms = new List<Tuple<Term, List<int>, int>>();
                    foreach (Tuple<Term, List<int>, int> checkSupport in check)
                    {
                        if (j<= checkSupport.Item3)
                        {
                            //if we got here it means that i was deleted otherwise this term wold not still be in check
                            if (j< checkSupport.Item3 && checkSupport.Item2.Contains(-j))
                            {
                                ruleInstance.DeleteActions.Add(j);
                            }
                        else if (checkSupport.Item2.Contains(j))
                        {
                            if (!mainTaskVector[j - 1]) //this if means that this task des not decompose to action j
                                supports.Add(new Tuple<Term, List<int>, int>(checkSupport.Item1, checkSupport.Item2, j));
                                removeTerms.Add(checkSupport);
                        }
                        }
                    }
                    check = check.Except(removeTerms).ToList();
                }
            }
            return supports;
        }

        /// <summary>
        /// Returns true if this task is new. 
        /// </summary>
        /// <param name="newTasks"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private bool CheckNewness(List<Task> newTasks, Task t)
        {
            List<Task> sameNameTasks = newTasks.Where(x => x.TaskInstance.Equals(t.TaskInstance)).ToList();
            foreach (Task t1 in sameNameTasks)
            {
                if (t1.GetActionVector().SequenceEqual(t.GetActionVector()) && t1.GetStartIndex() == t.GetStartIndex() && t1.GetEndIndex() == t.GetEndIndex())
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks between conditions. 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="timeline"></param>
        /// <param name="mainTaskVector"></param>
        /// <returns></returns>
        private bool CheckBetweenConditions(RuleInstance r, bool[] mainTaskVector, AtomTlnMaker atomTimelines, ref List<Tuple<Term, List<int>, int>> check)
        {
            if (!CheckBetweenConditions(r.PosBetweenConditions, true,r, mainTaskVector, atomTimelines, ref check)) return false;
            return CheckBetweenConditions(r.NegBetweenConditions, false,r, mainTaskVector, atomTimelines, ref check);
        }

        private bool CheckBetweenConditions(List<Tuple<int, int, Term>> betweenConditions, bool positive, RuleInstance r, bool[] mainTaskVector, AtomTlnMaker atomTimelines, ref List<Tuple<Term, List<int>, int>> check)
        {
            foreach (Tuple<int, int, Term> tuple in betweenConditions)
            {
                List<int> oneAtomTln = atomTimelines.GetAtomTimeline(tuple.Item3,positive);
                bool solved = false;
                //Why this is like this is explained in preconditions. 
                for (int j = tuple.Item2 + 1+1; j > 0; j--)
                {
                    if (oneAtomTln.Contains(-j))
                    {
                        if (!r.DeleteActions.Contains(j)) r.DeleteActions.Add(j);
                    }
                    //this action provides the condition
                    else if (oneAtomTln.Contains(j) && j <= tuple.Item1)
                    {
                        // if j is already an action that this task decomposes too then this doesnt need to be added to chec supports because this action wil alwazs be available and support the atoom. 
                        if (!mainTaskVector[j - 1]) check.Add(new Tuple<Term, List<int>, int>(tuple.Item3, oneAtomTln, j));
                        solved = true;
                        break; //jumps out of for cycle
                    }
                }
                if (!solved && !oneAtomTln.Contains(0))
                {
                    return false; //This condition can never be satisfied for this task. So we can stop trying to make it. 
                }
            }
            return true;
        }

            /// <summary>
            /// Checks precondnitions of a rule using supports and delete vectors. 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="timeline"></param>
            /// <param name="mainTaskVector"></param>
            /// <returns></returns>
            private bool CheckPreconditions(RuleInstance r, bool[] mainTaskVector, AtomTlnMaker atomTimeline, ref List<Tuple<Term, List<int>, int>> check)
        {
            if (!CheckPreconditions(r, r.PosPreConditions, true, mainTaskVector, atomTimeline, ref check)) return false;
            return CheckPreconditions(r, r.NegPreConditions, false, mainTaskVector, atomTimeline, ref check);
        }

        /// <summary>
        /// Checks whether each precondition has support. 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="conditions"></param>
        /// <param name="positive"></param>
        /// <param name="mainTaskVector"></param>
        /// <param name="atomTimeline"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        private bool CheckPreconditions(RuleInstance r, List<Tuple<int,Term>> conditions,bool positive, bool[] mainTaskVector, AtomTlnMaker atomTimeline, ref List<Tuple<Term, List<int>, int>> check)
            {
                foreach (Tuple<int, Term> tuple in conditions)
                {
                    if (!tuple.Item2.Name.Equals("=")) //equality conditions are handled in rule instance
                    {
                    List<int> oneAtomTln = new List<int>();
                    if (positive) oneAtomTln= atomTimeline.GetAtomTimeline(tuple.Item2,true);
                    else oneAtomTln = atomTimeline.GetAtomTimeline(tuple.Item2,false);
                    bool solved = false;
                    if (oneAtomTln == null)
                    {
                        if (positive) return false;
                        //There is no condition like this in the timeline, so this precondition cannot be satisfied. 

                    }                        
                    //actions are ordered from 1
                    //Slightly redundant but for better understanding. +1 is because actions are ordered form 1 and not 0. -1 is because we can have a task with
                    //precondiiton on position x for atom t and action that removes t on position x. Then the atom has -x in timeline but it is still valid. 
                    for (int j = tuple.Item1 + 1-1; j > 0; j--)
                        {
                            if (oneAtomTln.Contains(-j))
                            {
                                if (!r.DeleteActions.Contains(j)) r.DeleteActions.Add(j);
                            }
                            else if (oneAtomTln.Contains(j))
                            {
                                // if j is already an action that this task decomposes too then this doesnt need to be added to chec supports because this action wil alwazs be available and support the atoom. 
                                if (!mainTaskVector[j - 1]) check.Add(new Tuple<Term, List<int>, int>(tuple.Item2, oneAtomTln, j));
                                solved = true;
                                break; //jumps out of for cycle
                            }
                        }
                        if (!solved && !oneAtomTln.Contains(0))
                        {
                        //Console.WriteLine("This condition {0} for rule {1} is not satisfied", tuple.Item2, r.MainTask.TaskInstance.Name);
                       return false; //This condition can never be satisfied for this task. So we can stop trying to make it. 
                        }
                    }
                }
            return true;
            }

        private TaskType FindTaskType(Action a, List<TaskType> allTaskTypes)
        {
            foreach (TaskType t in allTaskTypes)
            {
                if (t.Name.Equals(a.ActionInstance.Name) && t.NumOfVariables == a.ActionInstance.Variables.Length) return t;
            }
            Console.WriteLine("Error: No task type matches this action {0}", a.ActionInstance);
            return null;
        }

        private double FindMaxIndex(List<Task> subtasks)
        {
            double curMax = -1;
            foreach (Task t in subtasks)
            {
                double eI = t.GetEndIndex();
                if (eI > curMax) curMax = eI;
            }
            return curMax;
        }

        private List<Rule> GetApplicableRules(List<Task> newTasks,int iteration)
        {
            List<Rule> readyRules = new List<Rule>();
            foreach (Task t in newTasks)
            {
                t.AddToTaskType();
                TaskType taskType = t.TaskType;
                List<Rule> taskRules = taskType.ActivateRules(iteration);
                readyRules.AddRange(taskRules);
            }
            return readyRules;
        }

        /// <summary>
        /// Goal task is any task that spans over the whole timeline.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private bool IsGoalTask(Task t)
        {
            bool[] actionVector = t.GetActionVector();
            for (int i = 0; i < actionVector.Length; i++)
            {
                if (!actionVector[i]) return false;
            }
            return true;
        }
    }
}
