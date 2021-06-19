using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanValidation1
{
    /// <summary>
    /// Represents a method. 
    /// </summary>
    internal class Rule
    {
        /// <summary>
        /// Describes the main task of this rule. That is the task this rules describes how to decompose. 
        /// </summary>
        internal TaskType MainTaskType;
        /// <summary>
        /// Types of subtasks. 
        /// </summary>
        internal TaskType[] TaskTypeArray;

        /// <summary>
        /// Has 1 if the given task type has at least one task instance 
        /// </summary>
        bool[] TaskTypeActivationArray;
        /// <summary>
        /// Describes in which iteration were these tasks created. 
        /// </summary>
        int[] TaskTypeActivationIterationArray;
        /// <summary>
        /// Describes min length of each subtask. 
        /// </summary>
        int[] TaskMinLegthArray; //This is set to fixed 100000. 
        int[] minOrderedTaskPositionAfter;
        int[] minOrderedTaskPosition;
        /// <summary>
        /// Number of subtasks with at least one instance 
        /// </summary>
        int ActivatedTasks;

        /// <summary>
        /// One list represents one task and the numbers in him say which variables of all vars this corresponds to. So for example for rule:
        /// Transfer(L1,C,R,L2):-Load(C,L1,R),Move(R,L1,L2),Unload(C,L2,R) with all vars (L1,C,R,L2)
        /// The array looks like this{(1,0,2),(2,0,3),(1,3,2)}.
        /// 
        /// </summary>
        public List<int>[] ArrayOfReferenceLists;

        //Refrences from main task to allVars.
        public List<int> MainTaskReferences;

        /// <summary>
        /// All variables used in this rule (in main task or any subtask)
        /// </summary>
        public List<String> AllVars = new List<string>();
        public List<ConstantType> AllVarsTypes = new List<ConstantType>();

        /// The first int says to which of the rule's subtasks this applies to,the string is the name of the condition and the list of ints i the references to the variables in this rule.
        /// -1 means it must be before the all the rules of this subtask. 
        /// So for example for condition at(C,L1) for load(C,L1,R)  we
        /// have tuple (0,at,(0,1))
        /// </summary>
        public List<Tuple<int, String, List<int>>> PosPreConditions;
        public List<Tuple<int, String, List<int>>> NegPreConditions;

        /// <summary>
        /// For between conditions, we have two ints representing which actions they are related too. Then name of condition. Then lists of int representing to which variables they are related to.
        /// 
        /// So for exmaple for condition on(R,C) between Load(C,L1,R) and Unload(C,L2,R) would be this: (0,2,on,(2,0),(2,0))
        /// </summary>
        public List<Tuple<int, int, String, List<int> >> PosBetweenConditions;
        public List<Tuple<int, int, String, List<int>>> NegBetweenConditions;

        /// <summary>
        /// Order conditions for subtasks. For example (1,3) says that task 1 must be before task 3. 
        /// </summary>
        public List<Tuple<int, int>> OrderConditions;

        /// <summary>
        /// Represents the number of subtasks that are after this particual subtask based on ordering.         /// 
        /// </summary>
        public int[] NumOfOrderedTasksAfterThisTask;

        /// <summary>
        /// Same as after this task except for Before. 
        /// </summary>
        public int[] NumOfOrderedTasksBeforeThisTask;

        public Rule()
        {
            PosPreConditions = new List<Tuple<int, string, List<int>>>();
            NegPreConditions = new List<Tuple<int, string, List<int>>>();
            PosBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
            NegBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
        }

        //It is given everything it wil be given. It should fill up the rest.
        //For exmaple must fill reference list and maintaskreferences.
        /// <summary>
        /// This should be called everytime you create a new rule from input reader. 
        /// </summary>
        /// <param name="refList"></param>
        internal void Finish(List<List<int>> refList)
        {
            TaskTypeActivationArray = new bool[TaskTypeArray.Length];
            TaskTypeActivationIterationArray = new int[TaskTypeArray.Length];
            TaskMinLegthArray = Enumerable.Repeat(100000, TaskTypeArray.Length).ToArray();
            minOrderedTaskPositionAfter = new int[TaskTypeArray.Length];
            minOrderedTaskPosition = new int[TaskTypeArray.Length];
            ArrayOfReferenceLists = refList.ToArray();
            if (PosPreConditions == null) PosPreConditions = new List<Tuple<int, string, List<int>>>();
            if (NegPreConditions == null) NegPreConditions = new List<Tuple<int, string, List<int>>>();
            if (PosBetweenConditions == null) PosBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
            if (NegBetweenConditions == null) NegBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
        }

        private void CalculateTaskMinMaxPosition()
        {
            if (OrderConditions != null && OrderConditions.Any())
            {
                List<List<int>> listAfter = CreateListsOfTasks(true);
                List<List<int>> listBefore = CreateListsOfTasks(false);
                for (int i = 0; i < listAfter.Count; i++)
                {
                    List<int> tasksAfter = CreateListFor(i, listAfter);
                    tasksAfter = tasksAfter.Distinct().ToList();
                    int sum = 0;
                    for (int j = 0; j < tasksAfter.Count; j++)
                    {
                        sum = sum + TaskMinLegthArray[tasksAfter[j]];
                    }
                    minOrderedTaskPositionAfter[i] = sum;
                    List<int> tasksBefore = CreateListFor(i, listBefore);
                    sum = 0;
                    for (int j = 0; j < tasksBefore.Count; j++)
                    {
                        sum = sum + TaskMinLegthArray[tasksBefore[j]];
                    }
                    minOrderedTaskPosition[i] = sum;

                }
            }
        }

       
        private List<int> CreateListFor(int i, List<List<int>> listAfter)
        {
            List<int> tasksAfter = listAfter[i].ToList();
            foreach (int l in listAfter[i])
            {
                tasksAfter.AddRange(CreateListFor(l, listAfter));
            }
            return tasksAfter;
        }

        /// <summary>
        /// True means tasks after false means tasks before. 
        /// </summary>
        /// <returns></returns>
        private List<List<int>> CreateListsOfTasks(bool after)
        {
            List<List<int>> indexOfTasksAfter = new List<List<int>>(TaskTypeArray.Length); //Represents the index of tasks that are ordered with this task. Only immediate level. Meaning if I have ordering 1<2 and 2<3. INdex 1 onlz has 2 there. 
            for (int i = 0; i < TaskTypeArray.Length; i++) indexOfTasksAfter.Add(null);
            for (int i = 0; i < TaskTypeArray.Length; i++)
            {
                List<int> tupledWith;
                if (after)
                {
                    TupleWithXFirst(i, out tupledWith);
                    indexOfTasksAfter[i] = tupledWith;
                }
                else
                {
                    TupleWithXLast(i, out tupledWith);
                    indexOfTasksAfter[i] = tupledWith;
                }
            }
            return indexOfTasksAfter;
        }

        /// <summary>
        /// Returns the number of ordering tuples where this number is first.
        /// In the tupledWithList returns the indexof tasks it's ordered with. 
        /// </summary>
        /// <returns></returns>
        private int TupleWithXFirst(int index, out List<int> tupledWith)
        {
            List<Tuple<int, int>> rightTuples = OrderConditions.Where(x => x.Item1 == index).ToList();
            tupledWith = rightTuples.Select(x => x.Item2).ToList();
            return tupledWith.Count;
        }

        /// <summary>
        /// Returns the number of ordering tuples where this number is first.
        /// In the tupledWithList returns the indexof tasks it's ordered with. 
        /// </summary>
        /// <returns></returns>
        private int TupleWithXLast(int index, out List<int> tupledWith)
        {
            List<Tuple<int, int>> rightTuples = OrderConditions.Where(x => x.Item2 == index).ToList();
            tupledWith = rightTuples.Select(x => x.Item1).ToList();
            return tupledWith.Count;
        }

        /// <summary>
        /// Calculates number of subtasks before and after this subtask. 
        /// </summary>
        private void CalculateActionsAfter()
        {
            NumOfOrderedTasksAfterThisTask = new int[TaskTypeArray.Length];
            NumOfOrderedTasksBeforeThisTask = new int[TaskTypeArray.Length];
            if (OrderConditions != null && OrderConditions.Any())
            {
                List<List<int>> listAfter = CreateListsOfTasks(true);
                List<List<int>> listBefore = CreateListsOfTasks(false);
                for (int i = 0; i < listAfter.Count; i++)
                {
                    List<int> tasksAfter = CreateListFor(i, listAfter);
                    tasksAfter = tasksAfter.Distinct().ToList();
                    NumOfOrderedTasksAfterThisTask[i] = tasksAfter.Count();
                    List<int> tasksBefore = CreateListFor(i, listBefore);
                    NumOfOrderedTasksBeforeThisTask[i] = tasksBefore.Distinct().Count();
                }
            }
        }

        /// <summary>
        /// Returns true if after activating this task the rule is ready to be used. 
        /// Int j says how many instances maximum I can fill in this rule. 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool Activate(TaskType t, int j,int iteration)
        {
            List<int> occurences = Enumerable.Range(0, TaskTypeArray.Length).Where(p => TaskTypeArray[p] == t).ToList();
            if (occurences.Count > j) return false; //I cant fill all instances of this subtask in this rule so it definitely canot be used.
            else
            {
                foreach (int i in occurences)
                {
                    if (!TaskTypeActivationArray[i]) ActivatedTasks++; //If this activated the task (as in it was not ready before) it should increase the activated task counter.
                    TaskTypeActivationArray[i] = true;
                    TaskTypeActivationIterationArray[i] = iteration; //Iterations always increases over time. So if I had a different task here in iteration 4, that's fine now I rewrite it to 6.
                    if (t.MinTaskLength < TaskMinLegthArray[i]) TaskMinLegthArray[i] = t.MinTaskLength;
                }
                if (!TaskMinLegthArray.Contains(100000))
                {
                    int sum = TaskMinLegthArray.Sum();
                    bool changed = MainTaskType.SetMinTaskLengthIfSmaller(sum);
                    if (changed) CalculateTaskMinMaxPosition();
                }
            }

            return ActivatedTasks == TaskTypeActivationArray.Length;
        }

        /// <summary>
        /// Order is fixed from the position of subtasks. This creates the orderConditions.  
        /// </summary>
        internal void CreateOrder()
        {
            OrderConditions = new List<Tuple<int, int>>();
            for (int i = 0; i < TaskTypeArray.Length - 1; i++)
            {
                int j = i + 1;
                Tuple<int, int> t = new Tuple<int, int>(i, j);
                OrderConditions.Add(t);
            }
            CalculateActionsAfter();
        }

        /// <summary>
        /// Returns combination of taskInstances from task types that works with this rule.
        /// Empty rules can go through this they will not return any ruleInstance.
        /// </summary>
        public List<RuleInstance> GetRuleInstances(int size, List<Constant> allConstants,int iteration, int planSize)
        {
            Constant[] nullArray = new Constant[MainTaskType.NumOfVariables];
            for (int i = 0; i < nullArray.Length; i++)
            {
                nullArray[i] = null;
            }
            Term t = new Term(MainTaskType.Name, nullArray);
            Task MainTaskInstance = new Task(t, size, MainTaskType);
            List<Constant> emptyVars = FillFromAllVars(allConstants); //Fixed constants are fixed. Also forall constant is fixed. 
            List<Tuple<Task, List<Task>, List<Constant>>> ruleVariants = new List<Tuple<Task, List<Task>, List<Constant>>>();
            for (int i = 0; i < TaskTypeActivationIterationArray.Length; i++)
            {
                if (TaskTypeActivationIterationArray[i] == iteration)
                {
                    List<Tuple<Task, List<Task>, List<Constant>>> newvariants = GetNextSuitableTask(TaskTypeArray[i], -1, i, emptyVars, new List<Task>(new Task[TaskTypeArray.Length]), planSize, iteration); //Trying with emptz string with all vars it has error in fill maintask //Should this be new empty string or is allvars ok?
                    ruleVariants.AddRange(newvariants);
                }
            }
            List<RuleInstance> ruleInstances = new List<RuleInstance>();
            if (ruleVariants != null)
            {
                foreach (Tuple<Task, List<Task>, List<Constant>> ruleVariant in ruleVariants)
                {
                    if (ruleVariant.Item3.Contains(null)) //This might happen in multiple ways:
                                                          //1] main task has some parameter that none of its subtasks look at. Problem one we fill by creating a task with all possible constants. 
                                                          //2] there is a forall condition in my conditions. This will not happen as in emptyvars this value is filled. 
                    {
                        List<List<Constant>> newAllVars = FillWithAllConstants(ruleVariant.Item3, AllVarsTypes, allConstants, new List<List<Constant>>());
                        newAllVars = newAllVars.Distinct().ToList();
                        foreach (List<Constant> allVar in newAllVars)
                        {
                            //Aside from making the rule instance we must also fill the main task properly here. 
                            Task t2 = FillMainTaskFromAllVars(allVar);
                            RuleInstance ruleInstance = new RuleInstance(t2, ruleVariant.Item2, this, allVar.Select(x => x.Name).ToList(), allConstants);
                            if (ruleInstance.IsValid())
                            {
                                ruleInstances.Add(ruleInstance);
                            }
                        }
                    }
                    else
                    {
                        RuleInstance ruleInstance = new RuleInstance(ruleVariant.Item1, ruleVariant.Item2, this, ruleVariant.Item3.Select(x => x.Name).ToList(), allConstants);
                        if (ruleInstance.IsValid()) ruleInstances.Add(ruleInstance);
                    }
                }
            }
            return ruleInstances;
        }

        /// <summary>
        /// Fills nulls in rule with all possible constants that fit the type. Returns as one big list of list of strings.
        /// </summary>
        /// <param name="item3"></param>
        /// <param name="allVarsTypes"></param>
        /// <param name="allConstants"></param>
        /// <returns></returns>
        public List<List<Constant>> FillWithAllConstants(List<Constant> item3, List<ConstantType> allVarsTypes, List<Constant> allConstants, List<List<Constant>> solution)
        {
            int i = item3.IndexOf(null);
            if (i == -1)
            {
                solution.Add(item3);
                return solution;
            }
            else
            {
                ConstantType desiredType = AllVarsTypes[i];
                List<Constant> fittingConstants = allConstants.Where(x => desiredType.IsAncestorTo(x.Type)).ToList();
                foreach (Constant c in fittingConstants)
                {
                    List<Constant> newAllVars = new List<Constant>(item3);
                    newAllVars[i] = c;
                    List<List<Constant>> newSolutions = FillWithAllConstants(newAllVars, allVarsTypes, allConstants, solution);
                    solution.AddRange(newSolutions);
                    solution = solution.Distinct().ToList();
                    newAllVars = new List<Constant>(item3);
                }
                return solution;
            }
        }

        /// <summary>
        /// Creates empty vars from all vars. Empty vars is a list that is empty and as big as allvars but filled where rule uses constant not variable. 
        /// variables start with ?. 
        /// </summary>
        /// <returns></returns>
        private List<Constant> FillFromAllVars(List<Constant> allConstants)
        {
            List<Constant> emptyVars = new List<Constant>(new Constant[AllVars.Count]);
            for (int i = 0; i < AllVars.Count; i++)
            {
                if (!AllVars[i].StartsWith("?"))
                {
                    Constant c = allConstants.Find(x => x.Name == AllVars[i] && AllVarsTypes[i].IsAncestorTo(x.Type)); //If this is forall consatnt it will return null.
                    if (AllVars[i].StartsWith("!")) c = new Constant(AllVars[i], AllVarsTypes[i]);
                    emptyVars[i] = c;
                }
            }
            return emptyVars;
        }

        internal void AddOrderCondition(int item31, int item32)
        {
            if (OrderConditions == null) OrderConditions = new List<Tuple<int, int>>();
            Tuple<int, int> t = new Tuple<int, int>(item31, item32);
            OrderConditions.Add(t);
        }

        //This finds all applicable tasks from list.
        //Tuple item1 is main task, list of subtasks and allvars.       
        //will we have the same problem with conditions being of wrong type????
        private List<Tuple<Task, List<Task>, List<Constant>>> GetNextSuitableTask(TaskType t, int index, int newindex, List<Constant> partialAllVars, List<Task> subtasks, int planSize, int curIteration)
        {
            List<Task> unusedInstances = t.Instances.Except(subtasks).Distinct().ToList();
            if (index == -1) //Tasktype must be given as the new one. 
            {
                unusedInstances = unusedInstances.Where(x => x.Iteration == curIteration).ToList();
                index = newindex; //Temporarily we change the index so we don't have to change everything else and then after we switch it back to -1.
            }
            else if (index < newindex) //This ensures that if I have rule with 2 newsubtasks I wont get it twice. 
                                       //Anything after newindex can be both new and old. 
            {
                unusedInstances = unusedInstances.Where(x => x.Iteration < curIteration).ToList();
            }
            List<int> myReferences = ArrayOfReferenceLists[index];
            List<Tuple<Task, List<Task>, List<Constant>>> myResult = new List<Tuple<Task, List<Task>, List<Constant>>>();
            List<Tuple<Task, List<Task>, List<Constant>>> newMyResult = null;
            int oldIndex = 0; //Index of tasks already picked for this instance. 
            foreach (Task l in subtasks)
            {
                if (l != null)
                {
                    //These are tasks I already picked for this instance. 
                    if (IsBefore(index, oldIndex))
                    {
                        unusedInstances = unusedInstances.Where(x => Math.Floor(x.EndIndex) < Math.Ceiling(l.StartIndex)).ToList(); //Our task must be before this subtask so I shall only look at possible instances that end before the other starts. 
                      
                    }
                    else if (IsBefore(oldIndex, index))
                    {
                       unusedInstances = unusedInstances = unusedInstances.Where(x => Math.Ceiling(x.StartIndex) > Math.Floor(l.EndIndex)).ToList(); //My task must start after task l.
                    }
                    unusedInstances = unusedInstances.Where(x => Differs(x.GetActionVector(), l.GetActionVector())).ToList(); //NO problem on empty task becasue they return null.
                }
                oldIndex++;
            }

            if (NumOfOrderedTasksAfterThisTask?[index] > 0) 
            {
                unusedInstances = unusedInstances.Where(x => Math.Floor(x.EndIndex) < planSize - minOrderedTaskPositionAfter[index]).ToList(); //assuming plan size of action number. So for plan from 0-7 plan size is 8.
            }
            if (NumOfOrderedTasksBeforeThisTask?[index] > 0)
            {
                unusedInstances = unusedInstances.Where(x => Math.Ceiling(x.StartIndex) >= minOrderedTaskPosition[index]).ToList(); 
            }

            if (index == newindex) index = -1;

            foreach (Task tInstance in unusedInstances)
            {
                List<Constant> newAllVars = FillMainTask(tInstance, myReferences, partialAllVars);
                if (newAllVars != null)
                {
                    List<Task> newSubTasks = new List<Task>(subtasks);
                    //newSubTasks.Add(tInstance);
                    if (index == -1) newSubTasks[newindex] = tInstance;
                    else newSubTasks[index] = tInstance;
                    //We just assigned the last task. 
                    if (index == TaskTypeArray.Length - 1 || (index + 1 == newindex && newindex == TaskTypeArray.Length - 1))
                    {
                        if (myResult == null) myResult = new List<Tuple<Task, List<Task>, List<Constant>>>();
                        //We must fill up the main task from allVars.
                        Task newMainTask = FillMainTaskFromAllVars(newAllVars);
                        Tuple<Task, List<Task>, List<Constant>> thisTaskSubTaskCombo = Tuple.Create(newMainTask, newSubTasks, newAllVars);
                        myResult.Add(thisTaskSubTaskCombo);
                    }
                    else
                    {
                        if (index + 1 == newindex && newindex < TaskTypeArray.Length - 1)
                        {//This makes us skip the new task. because we already picked the task for the new task. 
                            newMyResult = GetNextSuitableTask(TaskTypeArray[index + 2], index + 2, newindex, newAllVars, newSubTasks, planSize, curIteration);
                        }
                        else
                        {
                            newMyResult = GetNextSuitableTask(TaskTypeArray[index + 1], index + 1, newindex, newAllVars, newSubTasks, planSize, curIteration);
                        }
                        myResult.AddRange(newMyResult);
                    }
                }
            }
            return myResult;
        }

        /// <summary>
        /// Returns true if task with index must be in rule before the task with oldindex.
        /// If there is no orderinng between them or if oldIndex task must be first we return false. 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="oldIndex"></param>
        /// <returns></returns>
        private bool IsBefore(double index, double oldIndex)
        {
            if (OrderConditions?.Any() != true) return false; //There is no ordering. 
            foreach (Tuple<int, int> tuple in OrderConditions)
            {
                if (tuple.Item1 == index && tuple.Item2 == oldIndex) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if arrays don't contain same elements. 
        /// </summary>
        /// <param name="usedActions1"></param>
        /// <param name="usedActions2"></param>
        /// <returns></returns>
        private bool Differs(bool[] usedActions1, bool[] usedActions2)
        {
            if (usedActions1?.Any() != true) return true;
            if (usedActions2?.Any() != true) return true;
            for (int i = 0; i < usedActions1.Length; i++)
            {
                if (i < usedActions2.Length && usedActions1[i] && usedActions2[i]) return false;
            }
            return true;
        }

        private Task FillMainTaskFromAllVars(List<Constant> myAllVars)
        {
            String taskName = MainTaskType.Name;
            Constant[] vars = new Constant[MainTaskReferences.Count];
            for (int i = 0; i < MainTaskReferences.Count; i++)
            {
                vars[i] = myAllVars[MainTaskReferences[i]]; //Here we give the type from the task but we should give it the type of the constant.                 
            }
            Term term = new Term(taskName, vars);
            Task t = new Task(term, MainTaskReferences.Count, MainTaskType);
            return t;
        }

        /// <summary>
        /// Treis to fill the allvars in this rule. Currently will not fillthe main task variables those will be filled retrospectively if the rule filling is correct.
        /// Returns new string[] which represents new allVars adjusted. If it didn't work returns null.
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="myReferences"></param>
        /// <param name="partialMainTask"></param>
        /// <param name="allVars"></param>
        /// <returns></returns>
        private List<Constant> FillMainTask(Task t, List<int> myReferences, List<Constant> allVars)
        {
            List<Constant> newAllVars = new List<Constant>(allVars);
            for (int i = 0; i < myReferences.Count; i++)
            {
                //First check if the type fits and then if so try to fill the variable in. 
                ConstantType desiredType = AllVarsTypes[myReferences[i]];
                Constant myVariable = allVars[myReferences[i]];
                if (allVars[myReferences[i]] == null)
                {
                    //allvars is empty. Does what I want to put here fit my desired type if so I just add it if not I will return null.                     
                    if (desiredType.IsAncestorTo(t.TaskInstance.Variables[i].Type))
                    {
                        newAllVars[myReferences[i]] = t.TaskInstance.Variables[i];
                    }
                    else return null;
                }
                else if (t.TaskInstance.Variables[i].Name != myVariable.Name || !desiredType.IsAncestorTo(t.TaskInstance.Variables[i].Type)) //in all vars this variable is already assigned and its not to the same value as my variable. So this task cannot be used. 
                                                                                                                                             //we must also check if it's the right type. if not return null 
                {
                    return null;
                }
            }
            return newAllVars;
        }

        public override string ToString()
        {
            string text = "";
            if (TaskTypeArray?.Any() == true) text = string.Join(",", TaskTypeArray.Select(x => x.Name));
            string text2 = string.Join(",", AllVars);
            string text3 = string.Join(",", PosPreConditions.Select(x => x.Item2)) + string.Join(",", PosPreConditions.Select(x => x.Item3));
            string text4 = string.Join(",", NegPreConditions.Select(x => x.Item2)) + string.Join(",", NegPreConditions.Select(x => x.Item3));
            string text7 = string.Join(",", PosBetweenConditions.Select(x => x.Item3)) + string.Join(",", PosBetweenConditions.Select(x => x.Item4));
            string text8 = string.Join(",", NegBetweenConditions.Select(x => x.Item3)) + string.Join(",", NegBetweenConditions.Select(x => x.Item4));
            String s = "Rule: " + this.MainTaskType.Name + " subtasks " + text + " parameters " + text2 + " posPreCond" + text3 + "negPreCond " + text4 + "posBetweenCond " + text7 + "negBetweenCond" + text8;
            return s;
        }
    }
}
