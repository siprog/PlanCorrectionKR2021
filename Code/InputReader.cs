using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PlanValidation1
{
    /// <summary>
    /// Reads input and creates all action/task types and all rules. 
    /// </summary>
    class InputReader
    {
        enum State { InMethod, InSubtasks, Nowhere, InTaskInfo, Ordering, Conditions, InAction, ActPrecond, ActEffects, InTypes, InConstants, BetweenConditions };
        enum Ordering { Preset, Later, None };
        /// <summary>
        /// List of all actions types. 
        /// </summary>
        public List<ActionType> GlobalActions;
        /// <summary>
        /// List of all task types
        /// </summary>
        public List<TaskType> AlltaskTypes;
        /// <summary>
        /// List of all rules
        /// </summary>
        public List<Rule> AllRules;
        /// <summary>
        /// List of all empty rules. 
        /// </summary>
        public List<Rule> EmptyRules = new List<Rule>();
        /// <summary>
        /// List of all actions. 
        /// </summary>
        public List<Action> MyActions = new List<Action>();
        /// <summary>
        /// List of all constant types
        /// </summary>
        public List<ConstantType> AllConstantTypes = new List<ConstantType>();
        /// <summary>
        /// List of all constants
        /// </summary>
        public List<Constant> AllConstants = new List<Constant>();
        bool Forall = false;
        Constant ForallConst = null; //INFO so far we just allow one. 

        /// <summary>
        /// Reads input domain. 
        /// </summary>
        /// <param name="fileName"></param>
        public bool ReadDomain(String fileName)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            String line = "";
            int lineCount=0;
            Ordering ordering = Ordering.None;
            State state = State.InTaskInfo;
            AlltaskTypes = new List<TaskType>();
            Rule curRule = new Rule();
            AllRules = new List<Rule>();
            List<Constant> paramTypeInfo = new List<Constant>();
            List<String> parameters = new List<string>();
            List<Tuple<TaskType, String, int>> namedTasks = new List<Tuple<TaskType, string, int>>();
            List<TaskType> curSubtaskList = new List<TaskType>();
            List<List<int>> referenceLists = new List<List<int>>();
            Rule lastRule = null;
            ActionType curActionType = new ActionType();
            int num = 0;
            List<Tuple<Term, bool>> preconditions = new List<Tuple<Term, bool>>();
            List<Tuple<List<int>, Term, bool>> betweenConditions = new List<Tuple<List<int>, Term, bool>>();
            GlobalActions = new List<ActionType>();
            int subtaskCount = 0;
            bool doneSubtask = false;
            bool doneConditions = false;
            bool doneConstants = false;
            bool doneActEff = false;
            bool doneOrder = false;

            String actName = "";
            bool lastInConditions = false;
            try
            {
                while ((line = file.ReadLine()) != null)
                {
                    lineCount++;
                    line = line.Trim();
                    if (line.Contains(":types"))
                    {
                        state = State.InTypes;
                    }
                    if (line.Contains(":constants"))
                    {
                        state = State.InConstants;
                    }
                    if (state == State.InTypes)
                    {
                        if (line.Trim().Equals(")"))
                        {
                            FinishTypeHierarchy(ref AllConstantTypes);
                            state = State.InTaskInfo;
                        }
                        else
                        {
                            CreateTypeHieararchy(line, AllConstantTypes);
                        }
                    }
                    if (state == State.InConstants)
                    {
                        if (line.Trim().Equals(")"))
                        {
                            state = State.InTaskInfo;
                        }
                        else
                        {
                            GetConstants(line, ref AllConstants, AllConstantTypes);
                            doneConstants = CheckParenthesis(line) > 0;
                            if (doneConstants)
                            {
                                state = State.InTaskInfo;
                                doneConstants = false;
                            }
                        }
                    }
                    if (state == State.InTaskInfo && line.Contains(":task"))
                    {
                        //Getting list of all tasks
                        TaskType tT = CreateTaskType(line);
                        AlltaskTypes.Add(tT);
                    }
                    if (line.Contains("(:method")) state = State.InMethod;
                    if (line.Contains("(:action")) state = State.InAction;
                    if (state == State.InMethod)
                    {
                        if (line.Trim().Equals(")") && lastInConditions)
                        {
                            //This is an empty rule.   
                            if (paramTypeInfo != null)
                            {
                                curRule.AllVars = paramTypeInfo.Select(x => x.Name).ToList();
                                curRule.AllVarsTypes = paramTypeInfo.Select(x => x.Type).ToList();
                            }
                            CreateConditions(curRule, preconditions);
                            preconditions = new List<Tuple<Term, bool>>();
                            EmptyRules.Add(curRule);
                            lastInConditions = false;
                            curRule = new Rule();
                        }
                        if (line.Contains(":parameters"))
                        {
                            paramTypeInfo = GetParameters(line, AllConstantTypes);
                            if (paramTypeInfo != null)
                            {
                                parameters = paramTypeInfo.Select(x => x.Name).ToList();
                                curRule.AllVars = parameters; //This will change later but at least for conditions we must have it already.
                                curRule.AllVarsTypes = paramTypeInfo.Select(x => x.Type).ToList();
                            }
                        }
                        else if (line.Contains(":task"))
                        {
                            //Getting  main task
                            TaskType tT = CreateTaskType(line, ref paramTypeInfo, out List<int> refList, AllConstants);
                            TaskType t = FindTask(tT, AlltaskTypes);
                            curRule.MainTaskType = t;
                            curRule.MainTaskReferences = refList;
                        }
                        else if (line.Contains(":subtasks") || line.Contains(":ordered-subtasks"))
                        {
                            lastInConditions = false;
                            state = State.InSubtasks;
                            subtaskCount = 0;
                            if (line.Contains("ordered")) ordering = Ordering.Preset;
                            doneSubtask = false;
                        }
                        else if (line.Contains(":ordering"))
                        {
                            state = State.Ordering;
                            num = 0;
                        }
                        else if (line.Contains(":precondition"))
                        {
                            state = State.Conditions;
                            doneConditions = false;
                        }
                        else if (line.Contains(":between-condition"))
                        {
                            state = State.BetweenConditions;
                        }
                    }
                    else if (state == State.Conditions)
                    {
                        //Checks if there are more closed parenthesis and this section is over. 
                        if (Forall) doneConditions = CheckParenthesis(line) > 1; // one closed parentehis is from forall. 
                        else doneConditions = CheckParenthesis(line) > 0;
                        if (line.Trim().Equals(")"))
                        {
                            state = State.InMethod;
                        }
                        else
                        {
                            Tuple<Term, bool> condition = CreateCondition(line, ref paramTypeInfo, AllConstants);
                            if (condition != null) preconditions.Add(condition);
                            if (doneConditions)
                            {
                                //If the rule is empty as in has no substasks than this is the last thing it will go through.
                                state = State.InMethod;
                                lastInConditions = true;
                            }
                        }
                    }
                    else if (state == State.BetweenConditions)
                    {
                        doneConditions = CheckParenthesis(line) > 0;
                        if (line.Trim().Equals(")"))
                        {
                            state = State.InMethod;
                        }
                        else
                        {
                            line = line.Replace("(", "");
                            string[] parts = line.Split(' '); //line loks like this>     1 2 powerco-of ?town ?powerco))
                            while (parts.Length >= 1 && parts[0] == "")
                            {
                                parts = (string[])parts.Skip(1).ToArray();
                            }
                            try
                            {
                                List<int> betweenTasks = new List<int>
                            {
                                Int32.Parse(parts[0]),
                                Int32.Parse(parts[1])
                            };
                                line = line.Replace(Int32.Parse(parts[0]) + " " + Int32.Parse(parts[1]) + " ", ""); //Now this looks like a normal condition.     powerco-of ?town ?powerco))
                                Tuple<Term, bool> condition = CreateCondition(line, ref paramTypeInfo, AllConstants);
                                betweenConditions.Add(new Tuple<List<int>, Term, bool>(betweenTasks, condition.Item1, condition.Item2));
                                if (doneConditions)
                                {
                                    //If the rule is empty as in has no substasks than this is the last thing it will go through.
                                    state = State.InMethod;
                                    lastInConditions = true; 
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error: Invalid description of a between condition: " + line);
                            }
                        }
                    }
                    else if (state == State.InSubtasks)
                    {
                        if (!line.Trim().Equals(")"))
                        {
                            //Checks if there are more closed parenthesis and this section is over. 
                            doneSubtask = CheckParenthesis(line) > 0;
                            if (subtaskCount == 0 && ordering != Ordering.Preset)
                            {
                                //Only check this if ordering is not preset. Cause then I know the ordering. 
                                int parenthesisCount = line.Count(x => x == '(');
                                if (parenthesisCount > 1) ordering = Ordering.Later;
                                else ordering = Ordering.None;
                            }
                            if (ordering == Ordering.Preset || ordering == Ordering.None)
                            {
                                //Ordered subtasks look almost the same as regular tasks. 
                                List<int> refList = new List<int>();
                                TaskType tT = CreateTaskType(line, ref paramTypeInfo, out refList, AllConstants);
                                TaskType t = FindTask(tT, AlltaskTypes);
                                if (t == tT)
                                {
                                    AlltaskTypes.Add(t);
                                }
                                if (!t.Rules.Contains(curRule)) t.AddRule(curRule);
                                referenceLists.Add(refList);
                                curSubtaskList.Add(t);
                                subtaskCount++;
                            }
                            else
                            {
                                //Unordered subtask. After subtasks there will be ordering.
                                List<int> refList = new List<int>();
                                Tuple<TaskType, string> tupleTaskName = CreateNamedTaskType(line, ref paramTypeInfo, out refList, AllConstants);
                                Tuple<TaskType, string, int> tupleFull = new Tuple<TaskType, string, int>(tupleTaskName.Item1, tupleTaskName.Item2, num);
                                namedTasks.Add(tupleFull);
                                TaskType t = FindTask(tupleTaskName.Item1, AlltaskTypes);  //Finds the task in lists of all tasks. 
                                if (t == tupleTaskName.Item1)
                                {
                                    AlltaskTypes.Add(t);
                                }
                                if (!t.Rules.Contains(curRule)) t.AddRule(curRule); //Adds a link from this tasktype to the rule. 
                                curSubtaskList.Add(t);
                                referenceLists.Add(refList);
                                num++;
                                subtaskCount++;
                            }
                        }
                        if (line.Trim().Equals(")") || doneSubtask)
                        {                           
                            if (paramTypeInfo != null)
                            {
                                curRule.AllVars = paramTypeInfo.Select(x => x.Name).ToList();
                                curRule.AllVarsTypes = paramTypeInfo.Select(x => x.Type).ToList();
                            }
                            //At least one subtask is not fully ordered.                             
                            CreateConditions(curRule, preconditions);
                            CreateBetweenConditions(curRule, betweenConditions);
                            curRule.TaskTypeArray = curSubtaskList.ToArray();
                            curRule.Finish(referenceLists);
                            if (ordering == Ordering.Preset) curRule.CreateOrder();
                            curSubtaskList = new List<TaskType>();
                            referenceLists = new List<List<int>>();
                            paramTypeInfo = null;
                            AllRules.Add(curRule);
                            lastRule = curRule; //for ordering
                            curRule = new Rule();
                            if (ordering != Ordering.Later) state = State.Nowhere;
                            else state = State.InMethod;
                            parameters = new List<string>();
                            preconditions = new List<Tuple<Term, bool>>();
                            betweenConditions = new List<Tuple<List<int>, Term, bool>>();
                            ordering = Ordering.None;
                            subtaskCount = 0;
                        }
                    }
                    else if (state == State.Ordering)
                    {
                        doneOrder = CheckParenthesis(line) > 0;
                        if (!line.Trim().Equals(")"))
                        {
                            CreateOrder(line, namedTasks, ref lastRule);
                        }
                        if (line.Trim().Equals(")") || doneOrder)
                        {
                            num = 0;
                            namedTasks = new List<Tuple<TaskType, string, int>>();
                            state = State.Nowhere;
                            ordering = Ordering.None;                           
                            
                        }
                    }
                    else if (state == State.InAction)
                    {
                        List<Constant> actVars = new List<Constant>();
                        if (line.Contains(":action"))
                        {
                            actName = GetActionName(line);
                            curActionType.Name = actName;
                        }
                        else if (line.Contains("parameters"))
                        {
                            actVars = GetParameters(line, AllConstantTypes);
                            if (actVars != null)
                            {
                                curActionType.NumOfVariables = actVars.Count;
                                curActionType.Vars = actVars;
                            }
                            else
                            {
                                curActionType.NumOfVariables = 0;
                            }
                        }
                        else if (line.Contains(":precondition"))
                        {
                            state = State.ActPrecond;
                        }
                    }
                    if (state == State.ActPrecond) //since preconditions have the first condition on the same line as the declaration this must be if and not else if. 
                    {
                        bool isPos;
                        if (line.Contains(":effect"))
                        {
                            state = State.ActEffects;
                        }
                        else
                        {
                            Tuple<String, List<int>> condition = null;
                            isPos = true;
                            //What if I have action without parameters and with conditions?
                            //The question mark does that if vars is null it will jsut pass null inside the method. 
                            condition = GetActionCondition(line, curActionType.Vars?.Select(x => x.Name).ToList(), ref curActionType.Constants, out isPos);

                            //If the variable does not have any parameters than it ccannot have any conditions either. 
                            if (condition != null)
                            {
                                if (isPos) curActionType.PosPreConditions.Add(condition);
                                else curActionType.NegPreConditions.Add(condition);
                            }
                        }
                    }
                    if (state == State.ActEffects)
                    {
                        if (Forall) doneActEff = CheckParenthesis(line) > 1;
                        else doneActEff = CheckParenthesis(line) > 0;
                        if (!line.Trim().Equals(")"))
                        {
                            bool isPos;
                            if (!line.Trim().Equals(""))
                            {
                                Tuple<String, List<int>> condition = null;
                                isPos = true;
                                condition = GetActionCondition(line, curActionType.Vars?.Select(x => x.Name).ToList(), ref curActionType.Constants, out isPos);

                                if (condition != null)
                                {
                                    if (isPos) curActionType.PosEffects.Add(condition);
                                    else curActionType.NegEffects.Add(condition);
                                }
                            }
                        }
                        if (line.Trim().Equals(")") || doneActEff)
                        {
                            if (curActionType.Name != null) GlobalActions.Add(curActionType);
                            curActionType = new ActionType();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Eror reading domain in file {0}.", fileName);
                Console.WriteLine("Error was found on line number {0}: {1}",lineCount, line);
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reads constants from list of constants.Line looks like this :fema ebs police-chief - callable
        /// </summary>
        /// <param name="line"></param>
        /// <param name="allConstants"></param>
        /// <param name="allConstantTypes"></param>
        /// 
        private void GetConstants(string line, ref List<Constant> allConstants, List<ConstantType> allConstantTypes)
        {
            line = CleanUpInput(line, new List<string>() { "(:constants", "(and", "(", ")" }, ";;");
            string[] parts = line.Trim().Split(' ');
            if (parts.Length < 1) return;
            while (parts.Length >= 1 && parts[0] == "")
            {
                parts = (string[])parts.Skip(1).ToArray();
            }
            if (parts.Length < 1) return;
            String s = parts[parts.Length - 1]; //The final type. In example it's callable.
            ConstantType t = ContainsType(allConstantTypes, s);
            if (t == null)
            {
                //This type does not exist. 
                if (s != ":constants") Console.WriteLine("Warning:Constants have non existent Type {0}", s);
                return;
            }
            parts[parts.Length - 1] = null;
            foreach (String m in parts)
            {
                if (m != null && m != "-")
                {
                    Constant c1 = new Constant(m, t);
                    allConstants.Add(c1);
                }
            }
        }


        /// <summary>
        /// Returns true if this ordering is full.
        /// This requires only non transitive order conditions.
        /// </summary>
        /// <param name="lastRule"></param>
        /// <returns></returns>
        private bool IsFullyOrdered(Rule lastRule)
        {
            //This is true if a subtask at given posisiton is immediately before some other task
            bool[] isImmediatelyBefore = new bool[lastRule.TaskTypeArray.Length];
            //This is true if a subtask at given posisiton is immediately after some other task
            bool[] isImmediatelyAfter = new bool[lastRule.TaskTypeArray.Length];
            foreach (var order in lastRule.OrderConditions)
            {
                isImmediatelyBefore[order.Item1] = true;
                isImmediatelyAfter[order.Item2] = true;
            }
            if (isImmediatelyBefore.Count(x => !x) == 1 && isImmediatelyAfter.Count(x => !x) == 1) return true;
            return false;
        }

        /// <summary>
        /// I have created the entire type hierarchy. Now I must add type any which is a child to everything.  
        /// This is used in rules or actiontypes, when we have a constant without a type. 
        /// Can also be used to ignore all types.        ///
        /// </summary>
        /// <param name="types"></param>
        private void FinishTypeHierarchy(ref List<ConstantType> types)
        {
            ConstantType any = new ConstantType("any");
            foreach (ConstantType c in types)
            {
                any.AddAncestor(c);
                c.AddChild(any);
                c.CreateDescendantLine();
            }
            any.CreateDescendantLine();
            types.Add(any);
        }


        /// <summary>
        /// Creates type hierarchy for our own types. 
        /// line looks like this:
        ///waterco powerco - callable
        /// </summary>
        /// <param name="line"></param>
        /// <param name="types"></param>
        private void CreateTypeHieararchy(string line, List<ConstantType> types)
        {
            string[] parts = line.Trim().Split(' ');
            if (line.Trim().Equals("(:types")) return;
            String s = parts[parts.Length - 1]; //The final type.
            ConstantType t = ContainsType(types, s);
            if (t == null)
            {
                //This type does not exist create it. 
                t = new ConstantType(s);
                types.Add(t);
            }
            parts[parts.Length - 1] = null;
            foreach (String m in parts)
            {
                if (m != null && m != "-")
                {
                    ConstantType t1 = ContainsType(types, m);
                    if (t1 == null)
                    {
                        t1 = new ConstantType(m);
                        types.Add(t1);
                    }
                    t1.AddAncestor(t);
                    t.AddChild(t1);
                }
            }
        }

        private ConstantType ContainsType(List<ConstantType> types, String name)
        {
            foreach (ConstantType type in types)
            {
                if (type.Name.Equals(name)) return type;
            }
            return null;
        }

        private string GetActionName(string line)
        {
            line = line.Replace("(:action ", "");
            line = line.Trim();
            return line;
        }

        private int CheckParenthesis(string line)
        {
            int openParenthesisCount = line.Count(x => x == '(');
            int closedParenthesisCount = line.Count(x => x == ')');
            return closedParenthesisCount - openParenthesisCount;
        }


        private Tuple<string, List<int>> GetActionCondition(string line, List<string> vars, ref List<Constant> constants, out bool isPos)
        {
            if (constants == null) constants = new List<Constant>();
            isPos = true;
            if (line.Contains("(not"))
            {
                line = line.Replace("(not", "");
                isPos = false;
            }
            line = CleanUpInput(line, new List<string>() { "(and ",":precondition ", ":effect", ")", "(", }, ";;");
            string[] parts = line.Trim().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length < 1 || line.Trim().Equals("and")) return null;
            string name = parts[0];
            if (Forall)
            {
                Forall = false;
                name = "!" + name;
            }
            if (name.Trim().Equals("exists")) return null;//Currently ignores both exist conditions, which is proper behaviour. 
            if (name.Trim().Equals("forall"))
            {
                Forall = true;
                for (int i = 1; i + 2 < parts.Length; i += 3)
                {
                    name = parts[i];
                    ConstantType t = AllConstantTypes.Find(x => x.Name.Equals(parts[i + 2]));
                    if (t == null) t = AllConstantTypes.Find(x => x.Name.Equals("any"));
                    ForallConst = new Constant("!" + name, t);
                    //We will add the forallconstant to list of constants
                    constants.Add(ForallConst);
                }
                return null;
            }
            string[] myVars = (string[])parts.Skip(1).ToArray();
            List<int> references = new List<int>();
            foreach (string var in myVars)
            {
                if (!var.Equals("-"))
                {
                    int i;
                    if (vars == null) i = -1;
                    else i = vars.IndexOf(var);
                    if (i == -1)
                    {
                        //this is a constant or a forall condition
                        //First we check if it is already in our list of constants.
                        ConstantType t = AllConstantTypes.Find(x => x.Name.Equals(var));
                        if (t == null) t = AllConstantTypes.Find(x => x.Name.Equals("any"));
                        Constant c = new Constant(var, t);
                        int index = constants.FindIndex(x => x.Name == var);
                        if (index == -1)
                        {
                            if (!var.StartsWith("?"))
                            {
                                //This constant is not in the list of constants for this actionType we must add it.                                 
                                constants.Add(c);
                                index = constants.Count - 1;
                            }
                            else
                            {
                                //This is a forall condition
                                index = constants.FindIndex(x => x.Name == "!" + var);
                            }
                        }
                        i = Globals.ConstReferenceNumber - index; //This ensures that this number remains negaitve and won't trickle over to normal references
                    }
                    references.Add(i);
                }
            }
            ForallConst = null;
            return new Tuple<string, List<int>>(name, references);
        }

        private TaskType FindTask(TaskType tT, List<TaskType> alltaskTypes)
        {
            foreach (TaskType t in alltaskTypes)
            {
                if (t.Name == tT.Name && t.NumOfVariables == tT.NumOfVariables) return t;
            }
            return tT;
        }

        //Creates proper rule conditions.
        private void CreateConditions(Rule curRule, List<Tuple<Term, bool>> preconditions)
        {
            List<string> methodParams = curRule.AllVars;
            List<int> varReferences = new List<int>();
            Tuple<int, String, List<int>> condition;
            if (curRule.PosPreConditions == null) curRule.PosPreConditions = new List<Tuple<int, string, List<int>>>();
            if (curRule.NegPreConditions == null) curRule.NegPreConditions = new List<Tuple<int, string, List<int>>>();
            foreach (Tuple<Term, bool> cond in preconditions)
            {
                for (int i = 0; i < cond.Item1.Variables.Length; i++)
                {
                    int j = 0;
                    foreach (String s in methodParams)
                    {
                        if (s.Equals(cond.Item1.Variables[i].Name) && curRule.AllVarsTypes[j] == cond.Item1.Variables[i].Type) break;
                        j++;
                    }
                    varReferences.Add(j);
                    if (j == -1 || j > methodParams.Count - 1)
                    {
                        if (cond.Item1.Variables[i] != ForallConst && cond.Item1.Variables[i].Name.StartsWith("?")) Console.WriteLine("Warning: Coudnt find condition {0} in allvars {1} in rule {2}", cond.Item1.Variables[i], string.Join(",", methodParams.ToArray()), curRule.MainTaskType.Name);
                        //If it doesnt start with a ? then it is a constant so of course its not in the main rule. It will be added later on. Non need to call warning
                    }
                }
                condition = new Tuple<int, string, List<int>>(-1, cond.Item1.Name, varReferences);
                varReferences = new List<int>();
                if (cond.Item2) curRule.PosPreConditions.Add(condition);
                else curRule.NegPreConditions.Add(condition);
            }
        }

        private void CreateBetweenConditions(Rule curRule, List<Tuple<List<int>, Term, bool>> betweenconditions)
        {
            List<string> methodParams = curRule.AllVars;
            List<int> varReferences = new List<int>();
            Tuple<int, int, String, List<int>> condition;
            if (curRule.PosBetweenConditions == null) curRule.PosBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
            if (curRule.NegBetweenConditions == null) curRule.NegBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
            foreach (Tuple<List<int>, Term, bool> cond in betweenconditions)
            {
                for (int i = 0; i < cond.Item2.Variables.Length; i++)
                {
                    int j = 0;
                    foreach (String s in methodParams)
                    {
                        if (s.Equals(cond.Item2.Variables[i].Name) && curRule.AllVarsTypes[j] == cond.Item2.Variables[i].Type) break;
                        j++;
                    }
                    varReferences.Add(j);
                    if (j == -1 || j > methodParams.Count - 1)
                    {
                        if (cond.Item2.Variables[i] != ForallConst && cond.Item2.Variables[i].Name.StartsWith("?")) Console.WriteLine("Warning: Coudnt find condition {0} in allvars {1} in rule {2}", cond.Item2.Variables[i], string.Join(",", methodParams.ToArray()), curRule.MainTaskType.Name);
                        //If it doesnt start with a ? then it is a constant so of course its not in the main rule. It will be added later on. Non need to call warning
                    }
                }
                condition = new Tuple<int, int, string, List<int>>(cond.Item1[0], cond.Item1[1], cond.Item2.Name, varReferences);
                varReferences = new List<int>();
                if (cond.Item3) curRule.PosBetweenConditions.Add(condition);
                else curRule.NegBetweenConditions.Add(condition);
            }
        }

        // line looks like this: (contentOf ?b ?c)
        // or like this: (not(= ?b ?b2))        
        //returns condition and bool is true if condition is positive false, if negative. 
        private Tuple<Term, bool> CreateCondition(string line, ref List<Constant> methodInfo, List<Constant> allConstants)
        {
            line = line.Trim();
            if (Forall) Forall = false; //Last condition was for all now is the one it applies to.
            bool isPositive = true;
            if (line.Contains("(not(") || line.Contains("(not "))
            {
                line = line.Replace("(not", "");
                isPositive = false;
            }
            //now line loks like this> (contentOf ?b ?c) or this: (= ?b ?b2))
            line = CleanUpInput(line, new List<string>() { "(and ", ")", "(", ":precondition", ":effect" }, ";;");
            string[] parts = line.Trim().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length == 0) return null;
            parts[0] = parts[0].Trim();
            string name = parts[0];
            if (name.Trim().Equals("exists")) return null;
            if (name.Trim().Equals("forall"))
            {
                for (int i = 1; i + 2 < parts.Length; i += 3)
                {
                    name = parts[i];
                    ConstantType t = AllConstantTypes.Find(x => x.Name.Equals(parts[i + 2]));
                    if (t == null) t = AllConstantTypes.Find(x => x.Name.Equals("any"));
                    ForallConst = new Constant("!" + name, t);
                    if (methodInfo == null) { methodInfo = new List<Constant>(); } //this was added because we had a forall condition on method with no parameters.
                    methodInfo.Add(ForallConst);
                }
                return null;
            }
            string[] vars = (string[])parts.Skip(1).ToArray();
            List<Constant> conVars = new List<Constant>();
            foreach (String s in vars)
            {
                Constant c = FindConstant(s, methodInfo);
                ConstantType any = AllConstantTypes.Find(x => x.Name.Equals("any"));
                if (c == null)
                {
                    c = FindConstant("!" + s, methodInfo);
                    if (c == null)
                    {
                        //This constant is not in the rules paramaters. We should add it there. 
                        c = FindConstant(s, allConstants);
                        if (c == null) c = new Constant(s, any);
                        methodInfo.Add(c);
                    }
                }
                conVars.Add(c);
            }
            Term term = new Term(name, conVars.ToArray());
            Tuple<Term, bool> tuple = new Tuple<Term, bool>(term, isPositive);
            return tuple;
        }

        /// <summary>
        /// Creates method parameters (including the ?)
        /// The line loks like this: :parameters (?b1 ?b2 - bowl ?c1 ?c2 - content)
        /// We ignore types for now. 
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private List<Constant> GetParameters(string line, List<ConstantType> types)
        {
            List<Constant> parameters = new List<Constant>();
            line = CleanUpInput(line, new List<string>() { "(and ", ":parameters ", "(", ")" }, ";;");
            List<String> curNames = new List<string>();
            ConstantType type;
            string[] parts = line.Trim().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length == 0) return null;
            parts[0] = parts[0].Trim();
            foreach (string par in parts)
            {
                if (par.Contains("?"))
                {
                    curNames.Add(par);
                }
                else
                {
                    if (par != "-")
                    {
                        type = types.First(x => x.Name.Equals(par));
                        if (type == null) Console.WriteLine("This has not type {0}", par);
                        if (curNames?.Any() == true)
                        {
                            foreach (String name in curNames)
                            {
                                parameters.Add(new Constant(name, type));
                            }
                        }
                        curNames = new List<string>();
                    }
                }
            }
            return parameters;
        }

        //The line loks like this: (st1 <st2)  or ike this (< st1 st2)
        //The tuple is ordered the same way the tasks are in rule. So based on which tuple it is in list it is the num. 
        private void CreateOrder(string line, List<Tuple<TaskType, string, int>> namedTasks, ref Rule curRule)
        {
            if (line.Equals(")")) return;
            line = CleanUpInput(line, new List<string>() { "(and ", "(", ")", "<" }, ";;");
            string[] parts = line.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length == 0) return;
            while (parts.Length > 0 && parts[0] == "")
            {
                parts = (string[])parts.Skip(1).ToArray();
            }
            parts[0] = parts[0].Trim();
            if (parts.Length > 2)
            {
                Console.WriteLine("Warning: Ordering of line \"{0}\" might be in the wrong format. ", line);
                Console.WriteLine("Expected input should look like this: task1 < task2 or like this: < task1 task2 ");
            }

            Tuple<TaskType, string, int> tuple1 = (Tuple<TaskType, string, int>)namedTasks.First(c => c.Item2.Equals(parts[0]));
            Tuple<TaskType, string, int> tuple2 = (Tuple<TaskType, string, int>)namedTasks.First(c => c.Item2.Equals(parts[1]));
            curRule.AddOrderCondition(tuple1.Item3, tuple2.Item3);
        }

        //The line loks like this: st1 (add cream ?b1))
        private Tuple<TaskType, String> CreateNamedTaskType(string line, ref List<Constant> methodParam, out List<int> refList, List<Constant> fixedConstants)
        {
            line = line.Replace("(and ", "("); // if the line starts with (and we should ignore it. 
            int index = line.IndexOf(";;"); //Removes everythign after ;; which symbolizes comment
            if (index > 0)
            {
                line = line.Substring(0, index);
            }
            string[] parts = line.Trim().Split('(').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            string name = parts[0].Trim();
            if (parts.Length > 1) line = line.Replace(name, ""); //This means that there is ordering. Sometimes there is no ordering so if there is none then. the task is just normal.              
            TaskType t = CreateTaskType(line, ref methodParam, out refList, fixedConstants);
            return new Tuple<TaskType, string>(t, name);
        }

        /// <summary>
        /// The line loks like this: :task (makeNoodles ?n ?p)
        /// or like this:  (add water ?p)
        ///Depends on whether this is the main task of rule or subtask.
        /// </summary>
        private TaskType CreateTaskType(string line, ref List<Constant> methodParam, out List<int> refList, List<Constant> fixedConstants)
        {
            refList = new List<int>();
            line = CleanUpInput(line, new List<string>() { "(and ", ":task ", "(", ")" }, ";;");
            string[] parts = line.Trim().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length == 0) { Console.WriteLine("Empty Line {0}", line); }
            string name = parts[0];
            string[] parameters = (string[])parts.Skip(1).ToArray();
            if (methodParam != null)
            {
                List<String> methodParNames = methodParam.Select(x => x.Name).ToList();
                foreach (string param in parameters)
                {
                    if (param != "")
                    {
                        if (!param.Contains("?")) //What is the point of this if?????
                        {
                            //It's not in method list we must add it.                     
                            if (!methodParNames.Contains(param))
                            {
                                Constant c = FindConstant(param, fixedConstants);
                                if (c == null)
                                {
                                    Console.WriteLine("Warning: We were given constant that does not exist {0}", param);
                                    ConstantType a = AllConstantTypes.Find(x => x.Name.Equals("any"));
                                    c = new Constant(param, a);
                                    fixedConstants.Add(c);
                                }
                                methodParam.Add(c);
                                refList.Add(methodParam.Count - 1);
                            }
                            else
                            {
                                refList.Add(methodParNames.IndexOf(param));
                            }
                        }
                        else
                        {
                            refList.Add(methodParNames.IndexOf(param));
                        }
                    }
                }
            }
            TaskType tT = new TaskType(name, parameters.Length);
            return tT;
        }

        /// <summary>
        /// Gets a list of constants and a name and returns the constant associated to it. 
        /// </summary>
        /// <param name="param"></param>
        /// <param name="fixedConstants"></param>
        /// <returns></returns>
        private Constant FindConstant(string param, List<Constant> fixedConstants)
        {
            Constant c = fixedConstants.Find(x => x.Name.Equals(param));
            return c;
        }

        //The line loks like this: (:task makeTomatoSoup :parameters (?p - cookingPot))
        private TaskType CreateTaskType(string line) //From list of main tasks
        {
            line = CleanUpInput(line, new List<string>() { "(:task " }, ";;");
            String[] parts = line.Trim().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();  //parts: makeTomatoSoup :parameters (?p - cookingPot))
            String name = parts[0]; //makeTomatoSoup
            String[] parameters = (string[])parts.Skip(2).ToArray();// (? p - cookingPot))
            List<string> myParams = new List<string>();
            foreach (String possibleParam in parameters)
            {
                //Currently we ignore types so just find the one that contains ?
                if (possibleParam.Contains("?")) myParams.Add(possibleParam); //Berem to is  tim otaznikem!                
            }
            TaskType tT = new TaskType(name, myParams.Count);
            return tT;
        }

        /// <summary>
        /// Reads the plan from the plan file.
        /// </summary>
        /// <param name="fileName"></param>
        public List<Action> ReadPlan(String fileName, List<ActionType> allActionTypes, List<Constant> allConstants)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            MyActions = new List<Action>();
            String line = "";
            try
            {
                while ((line = file.ReadLine()) != null)
                {
                    foreach (String a in line.Split('(').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray())
                    {
                        Term actionInstance = CreateActionInstance(a, allActionTypes, allConstants);
                        if (actionInstance != null)
                        {
                            Action action = new Action(actionInstance);
                            ActionType aT = FindActionType(allActionTypes, action);
                            aT.Instances.Add(action);
                            action.ActionType = aT;
                            action.CreateConditions(allConstants);
                            //Console.WriteLine(action);
                            MyActions.Add(action);
                        }
                    }
                }
                return MyActions;
            }
            catch (Exception e)
            {
                Console.WriteLine("Eror reading plan in file {0}.", fileName);
                Console.WriteLine("Error was found on line: {0}", line);
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private ActionType FindActionType(List<ActionType> allActionTypes, Action a)
        {
            return allActionTypes.First(x => x.Name.Equals(a.ActionInstance.Name) && x.NumOfVariables == a.ActionInstance.Variables.Length);
        }

        private ActionType FindActionType(List<ActionType> allActionTypes, string name, int vars)
        {
            return vars > 0
                ? allActionTypes.First(x => x.Name.Equals(name) && x.Vars != null && x.Vars.Count == vars)
                : allActionTypes.First(x => x.Name.Equals(name) && (x.Vars == null || x.Vars.Count == 0));
        }

        /// <summary>
        /// Reads the file explaining the problem.
        /// </summary>
        /// <param name="fileName"></param>
        public List<Term> ReadProblem(String fileName, List<ConstantType> allConstantTypes, ref List<Constant> constants)
        {
            List<Constant> inputConstants = new List<Constant>();
            String line = "";
            try
            {
                System.IO.StreamReader file = new System.IO.StreamReader(fileName);               
                List<Term> conditions = new List<Term>();
                bool inInit = false;
                bool inObjects = false;

                while ((line = file.ReadLine()) != null)
                {
                    if (line.Trim().Equals(")") && inInit) return conditions;
                    if (line.Contains(":init")) inInit = true;
                    if (line.Contains(":objects")) inObjects = true;
                    else if (inInit)
                    {
                        string[] parts = Regex.Split(line, @"(?=\()");
                        foreach (string part in parts)
                        {
                            Term c = CreateStateCondition(part, ref inputConstants, constants);
                            if (c != null) conditions.Add(c);
                        }
                    }
                    else if (inObjects)
                    {
                        if (line.Trim().Equals(")"))
                        {
                            inObjects = false;
                            AddNewConstants(inputConstants, ref constants); //Adds inputconstants in constants. Check uniqueness and substitute constantswith type any if possible.                         
                        }
                        GetConstants(line, ref inputConstants, allConstantTypes);
                    }
                }
                return conditions;
            }
            catch (Exception e)
            {
                Console.WriteLine("Eror reading problem in file {0}.", fileName);
                Console.WriteLine("Error was found on line: {0}", line);
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private void AddNewConstants(List<Constant> inputConstants, ref List<Constant> constants)
        {
            foreach (Constant c in inputConstants)
            {
                List<Constant> sameName = constants.Where(x => x.Name == c.Name).ToList();
                if (sameName?.Any() != true) constants.Add(c); //If my type is subset of a previous type. We change it. 
                else
                {
                    foreach (Constant ct in sameName)
                    {
                        if (ct.Type.IsAncestorTo(c.Type))
                        {
                            ct.Type = c.Type;
                        }
                        if (ct.Type.Name == "any") ct.Type = c.Type; //We change the type to my type as we now know better what constant this is. 
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up input. Removes all required words. If you wanna check for space after the words you must add it in the string.         /// e.        
        /// Also removes all comments after commentMark
        /// </summary>
        /// <param name="line"></param>
        /// <param name="wordsToRemove"></param>
        /// <returns></returns>
        private String CleanUpInput(String line, List<String> wordsToRemove, String commentMark)
        {
            foreach (String s in wordsToRemove)
            {
                line = line.Replace(s, "");
            }
            line = line.Trim();
            int index = line.IndexOf(commentMark); //Removes everything after commentMark which symbolizes comment
            if (index > 0)
            {
                line = line.Substring(0, index);
            }
            return line;
        }

        //The line loks like this: (contentof pot1 contentpot1)
        private Term CreateStateCondition(string line, ref List<Constant> methodInfo, List<Constant> allConstants)
        {
            Tuple<Term, bool> tupleC = CreateCondition(line, ref methodInfo, allConstants);
            if (tupleC != null)
            {
                if (tupleC.Item2)
                {// we only remember positive initial conditions. Negative just means that it's not in the list of positive ones. 
                    Term c = tupleC.Item1;
                    return c;
                }
            }
            return null;
        }

        /// <summary>
        /// From a file with a solution creates actionInstance which is an action. 
        /// </summary>
        private Term CreateActionInstance(String s, List<ActionType> allActionTypes, List<Constant> allConstants)
        {
            s = s.Replace(")", "");
            string[] parts = s.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length == 0)
            {
                //Console.WriteLine("Empty Line {0}", s);
                return null;
            }
            else
            {
                string name = parts[0];
                string[] variables = (string[])parts.Skip(1).ToArray();
                Constant[] vars = new Constant[variables.Length];
                ActionType m = FindActionType(allActionTypes, name, variables.Length);
                for (int i = 0; i < variables.Length; i++)
                {
                    Constant c = FindConstant(variables[i], allConstants);
                    //Constant c= new Constant(variables[i], m.ActionTerm.Variables[i].Type); //The problem here was that oil is ingredient but add takes food so then  when it was used in higher task it would not accept oil,cause higher task wants ingredient again. 
                    vars[i] = c;
                }
                Term actionInstance = new Term(name, vars);
                return actionInstance;
            }
        }
    }
}
